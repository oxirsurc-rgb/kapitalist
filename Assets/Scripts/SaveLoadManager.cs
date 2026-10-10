using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using UnityEngine;
using DemocracySim.Engine.Core;
using DemocracySim.Engine.Data;
using DemocracySim.Engine.World;
using DemocracySim.Engine.Legislative;

/// <summary>
/// Oyunu JSON olarak kaydeder/yükler.
/// FAZ -1: Çoklu slot, yedek kayıt, versiyonlama ve metadata eklendi.
/// </summary>
public static class SaveLoadManager
{
    private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
    {
        IncludeFields = true,
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    // Değerleri otomatik kaydedilecek alt sistemler
    private static readonly string[] SubsystemNames =
        { "Legitimacy", "Media", "Crisis", "Economy", "Army", "Intel", "EventSys", "Elections", "Cabinet", "Campaign" };

    // FAZ -1: Çoklu slot sistemi
    private const int MaxSlots = 5;
    private const string SaveDirName = "saves";
    private const string AutoSaveName = "autosave";

    private static string SaveDir => Path.Combine(Application.persistentDataPath, SaveDirName);
    private static string SlotPath(int slot) => Path.Combine(SaveDir, $"slot_{slot}.json");
    private static string AutoSavePath => Path.Combine(SaveDir, $"{AutoSaveName}.json");
    private static string BackupPath(int slot) => Path.Combine(SaveDir, $"slot_{slot}.backup.json");

    // Geriye dönük uyumluluk
    private static string SavePath => AutoSavePath;

    // ============================================================================
    // YARDIMCI SINIFLAR
    // ============================================================================

    public class CountrySave
    {
        public string Id, Name;
        public bool IsPlayer;
        public float GlobalAlignment;
        public float LegitimacyValue;
        public Dictionary<string, float> EngineValues = new Dictionary<string, float>();
        public Dictionary<string, Dictionary<string, float>> Subsystems = new Dictionary<string, Dictionary<string, float>>();
        public List<ObjSave> Objects = new List<ObjSave>();
        public List<DemoSave> Demographics = new List<DemoSave>();
        public List<ActorSave> Actors = new List<ActorSave>();
        public UniverseState Universe = new UniverseState();
    }

    public class ObjSave
    {
        public string Id, Name;
        public bool IsPolicy, IsActive;
        public float Actual, Perceived, Target, Min, Max, Align, Intensity, ImplementationSpeed;
        public Dictionary<string, float> Frames = new Dictionary<string, float>();
        public Dictionary<string, float> GroupImpacts = new Dictionary<string, float>();
    }

    public class DemoSave
    {
        public string Id, Name;
        public float Satisfaction;
        public Dictionary<string, float> Values = new Dictionary<string, float>();
    }

    public class ActorSave
    {
        public string Id, Name, Role;
        public Dictionary<string, float> Values = new Dictionary<string, float>();
    }

    // FAZ -1: UI için slot metadata
    public class SlotInfo
    {
        public int Slot;
        public bool Exists;
        public string SaveName;
        public string SaveDate;
        public string PlayerCountry;
        public int Turn;
        public string FilePath;
    }

    // ============================================================================
    // GENEL API
    // ============================================================================

    public static bool SaveExists() => File.Exists(AutoSavePath) || AnySlotExists();

    public static bool AnySlotExists()
    {
        EnsureSaveDir();
        for (int i = 0; i < MaxSlots; i++)
            if (File.Exists(SlotPath(i))) return true;
        return false;
    }

    private static void EnsureSaveDir()
    {
        try { if (!Directory.Exists(SaveDir)) Directory.CreateDirectory(SaveDir); }
        catch (Exception e) { Debug.LogError($"[SaveLoad] Save dizini oluşturulamadı: {e.Message}"); }
    }

    // Eski API: autosave'e yazar (geriye dönük uyumluluk)
    public static void SaveGame(WorldManager world, Country player)
    {
        AutoSave(world, player);
    }

    // FAZ -1: Autosave
    public static void AutoSave(WorldManager world, Country player)
    {
        EnsureSaveDir();
        SaveGameInternal(world, player, AutoSavePath, "Otomatik");
    }

    // FAZ -1: Slot'a kaydet
    public static void SaveGameToSlot(WorldManager world, Country player, int slot, string saveName = "")
    {
        if (slot < 0 || slot >= MaxSlots) { Debug.LogError($"[SaveLoad] Geçersiz slot: {slot}"); return; }
        EnsureSaveDir();

        string targetPath = SlotPath(slot);

        // Mevcut kayıt varsa yedekle
        if (File.Exists(targetPath))
        {
            try { File.Copy(targetPath, BackupPath(slot), true); }
            catch (Exception e) { Debug.LogWarning($"[SaveLoad] Yedek oluşturulamadı: {e.Message}"); }
        }

        SaveGameInternal(world, player, targetPath, saveName);
    }

    public static void DeleteSave()
    {
        try { if (File.Exists(SavePath)) File.Delete(SavePath); }
        catch (Exception e) { Debug.LogError($"[SaveLoad] Kayıt silinemedi: {e.Message}"); }
    }

    public static void DeleteSlot(int slot)
    {
        if (slot < 0 || slot >= MaxSlots) return;
        try
        {
            if (File.Exists(SlotPath(slot))) File.Delete(SlotPath(slot));
            if (File.Exists(BackupPath(slot))) File.Delete(BackupPath(slot));
        }
        catch (Exception e) { Debug.LogError($"[SaveLoad] Slot silinemedi: {e.Message}"); }
    }

    public static SlotInfo GetSlotInfo(int slot)
    {
        var info = new SlotInfo { Slot = slot, Exists = false };
        if (slot < 0 || slot >= MaxSlots) return info;

        string path = SlotPath(slot);
        if (!File.Exists(path)) return info;

        try
        {
            string json = File.ReadAllText(path);
            var data = JsonSerializer.Deserialize<SaveData>(json, Options);
            if (data == null) return info;

            info.Exists = true;
            info.SaveName = data.SaveName;
            info.SaveDate = data.SaveDate;
            info.PlayerCountry = data.PlayerCountryName;
            info.Turn = data.TotalTurns;
            info.FilePath = path;
        }
        catch { /* bozuk kayıt */ }

        return info;
    }

    public static List<SlotInfo> GetAllSlots()
    {
        var result = new List<SlotInfo>();
        for (int i = 0; i < MaxSlots; i++)
            result.Add(GetSlotInfo(i));
        return result;
    }

    // ============================================================================
    // SAVE INTERNAL
    // ============================================================================

    private static void SaveGameInternal(WorldManager world, Country player, string path, string saveName)
    {
        if (world == null || player == null) return;
        try
        {
            var data = new SaveData
            {
                SaveVersion = 2,
                SaveName = string.IsNullOrEmpty(saveName) ? $"Kayıt - {DateTime.Now:yyyy-MM-dd HH:mm}" : saveName,
                SaveDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                PlayerCountryName = player.Name,
                TotalTurns = world.GlobalTurn,
                GlobalTurn = world.GlobalTurn,
                GlobalTension = world.GlobalTension,
                PlayerCountryId = player.Id
            };
            // EK-1: RNG durumunu kaydet
data.RngMasterSeed = KapitalistRng.MasterSeed;
data.RngStreamStates = KapitalistRng.ExportStates();

            // 1. Ülkeleri Kaydet
            foreach (var c in world.Countries)
            {
                var cSave = new CountrySaveData
                {
                    Id = c.Id, Name = c.Name, IsPlayerControlled = c.IsPlayerControlled,
                    GlobalAlignment = c.GlobalAlignment,
                    Engine = new EngineSaveData
                    {
                        CurrentTurn = c.Engine.CurrentTurn,
                        TurnUntilElection = c.Engine.TurnUntilElection,
                        PoliticalCapital = c.Engine.PoliticalCapital,
                        Legitimacy = c.Engine.Legitimacy.CurrentLegitimacy,
                        CorruptionLevel = c.Engine.CorruptionLevel,
                        DeepStateStability = c.Engine.DeepStateStability,
                        Inflation = c.Engine.Economy.Inflation,
                        NationalDebt = c.Engine.Economy.NationalDebt,
                        CreditRating = c.Engine.Economy.CreditRating,
                        ArmySatisfaction = c.Engine.Army.ArmySatisfaction,
                        MilitaryStrength = c.Engine.Army.MilitaryStrength,
                        IntelNetworkStrength = c.Engine.Intel.NetworkStrength,
                        PlayerGlobalAlignment = c.Engine.PlayerGlobalAlignment,
                        CurrentRole = c.Engine.CurrentRole.ToString(),
                        LegitimacyModifier = c.Engine.Legitimacy.Modifier,
                        ArmyLoyaltyToLeader = c.Engine.Army.LoyaltyToLeader,
                        ArmyCoupRiskPercent = c.Engine.Army.CoupRiskPercent,
                        IntelTechLevel = c.Engine.Intel.TechLevel,
                        IntelAgencyBudget = c.Engine.Intel.AgencyBudget,
                        LeaderImage = c.Engine.Elections.LeaderImage,
                        ConsecutiveTerms = c.Engine.Elections.ConsecutiveTerms,
                        ScandalScar = c.Engine.Elections.ScandalScar,
                        PartyFund = c.Engine.Party.Fund,
                        ProposedPolicies = c.Engine.ProposedPolicies.Select(p => new ProposedPolicySaveData { PolicyId = p.Id }).ToList(),
                        Factions = c.Engine.Factions.Factions.Select(f => new FactionSaveData
                        {
                            Id = f.Id, Support = f.Support,
                            IsChallenging = f.IsChallenging, ChallengeTurnsLeft = f.ChallengeTurnsLeft
                        }).ToList(),
                        RulingFactionId = c.Engine.Factions.RulingFactionId,
                        CoalitionPartners = c.Engine.Universe.Partners.Select(p => new CoalitionPartnerSaveData
                        {
                            Id = p.Id, Name = p.Name, Ideology = p.Ideology,
                            Satisfaction = p.Satisfaction, InGovernment = p.InGovernment, LeftTurn = p.LeftTurn
                        }).ToList(),
                        ShadowCabinet = c.Engine.ShadowCab.Members.Select(m => new ShadowMinisterSaveData
                        {
                            Id = m.Id, Name = m.Name, Portfolio = m.Portfolio,
                            Competence = m.Competence, MediaSkill = m.MediaSkill,
                            Loyalty = m.Loyalty, AppointedTurn = m.AppointedTurn
                        }).ToList(),
                        ActiveAntiCampaigns = new Dictionary<string, int>(c.Engine.ActiveAntiCampaigns),
                        ActiveProtestTheme = c.Engine.Protest.Current != null ? c.Engine.Protest.Current.Theme.ToString() : "",
                        ActiveProtestTurnsLeft = c.Engine.Protest.Current != null ? c.Engine.Protest.Current.TurnsLeft : 0,
                        ActiveProtestIntensity = c.Engine.Protest.Current != null ? c.Engine.Protest.Current.InitialIntensity : 0f,
                        ProtestCooldown = c.Engine.Protest.CooldownTurns,
                        JudicialIndependence = c.Engine.Judiciary.JudicialIndependence,
                        ActiveCaseType = c.Engine.Judiciary.CurrentCase != null ? c.Engine.Judiciary.CurrentCase.Type.ToString() : "",
                        ActiveCaseTargetId = c.Engine.Judiciary.CurrentCase != null ? c.Engine.Judiciary.CurrentCase.TargetId : "",
                        ActiveCaseTurnsLeft = c.Engine.Judiciary.CurrentCase != null ? c.Engine.Judiciary.CurrentCase.TurnsLeft : 0,
                        ActiveCaseChance = c.Engine.Judiciary.CurrentCase != null ? c.Engine.Judiciary.CurrentCase.SuccessChance : 0f,
                        CaseCooldown = c.Engine.Judiciary.CaseCooldown,
                                                Sectors = c.Engine.Sectors.Sectors.Select(s => new SectorSaveData
                                                 
                        {
                            Id = s.Id, GdpShare = s.GdpShare, Productivity = s.Productivity,
                            TaxBurden = s.TaxBurden, Employment = s.Employment
                        }).ToList(),
                        TechLevel = c.Engine.Technology.TechLevel,
                        PatentCount = c.Engine.Technology.PatentCount,
                        RndBudget = c.Engine.Technology.RndBudget,
                        BrainGain = c.Engine.Technology.BrainGain,
                        PatentProgress = c.Engine.Technology.PatentProgress,
                        ActiveCrises = new List<ActiveCrisis>(c.Engine.CrisisChains.ActiveCrises),
                        // FAZ 3.5: Seçmen geçişi — oy blokları
VotingBlocs = new Dictionary<string, Dictionary<string, float>>(c.Engine.VoterTransition.VotingBlocs),

// FAZ 3.5: Fraksiyon talepleri
FactionDemands = c.Engine.FactionBargain.ActiveDemands.ToDictionary(
    kv => kv.Key,
    kv => new FactionDemandSaveData
    {
        PolicyId = kv.Value.PolicyId,
        TargetValue = kv.Value.TargetValue,
        SupportReward = kv.Value.SupportReward,
        RejectionPenalty = kv.Value.RejectionPenalty,
        IsFulfilled = kv.Value.IsFulfilled
    }),
    MigrantIntegration = c.Engine.Migration.IntegrationScore,
MigrantPopulation = c.Engine.Migration.TotalMigrantPopulation,
MigrantPendingRemittance = c.Engine.Migration.PendingRemittance,
MigrantHistory = new List<float>(c.Engine.Migration.MigrationHistory),

// FAZ 3.5: Kademeli radikalleşme
RadicalizationTiers = c.Engine.Universe.RadicalizationTiers.ToDictionary(
    kv => kv.Key,
    kv => (int)kv.Value),

// FAZ 3: Çıkar grupları
InterestGroups = c.Engine.InterestGroups.Select(ig => new InterestGroupSaveData
{
    Id = ig.Id,
    Name = ig.Name,
    Ideology = ig.Ideology,
    Power = ig.Power,
    Satisfaction = ig.Satisfaction,
    LobbyBudget = ig.LobbyBudget
}).ToList(),
UnlockedAchievements = new List<string>(c.Engine.Achievements.Unlocked),

// FAZ 3.5: Karar günlüğü
DecisionLog = c.Engine.DecisionLog.Select(dl => new DecisionLogSaveData
{
    Turn = dl.Turn,
    Category = dl.Category,
    Description = dl.Description,
    Impact = dl.Impact,
    Timestamp = dl.Timestamp.ToString("o")
}).ToList(),
                        ResolvedCrises = new Dictionary<string, int>(c.Engine.CrisisChains.ResolvedCrises),
                        TotalCrisesTriggered = c.Engine.CrisisChains.TotalCrisesTriggered,
                        TotalCrisesResolved = c.Engine.CrisisChains.TotalCrisesResolved
                    },
                                                 Memory = c.Memory,

                };
                // Objeleri ekle
                foreach (var obj in c.Engine.AllObjects)
                    cSave.Engine.Objects.Add(new ObjectSaveData
                    {
                        Id = obj.Id, ActualValue = obj.ActualValue,
                        IsActive = (obj is SimPolicy p && p.IsActive),
                        Intensity = (obj is SimPolicy sp ? sp.Intensity : 0)
                    });

                data.Countries.Add(cSave);
            }

            // 2. Organizasyonları Kaydet
            foreach (var org in world.Organizations)
            {
                data.Organizations.Add(new OrgSaveData
                {
                    Id = org.Id,
                    MemberIds = new List<string>(org.MemberIds),
                    LeaderCountryId = org.LeaderCountryId
                });
            }

            // 3. Diplomasiyi Kaydet
            data.DiplomacyRelations = new Dictionary<string, float>(world.Diplomacy.Relations);
                        // FAZ 0: AI hafızasını kaydet
            foreach (var c in world.Countries)
            {
                var mem = AIStateController.GetOrCreateMemory(c.Id);
                data.AIMemories[c.Id] = mem;
            }

            
            // EK-5 FIX: Atomik yazma — önce .tmp, sonra File.Move.
// Çökme durumunda eski kayıt bozulmaz.
string json = JsonSerializer.Serialize(data, Options);
DemocracySim.Engine.Data.AtomicSave.WriteAllText(path, json);
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveLoad] Kayıt hatası: {e.Message}");
        }
    }

    // ============================================================================
    // LOAD
    // ============================================================================

    // Eski API: autosave'den yükle
    public static (WorldManager, Country) LoadGame(List<CountryProfile> profiles)
    {
        return LoadGameFromPath(AutoSavePath, profiles);
    }

    // FAZ -1: Slot'tan yükle (bozuksa yedekten)
    public static (WorldManager, Country) LoadGameFromSlot(int slot, List<CountryProfile> profiles)
    {
        if (slot < 0 || slot >= MaxSlots) return (null, null);
        string targetPath = SlotPath(slot);

        var result = LoadGameFromPath(targetPath, profiles);
        if (result.Item1 != null) return result;

        string backupPath = BackupPath(slot);
        if (File.Exists(backupPath))
        {
            Debug.LogWarning($"[SaveLoad] Slot {slot} bozuk, yedekten yükleniyor...");
            return LoadGameFromPath(backupPath, profiles);
        }

        return (null, null);
    }

    private static (WorldManager, Country) LoadGameFromPath(string path, List<CountryProfile> profiles)
    {


        if (!File.Exists(path)) return (null, null);
try
{
    // EK-5 FIX: Bozuk ana kayıt varsa .bak'dan oku
    string json = DemocracySim.Engine.Data.AtomicSave.ReadAllTextWithFallback(path);
    if (string.IsNullOrEmpty(json)) return (null, null);
            var data = JsonSerializer.Deserialize<SaveData>(json, Options);
            // EK-1: RNG durumunu geri yükle
if (data.RngMasterSeed != 0)
{
    KapitalistRng.Restore(data.RngMasterSeed, data.RngStreamStates);
    SimLogger.Log($"[SaveLoad] RNG durumu geri yüklendi. Tohum: {data.RngMasterSeed}");
}
            if (data == null || data.Countries == null) return (null, null);

            // FAZ -1: Versiyon kontrolü
            if (data.SaveVersion < 2)
            {
                Debug.LogWarning($"[SaveLoad] Eski sürüm kayıt (v{data.SaveVersion}), migrate ediliyor...");
            }

            // 1. Dünya Yöneticisini Kur
            var world = new WorldManager();
            world.GlobalTurn = data.GlobalTurn;
            world.GlobalTension = data.GlobalTension;

            Country player = null;

            // 2. Ülkeleri Geri Yükle
            foreach (var cs in data.Countries)
            {
                var country = new Country(cs.Id, cs.Name, cs.IsPlayerControlled);
                DataManager.LoadWorld(country.Engine, "Resources/data");

                                if (cs.Memory != null)
                    country.Memory = cs.Memory;

                if (cs.IsPlayerControlled) player = country;

                var e = country.Engine;

                // Temel değerler
                e.CurrentTurn = cs.Engine.CurrentTurn;
                e.TurnUntilElection = cs.Engine.TurnUntilElection;
                e.PoliticalCapital = cs.Engine.PoliticalCapital;
                e.Legitimacy.SetLegitimacy(cs.Engine.Legitimacy);
                e.CorruptionLevel = cs.Engine.CorruptionLevel;
                e.DeepStateStability = cs.Engine.DeepStateStability;
                e.PlayerGlobalAlignment = cs.Engine.PlayerGlobalAlignment;
                e.Legitimacy.Modifier = cs.Engine.LegitimacyModifier;

                // Rol
                if (Enum.TryParse<SimulationEngine.PlayerRole>(cs.Engine.CurrentRole, out var playerRole))
                    e.CurrentRole = playerRole;

                // Seçim modeli
                e.Elections.LeaderImage = cs.Engine.LeaderImage;
                e.Elections.ConsecutiveTerms = cs.Engine.ConsecutiveTerms;
                e.Elections.ScandalScar = cs.Engine.ScandalScar;

                // Ekonomi, Ordu, İstihbarat
                e.Economy.LoadState(cs.Engine.Inflation, cs.Engine.NationalDebt, cs.Engine.CreditRating);
                e.Army.LoadState(cs.Engine.ArmySatisfaction, cs.Engine.MilitaryStrength, cs.Engine.ArmyLoyaltyToLeader);
                e.Intel.LoadState(cs.Engine.IntelNetworkStrength, cs.Engine.IntelAgencyBudget, cs.Engine.IntelTechLevel);

                // Parti fonu
                if (e.Party != null) e.Party.Fund = cs.Engine.PartyFund;

                // Fraksiyonlar
                if (cs.Engine.Factions != null)
                {
                    foreach (var fs in cs.Engine.Factions)
                    {
                        var faction = e.Factions.Factions.FirstOrDefault(f => f.Id == fs.Id);
                        if (faction != null)
                        {
                            faction.Support = fs.Support;
                            faction.IsChallenging = fs.IsChallenging;
                            faction.ChallengeTurnsLeft = fs.ChallengeTurnsLeft;
                        }
                    }
                    e.Factions.RulingFactionId = cs.Engine.RulingFactionId;
                }
                // FAZ 3: Göç
e.Migration.IntegrationScore = cs.Engine.MigrantIntegration;
e.Migration.TotalMigrantPopulation = cs.Engine.MigrantPopulation;
e.Migration.PendingRemittance = cs.Engine.MigrantPendingRemittance;
e.Migration.MigrationHistory = cs.Engine.MigrantHistory ?? new List<float>();

                // Gölge kabine
                e.ShadowCab.Members.Clear();
                if (cs.Engine.ShadowCabinet != null)
                {
                    foreach (var sm in cs.Engine.ShadowCabinet)
                    {
                        e.ShadowCab.Members.Add(new ShadowMinister(sm.Id, sm.Name, sm.Portfolio)
                        {
                            Competence = sm.Competence,
                            MediaSkill = sm.MediaSkill,
                            Loyalty = sm.Loyalty,
                            AppointedTurn = sm.AppointedTurn
                        });
                    }
                }

                // Anti-kampanyalar
                e.ActiveAntiCampaigns.Clear();
                if (cs.Engine.ActiveAntiCampaigns != null)
                {
                    foreach (var kv in cs.Engine.ActiveAntiCampaigns)
                        e.ActiveAntiCampaigns[kv.Key] = kv.Value;
                }

                // Protesto
                if (e.Protest != null)
                {
                    if (!string.IsNullOrEmpty(cs.Engine.ActiveProtestTheme) && cs.Engine.ActiveProtestTurnsLeft > 0)
                    {
                        if (Enum.TryParse<ProtestTheme>(cs.Engine.ActiveProtestTheme, out var theme))
                        {
                            e.Protest.RestoreProtest(theme, cs.Engine.ActiveProtestTurnsLeft, cs.Engine.ActiveProtestIntensity);
                        }
                    }
                    e.Protest.CooldownTurns = cs.Engine.ProtestCooldown;
                }

                // Yargı
                if (e.Judiciary != null)
                {
                    e.Judiciary.JudicialIndependence = cs.Engine.JudicialIndependence;
                    e.Judiciary.CaseCooldown = cs.Engine.CaseCooldown;
                    if (!string.IsNullOrEmpty(cs.Engine.ActiveCaseType) && cs.Engine.ActiveCaseTurnsLeft > 0)
                    {
                        if (Enum.TryParse<CaseType>(cs.Engine.ActiveCaseType, out var caseType))
                        {
                            e.Judiciary.RestoreCase(caseType, cs.Engine.ActiveCaseTargetId,
                                cs.Engine.ActiveCaseTurnsLeft, cs.Engine.ActiveCaseChance);
                        }
                    }
                }

                // Koalisyon ortakları
                if (cs.Engine.CoalitionPartners != null)
                {
                    e.Universe.Partners.Clear();
                    foreach (var p in cs.Engine.CoalitionPartners)
                    {
                        e.Universe.Partners.Add(new CoalitionPartner
                        {
                            Id = p.Id, Name = p.Name, Ideology = p.Ideology,
                            Satisfaction = p.Satisfaction, InGovernment = p.InGovernment, LeftTurn = p.LeftTurn
                        });
                    }
                }

                // Taslak Yasalar
                foreach (var propSave in cs.Engine.ProposedPolicies)
                {
                    var policy = e.AllObjects.OfType<SimPolicy>().FirstOrDefault(p => p.Id == propSave.PolicyId);
                    if (policy != null) e.ProposedPolicies.Add(policy);
                }

                // Objeler
                foreach (var os in cs.Engine.Objects)
                {
                    var obj = e.AllObjects.FirstOrDefault(x => x.Id == os.Id);
                    if (obj != null)
                    {
                        obj.ActualValue = os.ActualValue;
                        if (obj is SimPolicy p)
                        {
                            p.IsActive = os.IsActive;
                            p.Intensity = os.Intensity;
                        }
                    }
                }

                // Sektörler
                if (cs.Engine.Sectors != null)
                {
                    foreach (var ss in cs.Engine.Sectors)
                    {
                        var sector = e.Sectors.Sectors.FirstOrDefault(s => s.Id == ss.Id);
                        if (sector != null)
                        {
                            sector.GdpShare = ss.GdpShare;
                            sector.Productivity = ss.Productivity;
                            sector.TaxBurden = ss.TaxBurden;
                            sector.Employment = ss.Employment;
                        }
                    }
                }
                                // FAZ 2: Teknolojik verileri geri yükle
                if (e.Technology != null)
                {
                    e.Technology.TechLevel = cs.Engine.TechLevel;
                    e.Technology.PatentCount = cs.Engine.PatentCount;
                    e.Technology.RndBudget = cs.Engine.RndBudget;
                    e.Technology.BrainGain = cs.Engine.BrainGain;
                    e.Technology.PatentProgress = cs.Engine.PatentProgress;
                }
                                // FAZ 3: Kriz verilerini geri yükle
                if (e.CrisisChains != null)
                {
                    e.CrisisChains.ActiveCrises = cs.Engine.ActiveCrises ?? new List<ActiveCrisis>();
                    e.CrisisChains.ResolvedCrises = cs.Engine.ResolvedCrises ?? new Dictionary<string, int>();
                    e.CrisisChains.TotalCrisesTriggered = cs.Engine.TotalCrisesTriggered;
                    e.CrisisChains.TotalCrisesResolved = cs.Engine.TotalCrisesResolved;
                }
                // FAZ 3.5: Seçmen geçişi — oy blokları
if (cs.Engine.VotingBlocs != null)
{
    e.VoterTransition.VotingBlocs.Clear();
    foreach (var kv in cs.Engine.VotingBlocs)
        e.VoterTransition.VotingBlocs[kv.Key] = new Dictionary<string, float>(kv.Value);
}

// FAZ 3.5: Fraksiyon talepleri
if (cs.Engine.FactionDemands != null)
{
    e.FactionBargain.ActiveDemands.Clear();
    foreach (var kv in cs.Engine.FactionDemands)
    {
        e.FactionBargain.ActiveDemands[kv.Key] = new FactionDemand
        {
            PolicyId = kv.Value.PolicyId,
            TargetValue = kv.Value.TargetValue,
            SupportReward = kv.Value.SupportReward,
            RejectionPenalty = kv.Value.RejectionPenalty,
            IsFulfilled = kv.Value.IsFulfilled
        };
    }
}

// FAZ 3.5: Kademeli radikalleşme
if (cs.Engine.RadicalizationTiers != null)
{
    e.Universe.RadicalizationTiers.Clear();
    foreach (var kv in cs.Engine.RadicalizationTiers)
        e.Universe.RadicalizationTiers[kv.Key] = (RadicalizationTier)kv.Value;
}

// FAZ 3: Çıkar grupları
if (cs.Engine.InterestGroups != null)
{
    e.InterestGroups.Clear();
    foreach (var ig in cs.Engine.InterestGroups)
    {
        e.InterestGroups.Add(new InterestGroup(ig.Id, ig.Name, ig.Ideology)
        {
            Power = ig.Power,
            Satisfaction = ig.Satisfaction,
            LobbyBudget = ig.LobbyBudget
        });
    }
}

// FAZ 3.5: Karar günlüğü
if (cs.Engine.DecisionLog != null)
{
    e.DecisionLog.Clear();
    foreach (var dl in cs.Engine.DecisionLog)
    {
        e.DecisionLog.Add(new SimulationEngine.DecisionLogEntry
        {
            Turn = dl.Turn,
            Category = dl.Category,
            Description = dl.Description,
            Impact = dl.Impact,
            Timestamp = DateTime.TryParse(dl.Timestamp, out var ts) ? ts : DateTime.Now
        });
    }
}

        // FAZ 4: Başarımlar
if (cs.Engine.UnlockedAchievements != null)
{
    e.Achievements.Unlocked.Clear();
    foreach (var id in cs.Engine.UnlockedAchievements)
        e.Achievements.Unlocked.Add(id);
}

                country.GlobalAlignment = cs.GlobalAlignment;
                country.Continent = profiles?.FirstOrDefault(p => p.id == cs.Id)?.continent ?? "";
                world.Countries.Add(country);
            }

            // 3. Organizasyonları Geri Yükle (varsayılanları koru)
            if (data.Organizations != null)
            {
                foreach (var os in data.Organizations)
                {
                    var org = world.Organizations.FirstOrDefault(o => o.Id == os.Id);
                    if (org == null) continue;
                    org.MemberIds = new List<string>(os.MemberIds);
                    org.LeaderCountryId = os.LeaderCountryId;
                }
            }

            // 4. Diplomasiyi Geri Yükle
            if (data.DiplomacyRelations != null)
            {
                foreach (var rel in data.DiplomacyRelations)
                {
                    var ids = rel.Key.Split('_');
                    if (ids.Length == 2)
                    {
                        world.Diplomacy.SetRelation(ids[0], ids[1], rel.Value);
                    }
                }
            }
                        // FAZ 0: AI hafızasını geri yükle
            if (data.AIMemories != null)
            {
                foreach (var kv in data.AIMemories)
                {
                    AIStateController.CountryMemories[kv.Key] = kv.Value;
                }
            }

            if (player == null) return (null, null);
            return (world, player);
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveLoad] Yükleme hatası: {e.Message}\n{e.StackTrace}");
            return (null, null);
        }
    }

    // ============================================================================
    // YANSIMA YARDIMCILARI (eski Snapshot/Restore için — kullanılmıyor ama lazım)
    // ============================================================================

    private static object GetMember(object o, string name)
    {
        if (o == null) return null;
        var t = o.GetType();
        var prop = t.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
        if (prop != null && prop.GetIndexParameters().Length == 0) return prop.GetValue(o);
        var field = t.GetField(name, BindingFlags.Public | BindingFlags.Instance);
        return field != null ? field.GetValue(o) : null;
    }

    private static float GetNum(object o, string name)
    {
        try
        {
            var v = GetMember(o, name);
            if (v == null) return 0f;
            if (v is Enum) return Convert.ToInt32(v);
            if (v is bool b) return b ? 1f : 0f;
            return Convert.ToSingle(v);
        }
        catch { return 0f; }
    }

    private static void SetNum(object o, string name, float value)
    {
        var t = o.GetType();
        var prop = t.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
        if (prop != null && prop.CanWrite && prop.GetIndexParameters().Length == 0)
        {
            TrySet(o, prop.PropertyType, value, v => prop.SetValue(o, v));
            return;
        }
        var field = t.GetField(name, BindingFlags.Public | BindingFlags.Instance);
        if (field != null && !field.IsInitOnly)
            TrySet(o, field.FieldType, value, v => field.SetValue(o, v));
    }

    private static void TrySet(object o, Type type, float value, Action<object> setter)
    {
        try
        {
            object converted;
            if (type == typeof(float)) converted = value;
            else if (type == typeof(double)) converted = (double)value;
            else if (type == typeof(int)) converted = (int)Math.Round(value);
            else if (type == typeof(bool)) converted = value > 0.5f;
            else if (type.IsEnum) converted = Enum.ToObject(type, (int)Math.Round(value));
            else return;
            setter(converted);
        }
        catch { }
    }

    private static bool IsNumeric(Type t)
        => t == typeof(float) || t == typeof(double) || t == typeof(int) || t == typeof(bool) || t.IsEnum;

    private static float ToFloat(object v)
    {
        if (v is bool b) return b ? 1f : 0f;
        if (v is Enum) return Convert.ToInt32(v);
        return Convert.ToSingle(v);
    }

    private static Dictionary<string, float> Dump(object o)
    {
        var d = new Dictionary<string, float>();
        if (o == null) return d;
        var t = o.GetType();

        foreach (var p in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!p.CanRead || p.GetIndexParameters().Length > 0 || p.GetSetMethod(true) == null) continue;
            if (!IsNumeric(p.PropertyType)) continue;
            try { d[p.Name] = ToFloat(p.GetValue(o)); } catch { }
        }
        foreach (var f in t.GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            if (f.IsInitOnly || f.IsLiteral || !IsNumeric(f.FieldType)) continue;
            try { d[f.Name] = ToFloat(f.GetValue(o)); } catch { }
        }
        return d;
    }

    private static void Apply(object o, Dictionary<string, float> values)
    {
        if (o == null || values == null) return;
        foreach (var kv in values) SetNum(o, kv.Key, kv.Value);
    }
}