using UnityEngine;
using UnityEngine.UI; 
using UnityEngine.SceneManagement;
using TMPro; 
using System;                          
using System.Collections.Generic;
using System.Linq;
using DemocracySim.Engine.Core;
using DemocracySim.Engine.Data;
using DemocracySim.Engine.World;
using DemocracySim.Engine.Legislative;
using DemocracySim.Engine.Core.Multiplayer;

public class GameManager : MonoBehaviour
{
    public enum GameState { Selection, Gameplay }
    // FAZ 23.1: Hot-Seat
public HotSeatController HotSeat { get; private set; }
private HotSeatUIPanel _hotSeatUI;
private PBEMUIPanel _pbemUI;
    public GameState currentState = GameState.Selection;

     [Header("Core Systems")]
    public UIManager ui;                          // UI referansı kalabilir (MonoBehaviour)

    // Runtime-only referanslar: private + property
    [System.NonSerialized] private WorldManager _world;
    [System.NonSerialized] private Country _playerCountry;

    public WorldManager world { get => _world; set => _world = value; }
    public Country playerCountry { get => _playerCountry; set => _playerCountry = value; }

    [Header("Prefabs")]
    public GameObject countryButtonPrefab; 

    public static bool LoadSaveOnStart = false;
    private List<CountryProfile> availableCountries;

    // EK-7: Tüm denge sabitleri BalanceConfig ScriptableObject'ten okunur.
// Editor'de Assets/Resources/BalanceConfig.asset'ten değiştirilebilir.
private BalanceConfig Cfg => BalanceConfig.Instance;

private float CustomPolicyCost => Cfg.CustomPolicyCost;
private float LobbyCapitalThreshold => Cfg.LobbyCapitalThreshold;
private float RallyCostGoverning => Cfg.RallyCostGoverning;
private float RallyCostOpposition => Cfg.RallyCostOpposition;
private float PopulistPromiseCost => Cfg.PopulistPromiseCost;
private float PersuadeMinisterCost => Cfg.PersuadeMinisterCost;
private float NeutralizeRivalCost => Cfg.NeutralizeRivalCost;
private float ScandalCost => Cfg.ScandalCost;
private const string MainMenuSceneName = "MainMenu";

    public static GameManager Instance { get; private set; }
    private bool gameOver = false;

    void Awake()
    {
        Instance = this;

    if (AudioManager.Instance == null)
    {
        var audioGO = new GameObject("AudioManager");
        audioGO.AddComponent<AudioManager>();
        Debug.Log("[GameManager] AudioManager otomatik oluşturuldu.");
    }



        SimLogger.OnLog = (msg, level) => {
    switch(level) {
        case SimLogger.LogLevel.Error: UnityEngine.Debug.LogError(msg); break;
        case SimLogger.LogLevel.Warning: UnityEngine.Debug.LogWarning(msg); break;
        default: UnityEngine.Debug.Log(msg); break;
    }
};
        Debug.Log("[TEŞHİS] GameManager.Awake() BAŞLADI");
        // EK-27: Modları tara
ModManager.DiscoverMods();

availableCountries = DataManager.LoadCountryProfiles();
        Debug.Log($"[TEŞHİS] availableCountries.Count = {(availableCountries != null ? availableCountries.Count.ToString() : "NULL!")}");
        Debug.Log($"[TEŞHİS] LoadSaveOnStart = {LoadSaveOnStart}, SaveExists = {SaveLoadManager.SaveExists()}");
        if (LoadSaveOnStart && SaveLoadManager.SaveExists())
        {
            LoadSaveOnStart = false;
            Debug.Log("[TEŞHİS] LoadGame() çağrılıyor");
            LoadGame();
        }
        else
        {
            Debug.Log("[TEŞHİS] CreateCountryButtons() çağrılıyor");
            CreateCountryButtons();
            Debug.Log("[TEŞHİS] ui.ShowPanel(\"Selection\") çağrılıyor");
            ui.ShowPanel("Selection");
        }
         
         WireGameplayButtons();
Debug.Log("[TEŞHİS] GameManager.Awake() BİTTİ");

// FAZ 5: Demo başlatma
if (PlayerPrefs.GetInt("StartDemo", 0) == 1)
{
    PlayerPrefs.SetInt("StartDemo", 0);
    PlayerPrefs.Save();
    StartDemo();
}
// FAZ 23.1: Hot-Seat UI panel
var hotSeatGO = new GameObject("HotSeatUIPanel");
hotSeatGO.transform.SetParent(transform.parent, false);
_hotSeatUI = hotSeatGO.AddComponent<HotSeatUIPanel>();
// FAZ 23.2: PBEM UI panel
var pbemGO = new GameObject("PBEMUIPanel");
pbemGO.transform.SetParent(transform.parent, false);
_pbemUI = pbemGO.AddComponent<PBEMUIPanel>();
    }
        /// <summary>FAZ 2.5: AI ülke için rastgele bakan isimleri üret.</summary>
    void RandomizeAIMinisters(Country ai, CountryProfile profile)
    {
        var aiMinisters = ai.Engine.Actors.Where(a => a.Role == ActorRole.Minister).ToList();
        
        // Ülke bazlı isim havuzları
        string[] firstNames = { "Hans", "Maria", "Klaus", "Anna", "Jean", "Pierre", 
                                "Chen", "Li", "Wei", "Carlos", "Maria", "João",
                                "John", "Sarah", "Michael", "Linda" };
        string[] lastNames = { "Schmidt", "Müller", "Weber", "Fischer", "Dubois", 
                               "Chen", "Wang", "Li", "Silva", "Santos",
                               "Smith", "Johnson", "Brown", "Davis" };
        
        // EK-1 FIX: String.GetHashCode() .NET Core'da kararlı değil. FNV-1a kullan.
int stableSeed = StableHash.Fnv1aInt(profile.id);
var rng = new System.Random(stableSeed);   // Ülkeye özgü deterministik seed
        
        int idx = 0;
        foreach (var minister in aiMinisters)
        {
            string first = firstNames[rng.Next(firstNames.Length)];
            string last = lastNames[rng.Next(lastNames.Length)];
            minister.Name = $"{first} {last}";
            
            // Portföy ata
            string[] portfolios = { "Ekonomi", "Sağlık", "Savunma", "Eğitim", "Adalet", "Dışişleri", "İçişleri" };
            minister.Portfolio = portfolios[idx % portfolios.Length];
            idx++;
        }
    }

    void OnApplicationQuit()
{
    if (currentState == GameState.Gameplay && !gameOver && world != null && playerCountry != null)
    {
        SaveLoadManager.SaveGame(world, playerCountry);

        // Hot-Seat veya PBEM ise oturumu da kaydet
        if (SessionManager.ActiveSession != null && !SessionManager.ActiveSession.IsSingle)
        {
            SessionManager.SaveSession("autosave.session.json");
        }
    }
    SessionManager.EndSession();
    }

    void WireGameplayButtons()
    {
        if (ui.nextTurnButton != null)
        {
            ui.nextTurnButton.onClick.AddListener(() =>
            {
                var net = DemocracySim.Engine.Core.Multiplayer.Online.DemocracyNetworkManager.Instance;
                if (net != null && net.IsOnlineActive)
                {
                    net.SendTurnFinished(playerCountry.Engine.CurrentTurn);
                    ui.nextTurnButton.interactable = false;
                    ui.Notify("[ÇEVRİM İÇİ] Turunuz tamamlandı. Diğer oyuncular bekleniyor...", false);
                    return;
                }
                NextTurn();
            });
        }
        if (ui.intelButton != null) ui.intelButton.onClick.AddListener(() => OnStartSecretOp(0));
        if (ui.sabotageButton != null) ui.sabotageButton.onClick.AddListener(() => OnStartSecretOp(1));
        if (ui.manipulateButton != null) ui.manipulateButton.onClick.AddListener(() => OnStartSecretOp(2));
        if (ui.closeDiplomacyButton != null) ui.closeDiplomacyButton.onClick.AddListener(ToggleDiplomacy);
        ui.OnDiplomacyTargetChanged = RefreshDiplomacyInfo;
        ui.CreateActionButtons(OpenCustomPolicyMenu, OpenLobbyMenu, OpenStateMenu, OpenProtestMenu,
    OpenFoundPartyMenu, OpenCandidateCampaignMenu, OpenMinistryMarketMenu);
    }

    void CreateCountryButtons()
    {
        if (ui == null || availableCountries == null) return;
        var items = new List<(string title, string subtitle)>();
        foreach (var p in availableCountries) items.Add((p.name, p.continent ?? ""));
        ui.ShowCountrySelection(items, SelectCountry);
    }
    // =====================================================================
// EK-23: PARTİ KURMA
// =====================================================================
void OpenFoundPartyMenu()
{
    if (playerCountry == null) return;
    var engine = playerCountry.Engine;

    // Önce sebep kontrolü
    string blocker = PartyFounder.CanFound(engine);
    if (blocker != null)
    {
        var reasons = PartyFounder.RepresentationCrisis(engine);
        string msg = reasons == null
            ? blocker
            : $"Parti kurmak için sebebiniz yok:\n\n{blocker}";
        ui.ShowChoicePopup($"PARTİ KUR — UYGUN DEĞİL\n\n{msg}",
            new List<(string, Action)> { ("Kapat", () => { }) });
        return;
    }

    var reasonList = PartyFounder.RepresentationCrisis(engine);

    // İsim isteme modal'ı
    ui.ShowPartyFoundPrompt(reasonList, (partyName, slogan, ideology) =>
    {
        string result = PartyFounder.Found(engine, partyName, slogan, ideology);
        ui.WriteLog($"🏛️ {result}");
        UpdateUI();
    });
}

// =====================================================================
// EK-24: ADAYLIK KAMPANYASI (Seçim kaybı sonrası fraksiyonlara yaranma)
// =====================================================================
void OpenCandidateCampaignMenu()
{
    if (playerCountry == null) return;
    var engine = playerCountry.Engine;

    if (!CandidateManager.InCandidateCrisis(engine))
    {
        ui.WriteLog("Şu an adaylık krizi içinde değilsiniz.");
        return;
    }

    float support = CandidateManager.GetCandidateSupport(engine);
    bool canRun = CandidateManager.CanRun(engine);

    var options = new List<(string label, Action onClick)>();

    // Her fraksiyona taviz seçeneği
    foreach (var f in engine.Factions.Factions)
    {
        var faction = f;
        string side = faction.Ideology < -20f ? "Sol" :
                      faction.Ideology > 20f ? "Sağ" : "Merkez";
        string label = $"Taviz Ver: {faction.Name} ({side}, %{faction.Support:F0}) — {CandidateManager.ConcessionCost:F0} sermaye";
        options.Add((label, () => DoConcede(faction)));
    }

    options.Add(("Kapat", () => { }));

    string title = $"ADAYLIK KAMPANYASI\n\n" +
                   $"Seçime kalan: {engine.TurnUntilElection} tur\n" +
                   $"Fraksiyon desteği: %{support:F0} (gereken: %{CandidateManager.MinSupportForCandidacy:F0})\n" +
                   $"Sermaye: {engine.PoliticalCapital:F0}\n\n" +
                   (canRun 
                       ? "✅ Adaylığınız güvende. Taviz vererek daha da güçlendirebilirsiniz."
                       : "⚠️ DESTEK YETERSİZ! Seçim günü aday olamazsanız parti içi darbe olur.");

    ui.ShowChoicePopup(title, options);
}

// =====================================================================
// EK-25: BAKAN PİYASASI
// =====================================================================
void OpenMinistryMarketMenu()
{
    if (playerCountry == null) return;
    var engine = playerCountry.Engine;

    var options = new List<(string label, Action onClick)>();

    // 1) Adayları listele
    foreach (var candidate in engine.Universe.MinistryMarket)
    {
        var cand = candidate;
        string side = cand.Ideology < -20f ? "Sol" :
                      cand.Ideology > 20f ? "Sağ" : "Merkez";
        string label = $"[İŞE AL] {cand.Name} ({cand.Portfolio}, {side})\n" +
                       $"Yetkinlik: %{cand.Competence:F0} | Sadakat: %{cand.Loyalty:F0} | {cand.Trait}";
        options.Add((label, () => DoHireMinister(cand.Id)));
    }

    // 2) Mevcut bakanları görevden alma
    var ministers = engine.Actors.Where(a => a.Role == ActorRole.Minister).ToList();
    if (ministers.Count > 0)
    {
        foreach (var m in ministers)
        {
            var minister = m;
            string label = $"[GÖREVDEN AL] {minister.Name} ({minister.Portfolio}) — {MinistryMarket.FiringCost:F0} sermaye";
            options.Add((label, () => DoFireMinister(minister.Id)));
        }
    }

    options.Add(("Kapat", () => { }));

    string title = $"BAKAN PİYASASI\n\n" +
                   $"Sermaye: {engine.PoliticalCapital:F0}\n" +
                   $"Bakan sayısı: {ministers.Count}/8\n" +
                   $"İşe alım maliyeti: {MinistryMarket.HireCost:F0}\n" +
                   $"Görevden alma maliyeti: {MinistryMarket.FiringCost:F0}\n\n" +
                   (engine.Universe.MinistryMarket.Count == 0
                       ? "(Piyasa boş — yeni adaylar 3 turda bir gelir)"
                       : $"{engine.Universe.MinistryMarket.Count} aday mevcut");

    ui.ShowChoicePopup(title, options);
}

// =====================================================================
// FAZ 9: EKONOMİ KRİZ MENÜSÜ
// =====================================================================
void OpenEconomyCrisisMenu()
{
    if (playerCountry == null) return;
    var engine = playerCountry.Engine;

    float health = EconomicCrisisManager.GetEconomicHealth(engine);
    string healthLabel = EconomicCrisisManager.GetHealthLabel(health);
    Color healthColor = health >= 0.55f ? HudTheme.Good :
                        health >= 0.30f ? HudTheme.Warn : HudTheme.Bad;

    var options = new List<(string label, Action onClick)>();

    // 1) IMF Yardımı
    string imfBlocker = EconomicCrisisManager.CanGetImfBailout(engine);
    if (imfBlocker == null)
    {
        options.Add(($"💰 IMF YARDIMI: +{BalanceConfig.Instance.ImfBailoutAmount:F0} milyar borç affı " +
                     $"(Meşruiyet -{BalanceConfig.Instance.ImfBailoutConditionLegitCost:F0}, " +
                     $"Ordu -{BalanceConfig.Instance.ImfBailoutConditionArmyCost:F0})",
            () => DoEconomicAction("IMF", () => EconomicCrisisManager.GetImfBailout(engine))));
    }
    else
    {
        options.Add(($"❌ IMF: {imfBlocker}", () => { }));
    }

    // 2) Acil Vergi
    string taxBlocker = EconomicCrisisManager.CanUseEmergencyTax(engine);
    if (taxBlocker == null)
    {
        options.Add(($"💸 ACİL VERGİ: +{BalanceConfig.Instance.EmergencyTaxCapital:F0} sermaye, " +
                     $"-80 borç (Huzursuzluk +{BalanceConfig.Instance.EmergencyTaxUnrest:F0}, " +
                     $"Meşruiyet -{BalanceConfig.Instance.EmergencyTaxLegitCost:F0})",
            () => DoEconomicAction("Vergi", () => EconomicCrisisManager.ApplyEmergencyTax(engine))));
    }
    else
    {
        options.Add(($"❌ Acil Vergi: {taxBlocker}", () => { }));
    }

    // 3) Yapısal Reform
    string reformBlocker = EconomicCrisisManager.CanUseStructuralReform(engine);
    if (reformBlocker == null)
    {
        options.Add(($"📋 YAPISAL REFORM: -{BalanceConfig.Instance.StructuralReformDebtReduction:F0} borç, " +
                     $"{BalanceConfig.Instance.StructuralReformDuration} tur boyunca meşruiyet toparlanır " +
                     $"(Maliyet {BalanceConfig.Instance.StructuralReformCost:F0} sermaye, " +
                     $"kısa vadede huzursuzluk +{BalanceConfig.Instance.StructuralReformUnrestAdd:F0})",
            () => DoEconomicAction("Reform", () => EconomicCrisisManager.ApplyStructuralReform(engine))));
    }
    else
    {
        options.Add(($"❌ Reform: {reformBlocker}", () => { }));
    }

    options.Add(("Kapat", () => { }));

    // Başlık
    string title = $"EKONOMİ KRİZ YÖNETİMİ\n\n" +
                   $"Ekonomik Sağlık: %{health * 100:F0} — {healthLabel}\n" +
                   $"Kredi Notu: {engine.Economy.CreditRating:F0}/100\n" +
                   $"Ulusal Borç: {engine.Economy.NationalDebt:F0} milyar\n" +
                   $"Enflasyon: %{engine.Economy.Inflation:F1}\n" +
                   $"Sermaye: {engine.PoliticalCapital:F0}\n\n" +
                   $"Uyarı Seviyesi: {GetWarningLevelText(engine.Universe.EconomicWarningLevel)}";

    ui.ShowChoicePopup(title, options);
}

string GetWarningLevelText(int level)
{
    switch (level)
    {
        case 2: return "🚨 KRİTİK";
        case 1: return "⚠️ UYARI";
        default: return "✅ Normal";
    }
}

void DoEconomicAction(string actionName, Func<string> action)
{
    string result = action();
    ui.WriteLog($"[Ekonomi] {actionName}: {result}");
    UpdateUI();
    OpenEconomyCrisisMenu();   // Menüyü yenile
}

void DoHireMinister(string candidateId)
{
    var engine = playerCountry.Engine;
    string result = MinistryMarket.Hire(engine, candidateId);
    ui.WriteLog(result);
    UpdateUI();
    if (!result.StartsWith("Yeterli") && !result.StartsWith("Aday") && !result.StartsWith("Kabine"))
        OpenMinistryMarketMenu();   // Menüyü yenile
}

void DoFireMinister(string actorId)
{
    var engine = playerCountry.Engine;
    string result = MinistryMarket.Fire(engine, actorId);
    ui.WriteLog(result);
    UpdateUI();
    OpenMinistryMarketMenu();
}

void DoConcede(PoliticalActor _) { /* placeholder - kullanılmıyor */ }

void DoConcede(Faction faction)
{
    var engine = playerCountry.Engine;
    string result = CandidateManager.ConcedeToFaction(engine, faction.Id);
    ui.WriteLog(result);
    UpdateUI();

    // Menüyü yeniden aç
    OpenCandidateCampaignMenu();
}

public void SelectCountry(int index)
{
    if (index < 0 || index >= availableCountries.Count) return;
    
    var scenarios = Scenario.GetPresets();
    ui.ShowScenarioPicker(scenarios, (scenario) =>
    {
        // EK-22: İsim belirleme
        ui.ShowPlayerNamePrompt((playerName) =>
        {
            PlayerProfile.PlayerName = playerName;
            StartGame(availableCountries[index], scenario);
            var policies = playerCountry.Engine.AllObjects.OfType<SimPolicy>().ToList();
            ui.RefreshPolicyList(policies, ChangePolicyValue, RequestMinisterAdvice);
        });
    });
}

void StartGame(CountryProfile profile, Scenario scenario = null)
{
SessionManager.EndSession();
    HotSeat = null;
// FAZ 5: Oyun istatistiği
if (!LoadSaveOnStart)
    DemocracySim.Engine.Data.GlobalStatsTracker.IncrementGamesPlayed();

    // Senaryo yoksa klasik kullan
    if (scenario == null)
        scenario = Scenario.GetPresets()[0];    
        // FAZ R: Deterministik simülasyon için tohum ayarla
// EK-1 FIX: Sabit tohum 12345 yerine, kayıttan yüklerken gerçek tohumu geri yükle.
// SaveLoadManager yükleme sırasında KapitalistRng.Restore() çağırır.
if (!LoadSaveOnStart)
{
    KapitalistRng.Initialize();   // Yeni oyun → rastgele tohum
    SimLogger.Log($"[GameManager] Yeni oyun tohumu: {KapitalistRng.MasterSeed}");
}
// else: SaveLoadManager zaten KapitalistRng.Restore() çağırdı, burada bir şey yapma.

        // ============================================================
        // 1) DÜNYA VE OYUNCU ÜLKESİNİ OLUŞTUR
        // ============================================================
        world = new WorldManager();
        playerCountry = new Country(profile.id, profile.name, true);
        playerCountry.Engine.IsPlayerCountry = true;

        
    playerCountry.Engine.Legitimacy.SetLegitimacy(scenario.Legitimacy);
    playerCountry.Engine.PoliticalCapital = scenario.PoliticalCapital;
    playerCountry.Engine.Universe.Unrest = scenario.Unrest;
    
    // Veri yükle (objects, actors, effects) — burada objeler yüklendiği için override'lar BURADAN SONRA uygulanmalı
    DataManager.LoadWorld(playerCountry.Engine, "");
    DataManager.AddDefaultDemographics(playerCountry.Engine, profile);
    
    // Objeleri override et
    foreach (var kv in scenario.ObjectOverrides)
{
    // EK-2: Registry O(1) erişim
    var obj = playerCountry.Engine.Registry.Get(kv.Key);
    if (obj != null)
    {
        obj.ActualValue = kv.Value;
        obj.EquilibriumValue = kv.Value;
    }
}
// EK-33: Engine direkt override'lar (Army, Intel vb.)
foreach (var kv in scenario.EngineOverrides)
{
    switch (kv.Key)
    {
        case "ArmySatisfaction":
            playerCountry.Engine.Army.LoadState(
                kv.Value, 
                playerCountry.Engine.Army.MilitaryStrength,
                playerCountry.Engine.Army.LoyaltyToLeader);
            break;
        case "LoyaltyToLeader":
            playerCountry.Engine.Army.LoadState(
                playerCountry.Engine.Army.ArmySatisfaction,
                playerCountry.Engine.Army.MilitaryStrength,
                kv.Value);
            break;
        case "CoupRiskPercent":
            // CoupRiskPercent set edilemiyor (private set), ama LoadState sonrası
            // UpdateArmy çağrılana kadar bekleyelim, o hesaplar
            break;
    }
}
    
    Debug.Log($"[Senaryo] '{scenario.Name}' yüklendi. Zorluk: {scenario.Difficulty}");
        playerCountry.GlobalAlignment = profile.globalAlignment;
        playerCountry.Continent = profile.continent;



        // ============================================================
        // 2) OYUNCU PARTİ SİSTEMİ
        // ============================================================
        playerCountry.Engine.PartyManager.AssignActorsToParties(playerCountry.Engine.Actors);
        playerCountry.Engine.PartyManager.AllocateInitialSeats();
        // FAZ 3.5: Seçmen geçişi sistemini başlat
playerCountry.Engine.VoterTransition.Initialize(
    playerCountry.Engine.Demographics, 
    playerCountry.Engine.PartyManager);

        // ============================================================
        // 3) AI ÜLKELERİ OLUŞTUR
        // ============================================================
        foreach (var profileAI in availableCountries.Where(p => p.id != profile.id))
        {
            Country ai = new Country(profileAI.id, profileAI.name, false);

            // Veri yükle
            DataManager.LoadWorld(ai.Engine, "");
            DataManager.AddDefaultDemographics(ai.Engine, profileAI);

            // Başlangıç değerleri
            ai.GlobalAlignment = profileAI.globalAlignment;
            ai.Continent = profileAI.continent;
            ai.Engine.Legitimacy.SetLegitimacy(profileAI.startingLegitimacy);
            ai.Engine.PoliticalCapital = profileAI.startingCapital;

            // FAZ 2: AI parti sistemi
            ai.Engine.PartyManager.AssignActorsToParties(ai.Engine.Actors);
            ai.Engine.PartyManager.AllocateInitialSeats();

            // FAZ 2.5: AI'ya özgü bakan isimleri ve portföyleri
            RandomizeAIMinisters(ai, profileAI);

            world.Countries.Add(ai);
        }
  // FAZ 3.5: Yerel LLM'i başlat
LLMClient.Initialize();
switch (LLMClient.State)
{
    case LLMClient.LLMState.Ready:   ui.WriteLog("Yerel LLM hazır. Metinler süsleniyor."); break;
    case LLMClient.LLMState.Loading: ui.WriteLog("Yerel LLM yükleniyor; hazır olunca metinler süslenecek."); break;
    case LLMClient.LLMState.Failed:  Debug.LogWarning("[LLM] Model başlatılamadı (model dosyası/port?). Metinler ham kalacak."); break;
    default:                         Debug.LogWarning("[LLM] Sahnede LLMAgent yok (oyun MainMenu'den başlatılmadı?). Metinler ham kalacak."); break;
}

        // ============================================================
        // 4) OYUNCUYU DÜNYAYA EKLE
        // ============================================================
        world.Countries.Add(playerCountry);

        // ============================================================
        // 5) UI VE OYUN DURUMU
        // ============================================================
        ui.ShowPanel("Gameplay");
        ui.PopulateCountryDropdown(world.Countries, playerCountry);
        currentState = GameState.Gameplay;
        UpdateUI();
        ui.WriteLog($"{playerCountry.Name} hükümeti kuruldu. Başarılar Sayın {PlayerProfile.PlayerName}!");

        // FAZ 5: Öğretici
        RunTutorial();

        // Kabine bildirimlerini bağla
        playerCountry.Engine.Cabinet.OnCabinetEvent = (msg, isWarn) => ui.Notify(msg, isWarn);

        // ============================================================
        // 6) OTOMATİK KAYIT
        // ============================================================
                SaveLoadManager.SaveGame(world, playerCountry);
                if (PlayerPrefs.GetInt("StartHotSeat", 0) == 1)
{
    PlayerPrefs.SetInt("StartHotSeat", 0);
    PlayerPrefs.Save();
    StartHotSeatLobby();
}
if (PlayerPrefs.GetInt("StartPBEM", 0) == 1)
{
    PlayerPrefs.SetInt("StartPBEM", 0);
    PlayerPrefs.Save();
    if (_pbemUI != null) _pbemUI.Show();
}
    }

    // =====================================================================
    // 1. KENDİ YASANI YARATMA
    // =====================================================================
    bool RequireGoverning(string what)
    {
        if (playerCountry.Engine.CurrentRole == SimulationEngine.PlayerRole.Governing) return true;
        ui.WriteLog($"Muhalefettesiniz: {what} için hükümette olmalısınız. Seçimi kazanın ya da hükümeti zayıflatın.");
        return false;
    }

    bool SpendCapital(float cost)
    {
        if (playerCountry.Engine.PoliticalCapital < cost)
        {
            ui.WriteLog($"Yeterli sermaye yok ({cost:F0} gerekir).");
            return false;
        }
        playerCountry.Engine.PoliticalCapital -= cost;
        return true;
    }

    public void ProposeCustomPolicy(string name, float alignment, Dictionary<string, float> impacts)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            ui.WriteLog("Yasa adı boş olamaz!");
            return;
        }
        if (playerCountry.Engine.PoliticalCapital < CustomPolicyCost)
        {
            ui.WriteLog("Yeni yasa taslağı oluşturmak için yeterli sermayeniz yok!");
            return;
        }

        playerCountry.Engine.PoliticalCapital -= CustomPolicyCost;

        // Yeni bir SimPolicy objesi oluştur
        string id = "custom" + System.Guid.NewGuid().ToString().Substring(0, 5);
        SimPolicy customPolicy = new SimPolicy(id, name, 0f);
        customPolicy.MinValue = 0f;
        customPolicy.MaxValue = 100f;
        customPolicy.TargetValue = 0f;
        customPolicy.IdeologicalAlignment = Mathf.Clamp(alignment, -100f, 100f);
        customPolicy.GroupImpacts = impacts ?? new Dictionary<string, float>();

        // Söylemleri otomatik ekle (Örn: "Halk İçin" her zaman biraz sola kaydırır)
        customPolicy.Frames["Halk İçin"] = -20f;
        customPolicy.Frames["Devlet İçin"] = 0f;
        customPolicy.Frames["Sermaye İçin"] = 20f;

        playerCountry.Engine.AddObject(customPolicy);
        ui.WriteLog($"📝 Yeni yasa tasarlandı: {name}. Şimdi meclise sunabilirsiniz!");

        // Listeyi güncelle
        var policies = playerCountry.Engine.AllObjects.OfType<SimPolicy>().ToList();
        ui.RefreshPolicyList(policies, ChangePolicyValue, RequestMinisterAdvice);
        UpdateUI(); // sermaye değişti
    }
        

    // =====================================================================
    // 2. LOBİ VE YOLSUZLUK ANLAŞMASI (Sadece siyasi sermaye bitince)
    // =====================================================================
    public void AcceptLobbyOffer(string groupName, float capitalGain, float legitimacyLoss)
    {
        if (playerCountry.Engine.PoliticalCapital >= LobbyCapitalThreshold)
        {
            ui.WriteLog("Henüz sermayeniz yeterli, lobi pazarlığına gerek yok.");
            return;
        }

        playerCountry.Engine.PoliticalCapital += Mathf.Max(0f, capitalGain);
        playerCountry.Engine.Legitimacy.AdjustLegitimacy(-Mathf.Abs(legitimacyLoss));
        playerCountry.Engine.CorruptionLevel = Mathf.Clamp(playerCountry.Engine.CorruptionLevel + 5f, 0f, 100f);

        ui.WriteLog($"💰 {groupName} ile gizli anlaşma yapıldı. Sermaye arttı ama meşruiyet düştü!");
        UpdateUI();
    }

    // =====================================================================
    // 3. BÜROKRASİ (DERİN DEVLET): Yasa geçse bile uygulama yavaşlayabilir
    // =====================================================================
    void ApplyPolicy(SimPolicy policy, float amount)
    {
        policy.IsActive = true;
        // Intensity hiçbir yerde atanmıyordu (varsayılan 0) ve GetEffectiveValue() = ActualValue * Intensity * ImplementationSpeed olduğu için
        // yürürlüğe giren yasa hiçbir etki üretmiyordu.
        if (policy.Intensity <= 0f) policy.Intensity = 1f;
        policy.ActualValue = Mathf.Clamp(policy.ActualValue + amount, 0f, 100f);
    }

    void ProcessPendingPolicies()
    {
        // Bekleyen yasalar artık motorun içinde (Universe.Pending) tutuluyor, böylece kayıtla birlikte saklanır
        var pending = playerCountry.Engine.Universe.Pending;
        for (int i = pending.Count - 1; i >= 0; i--)
        {
            var p = pending[i];
            p.TurnsLeft--;
            if (p.TurnsLeft > 0) continue;

            var policy = playerCountry.Engine.AllObjects.OfType<SimPolicy>().FirstOrDefault(x => x.Id == p.PolicyId);
            if (policy != null)
            {
                ApplyPolicy(policy, p.Amount);
                ui.WriteLog($"📜 {policy.Name} bürokrasiyi aştı ve yürürlüğe girdi.");
            }
            pending.RemoveAt(i);
        }
    }

    // =====================================================================
    // 4. EVREN SİSTEMLERİ: Devlet Menüsü, krizler, mesajlar
    // =====================================================================
    void FlushUniverseMessages()
    {
        var msgs = playerCountry.Engine.Universe.Messages;
        foreach (var m in msgs)
{
    ui.WriteLog(m.Text);
    if (m.IsWarning) ui.Notify(m.Text, true);   // Uyarı değil ama toast için ekleyelim
}
    }

    void ShowPendingCrisis()
    {
        if (playerCountry == null) return;
        var engine = playerCountry.Engine;
        if (engine.CurrentRole == SimulationEngine.PlayerRole.Opposition) return;

        var u = engine.Universe;
        var crisis = u.PendingCrisis;
        if (crisis == null) return;
        u.PendingCrisis = null;

        var options = new List<(string label, System.Action onClick)>();
        foreach (var o in crisis.Options)
        {
            var opt = o; // closure için kopya
            options.Add((opt.Label, () =>
            {
                opt.Effect?.Invoke(engine);
                FlushUniverseMessages();
                UpdateUI();
            }));
        }
        // Kriz penceresi kapatılamaz: oyuncu bir karar vermek zorunda
        ui.ShowChoicePopup($"{crisis.Title}\n{crisis.Description}", options, false);
    }
        // ============================================================
    // FAZ 3: Kriz seçim popup'ı
    // ============================================================
    void ShowCrisisChoicePopup(ActiveCrisis crisis)
    {
        var engine = playerCountry.Engine;
        var choices = engine.CrisisChains.GetCurrentChoices(crisis);
        if (choices == null || choices.Count == 0) return;

        // Ses çal
        AudioManager.Instance?.PlayNotification();

        var options = new List<(string label, System.Action onClick)>();
        foreach (var choice in choices)
        {
            var c = choice;   // closure
            string label = c.Label + "\n" + c.Description;
            options.Add((label, () =>
            {
                engine.CrisisChains.ApplyChoice(crisis.Id, c, engine);
                FlushUniverseMessages();
                UpdateUI();
            }));
        }

        string title = $"{crisis.Title}\nAşama {crisis.CurrentStage + 1}/{crisis.TotalStages}\n\n{crisis.StageDescription}";
        ui.ShowChoicePopup(title, options, false);   // Kapatılamaz — oyuncu karar vermeli
    }

    /// <summary>Her tur çağrılır: aktif krizler için popup göster.</summary>
    void ProcessActiveCrisisPopups()
    {
        var engine = playerCountry.Engine;
        if (engine.CrisisChains == null) return;
        if (engine.CurrentRole == SimulationEngine.PlayerRole.Opposition) return;

        foreach (var crisis in engine.CrisisChains.ActiveCrises)
        {
            // Bu kriz için daha önce popup gösterildi mi?
            if (_shownCrises.Contains(crisis.Id + "_" + crisis.CurrentStage)) continue;

            _shownCrises.Add(crisis.Id + "_" + crisis.CurrentStage);
            ShowCrisisChoicePopup(crisis);
            return;   // Aynı anda tek popup
        }
    }

    // Hangi kriz + aşama çiftleri gösterildi?
    HashSet<string> _shownCrises = new HashSet<string>();

    void ShowInfo(string title, List<string> lines)
    {
        ui.ShowChoicePopup(title + "\n" + string.Join("\n", lines), new List<(string label, System.Action onClick)>(), true, "Kapat");
    }

    void OpenStateMenu()
    {
        if (playerCountry == null) return;
        var engine = playerCountry.Engine;

        // ============================================================
        // MUHALEFET MENÜSÜ
        // ============================================================
        if (engine.CurrentRole == SimulationEngine.PlayerRole.Opposition)
        {
            var oppOptions = new List<(string label, System.Action onClick)>();
                       oppOptions.Add(("Kamuoyu Anketi", () => ShowInfo("Kamuoyu Anketi", UniverseSystems.BuildPoll(engine))));
            oppOptions.Add(("Meclis Gundemi (" + engine.ProposedPolicies.Count + " yasa)", () => OpenLegislativeHub()));
            oppOptions.Add(("Yargi Sistemi", () => OpenJudiciaryMenu()));
            
            // FAZ 2 AŞAMA D: Güvensizlik önergesi
            string ncbLabel = "GÜVENSİZLİK ÖNERGESİ";
            if (engine.PartyManager.IsVoteOfNoConfidenceActive)
                ncbLabel += $" (AKTİF - {engine.PartyManager.NoConfidenceTurnsLeft} tur)";
            else
                ncbLabel += $" ({PartyManager.NoConfidenceCapitalCost:F0} sermaye)";
            
            oppOptions.Add((ncbLabel, () => OpenNoConfidenceMenu()));
            
            oppOptions.Add(("Koalisyonu Sars", () => OpenInciteMenu()));
            oppOptions.Add(("Skandal Kozu (" + ScandalCost.ToString("F0") + " sermaye)", () => DoScandal()));
            oppOptions.Add(("Dunya Diplomasi Paneli", () => OpenWorldPanel()));
            oppOptions.Add(("Kapat", () => { }));

            int oppAntiCount = engine.ActiveAntiCampaigns != null ? engine.ActiveAntiCampaigns.Count : 0;
            ui.ShowChoicePopup(
                "Muhalefet Merkezi\nAktif karsi kampanya: " + oppAntiCount,
                oppOptions);
            return;
        }

        // ============================================================
        // HÜKÜMET MENÜSÜ
        // ============================================================
        var govOptions = new List<(string label, System.Action onClick)>();
       govOptions.Add(("Kamuoyu Anketi", () => ShowInfo("Kamuoyu Anketi", UniverseSystems.BuildPoll(engine))));
govOptions.Add(("💰 EKONOMİ KRİZ YÖNETİMİ", () => OpenEconomyCrisisMenu()));   // FAZ 9
govOptions.Add(("Kabine ve Medya Operasyonlari", () => OpenOperationsMenu()));
govOptions.Add(("Koalisyon Durumu", () => ShowInfo("Koalisyon", UniverseSystems.CoalitionReport(engine))));
govOptions.Add(("Devlet Durumu", () => ShowInfo("Devlet Durumu", UniverseSystems.StatusReport(engine))));
govOptions.Add(("Anayasa Degisikligi", () => OpenConstitutionMenu()));
govOptions.Add(("Gizli Polis / Gozetim", () => OpenSurveillanceMenu()));
govOptions.Add(("Ticaret Anlasmasi", () => OpenTradeMenu()));
govOptions.Add(("Dunya Diplomasi Paneli", () => OpenWorldPanel()));
govOptions.Add(("Kapat", () => { }));

        ui.ShowChoicePopup("Devlet Menusu", govOptions);
    }
void OpenWorldPanel()
{
    if (world == null || playerCountry == null) return;
    var pu = playerCountry.Engine.Universe;

    var report = new List<string>();
    report.Add($"🌍 KÜRESEL GERGİNLİK: %{world.GlobalTension:F1}");
    report.Add($"📈 Küresel büyüme: {world.GlobalGrowth:+0.00;-0.00}   🛢️ Petrol: {world.OilPrice:F0}");
    report.Add($"🧳 Net göç bakiyesi: {pu.MigrationBalance:+0.0;-0.0;0}   🔥 Bulaşma baskısı: {pu.ContagionPressure:F1}");
    report.Add($"🤝 Ticaret ortakları: {pu.TradePartners.Count}");
    report.Add("-----------------------------------");

    foreach (var org in world.Organizations)
    {
        bool isMember = org.MemberIds.Contains(playerCountry.Id);
        string leader = string.IsNullOrEmpty(org.LeaderCountryId) ? "-" : org.LeaderCountryId.ToUpper();
        report.Add($"{org.Name} ({org.Type}) {(isMember ? "[ÜYEYİZ]" : "[ÜYE DEĞİLİZ]")}  Lider: {leader}  Üye: {org.MemberIds.Count}");
        if (!isMember)
        {
            string why = org.JoinBlocker(playerCountry);
            report.Add(why == null ? "   ✅ Katılabilirsiniz" : $"   ⛔ {why}");
        }
    }

    var options = new List<(string label, System.Action onClick)>();
    foreach (var org in world.Organizations)
    {
        var o = org; // closure
        bool member = o.MemberIds.Contains(playerCountry.Id);
        options.Add((member ? $"Örgütten Ayrıl: {o.Name}" : $"Organizasyona Katıl: {o.Name}",
                     () => { if (member) TryLeaveOrganization(o.Id); else TryJoinOrganization(o.Id); }));
    }
    options.Add(("Kapat", () => { }));

    ui.ShowChoicePopup("🌐 DÜNYA DİPLOMASİ PANELİ\n\n" + string.Join("\n", report), options);
}
    /// <summary>FAZ 2 AŞAMA D: Güvensizlik önergesi menüsü.</summary>
    void OpenNoConfidenceMenu()
    {
        var engine = playerCountry.Engine;
        var pm = engine.PartyManager;

        if (pm.IsVoteOfNoConfidenceActive)
        {
            ShowInfo("GÜVENSİZLİK ÖNERGESİ",
                new List<string>
                {
                    $"Önerge aktif! Oylamaya {pm.NoConfidenceTurnsLeft} tur kaldı.",
                    $"Muhalefet desteği: %{pm.CalculateOppositionSupport(engine):F0}",
                    "Bu süre boyunca hükümet baskı altında."
                });
            return;
        }

        string blocker = pm.CanFileNoConfidence(engine);
        if (blocker != null)
        {
            ui.WriteLog($"Önerge verilemez: {blocker}");
            return;
        }

        float support = pm.CalculateOppositionSupport(engine);
        ui.ShowChoicePopup(
            $"GÜVENSİZLİK ÖNERGESİ\n\n" +
            $"Maliyet: {PartyManager.NoConfidenceCapitalCost:F0} sermaye\n" +
            $"Muhalefet desteği: %{support:F0}\n" +
            $"Oylama süresi: {PartyManager.NoConfidenceDuration} tur\n\n" +
            $"Başarılı olursa hükümet düşer, siz iktidara geçersiniz.\n" +
            $"Başarısız olursa prestij kaybedersiniz.",
            new List<(string label, System.Action onClick)>
            {
                ("Önergeyi Ver", () =>
                {
                    string result = pm.FileNoConfidence(engine);
                    ui.WriteLog($"⚖️ {result}");
                    UpdateUI();
                }),
                ("Vazgeç", () => { })
            });
    }

void TryJoinOrganization(string orgId)
{
    var org = world.Organizations.FirstOrDefault(o => o.Id == orgId);
    if (org == null) return;

    if (org.MemberIds.Contains(playerCountry.Id)) { ui.WriteLog($"Zaten {org.Name} üyesisiniz."); return; }

    // FAZ 4: gerçek kriterler (ideoloji + meşruiyet + GSYİH) ve açıklayıcı ret nedeni
    string blocker = org.JoinBlocker(playerCountry);
    if (blocker == null)
    {
        org.Join(playerCountry, world);
        ui.WriteLog($"🎉 TEBRİKLER: {org.Name} üyeliğiniz onaylandı! (aidat: {org.DuesPerTurn:F0} sermaye/tur)");
        UpdateUI();
    }
    else ui.WriteLog($"❌ REDDEDİLDİ: {org.Name} — {blocker}");
}

void TryLeaveOrganization(string orgId)
{
    var org = world.Organizations.FirstOrDefault(o => o.Id == orgId);
    if (org == null || !org.MemberIds.Contains(playerCountry.Id)) return;
    org.Leave(playerCountry, world, "üyelikten kendi isteğinizle ayrıldınız.");
    ui.WriteLog($"🚪 {org.Name} üyeliğinden ayrıldınız (meşruiyet -{org.LeavePenalty:F0}, üyelerle ilişkiler zayıfladı).");
    UpdateUI();
}

    void OpenLegislativeHub()
    {
        var engine = playerCountry.Engine;
        var proposed = engine.ProposedPolicies;

        if (proposed.Count == 0)
        {
            ui.WriteLog("Şu an meclis gündeminde oylanmayı bekleyen bir yasa bulunmuyor.");
            return;
        }

        var options = new List<(string label, System.Action onClick)>();
        
        // Her yasa için durum ve aksiyon butonu
        foreach (var p in proposed)
        {
            var policy = p;
            bool hasCampaign = engine.ActiveAntiCampaigns.ContainsKey(policy.Id);
            
            string status = hasCampaign 
                ? $"KAMPANYA AKTİF ({engine.ActiveAntiCampaigns[policy.Id]} tur)" 
                : "20 Sermaye ile karşı kampanya";
            
            options.Add(($"⚖️ {policy.Name}  —  {status}", () => CampaignAgainst(policy.Id)));
        }

        options.Add(("Vazgeç", () => { }));
        ui.ShowChoicePopup(
            $"MECLİS GÜNDEMİ ({proposed.Count} yasa)\n" +
            $"Her yasaya karşı kampanya başlatarak oy baskısını artırabilirsin.\n" +
            $"Aktif kampanyalar: {engine.ActiveAntiCampaigns.Count}",
            options);
    }


    void CampaignAgainst(string policyId)
    {
        float cost = 20f;
        var engine = playerCountry.Engine;
        string result = engine.StartAntiCampaign(policyId, cost);
        ui.WriteLog($"📢 {result}");
        UpdateUI();
    }




    // =====================================================================
    // KABİNE VE MEDYA OPERASYONLARI (popülist vaat, bakan ikna/etkisizleştirme, propaganda)
    // =====================================================================
    void OpenOperationsMenu()
    {
        ui.ShowChoicePopup("Kabine ve Medya Operasyonları", new List<(string label, System.Action onClick)>
        {
            ($"Popülist Vaat ({PopulistPromiseCost:F0} Sermaye)", () => OpenPopulistMenu()),
            ($"Bakanı İkna Et - oylarda bonus ({PersuadeMinisterCost:F0} Sermaye)", () => OpenMinisterMenu(true)),
            ($"Rakip Bakanı Etkisizleştir ({NeutralizeRivalCost:F0} Sermaye, riskli)", () => OpenMinisterMenu(false)),
            ($"Propaganda Kampanyası ({UniverseSystems.PropagandaCost:F0} Sermaye)", () => DoPropaganda()),
        });
    }

    void OpenPopulistMenu()
    {
        var options = new List<(string label, System.Action onClick)>();
        foreach (var group in playerCountry.Engine.Demographics)
        {
            var g = group; // closure için kopya
            options.Add((g.Name, () => DoPopulistPromise(g)));
        }
        ui.ShowChoicePopup("Hangi gruba büyük vaatler verilsin?", options);
    }

    void DoPopulistPromise(DemographicGroup target)
    {
        if (!SpendCapital(PopulistPromiseCost)) return;
        var engine = playerCountry.Engine;
        engine.GivePopulistPromise(target.Id, 10f);
        // Diğer gruplar kıskanır
        foreach (var g in engine.Demographics.Where(x => x != target)) g.AdjustSatisfaction(-1.5f);
        ui.WriteLog($"📢 {target.Name} grubuna büyük vaatler verildi (+10 memnuniyet). Diğer gruplar kıskandı.");
        UpdateUI();
    }

    void OpenMinisterMenu(bool persuade)
    {
        var options = new List<(string label, System.Action onClick)>();
        foreach (var m in playerCountry.Engine.Actors.Where(a => a.Role == ActorRole.Minister))
        {
            var minister = m; // closure için kopya
            options.Add(($"{minister.Name} (Sadakat %{minister.Loyalty:F0})", () =>
            {
                if (persuade) DoPersuadeMinister(minister); else DoNeutralizeMinister(minister);
            }));
        }
        if (options.Count == 0) { ui.WriteLog("Kabinenizde bakan yok."); return; }
        ui.ShowChoicePopup(persuade ? "Hangi bakan ikna edilsin?" : "Hangi bakan etkisizleştirilsin?", options);
    }

    void DoPersuadeMinister(PoliticalActor minister)
    {
        if (!SpendCapital(PersuadeMinisterCost)) return;
        playerCountry.Engine.PersuadeMinister(minister.Id);
        ui.WriteLog($"🤝 {minister.Name} ikna edildi: sonraki oylamalarda daha destekleyici olacak (etki zamanla azalır).");
        UpdateUI();
    }

    void DoNeutralizeMinister(PoliticalActor minister)
    {
        if (!SpendCapital(NeutralizeRivalCost)) return;
        bool ok = playerCountry.Engine.ManipulateMinister(minister.Id);
        ui.WriteLog(ok
            ? $"🗡️ {minister.Name} etkisizleştirildi: nüfuzu ve hırsı düştü."
            : $"❌ {minister.Name} üzerindeki girişim ifşa oldu! Meşruiyet zarar gördü.");
        UpdateUI();
    }

    void DoPropaganda()
    {
        ui.WriteLog(UniverseSystems.RunPropaganda(playerCountry.Engine));
        UpdateUI();
    }

    void DoScandal()
    {
        if (!SpendCapital(ScandalCost)) return;
        bool ok = playerCountry.Engine.TriggerScandal();
        ui.WriteLog(ok
            ? "📰 Skandal patladı! Hükümetin itibarı sarsıldı, halkın memnuniyeti düştü."
            : "📰 Skandal iddiaları inandırıcı bulunmadı; siyasi sermayeniz eridi.");
        UpdateUI();
    }
    // 1. Veto Menüsü: Aktif yasaları listeler
void OpenVetoMenu()
{
    var engine = playerCountry.Engine;
    var activePolicies = engine.AllObjects.OfType<SimPolicy>().Where(p => p.IsActive).ToList();

    if (activePolicies.Count == 0)
    {
        ui.WriteLog("Veto edilebilecek aktif yasa bulunmuyor.");
        return;
    }

    var options = new List<(string label, System.Action onClick)>();
    foreach (var p in activePolicies)
    {
        var policy = p; // closure için kopya
        options.Add(($"🚫 {policy.Name} (Maliyet: 20)", () => DoVeto(policy.Id)));
    }
    
    options.Add(("Vazgeç", () => { }));
    ui.ShowChoicePopup("Hangi yasayı veto etmek istersiniz?", options);
}

void DoVeto(string policyId)
{
    float cost = 20f;
    if (playerCountry.Engine.VetoPolicy(policyId, cost))
    {
        // Veto sonucunda sermaye zaten engine içinde düşüyor, burada sadece log yazdırıyoruz
        ui.WriteLog($"Operasyon başlatıldı: {policyId} üzerinde baskı kuruldu.");
        UpdateUI();
    }
    else
    {
        ui.WriteLog("Yeterli siyasi sermaye yok veya işlem başarısız oldu!");
    }
}

// 2. Kışkırtma Menüsü: Koalisyon ortaklarını listeler
void OpenInciteMenu()
{
    var u = playerCountry.Engine.Universe;
    var partners = u.Partners.Where(p => p.InGovernment).ToList();

    if (partners.Count == 0)
    {
        ui.WriteLog("Kışkırtılabilecek bir koalisyon ortağı bulunmuyor.");
        return;
    }

    var options = new List<(string label, System.Action onClick)>();
    foreach (var p in partners)
    {
        var partner = p; // closure için kopya
        options.Add(($"🔥 {partner.Name} (Maliyet: 15)", () => DoIncite(partner.Id)));
    }

    options.Add(("Vazgeç", () => { }));
    ui.ShowChoicePopup("Hangi ortağı hükümete karşı kışkırtacaksınız?", options);
}

void DoIncite(string partnerId)
{
    float cost = 15f;
    if (playerCountry.Engine.InciteCoalitionPartner(partnerId, cost))
    {
        ui.WriteLog($"Gizli görüşmeler yapıldı: {partnerId} hükümetten soğutuluyor.");
        UpdateUI();
    }
    else
    {
        ui.WriteLog("Yeterli siyasi sermaye yok!");
    }
}
    // =====================================================================
    // FAZ 3 ADIM 5: Yargı Sistemi
    // =====================================================================
    void OpenJudiciaryMenu()
    {
        if (playerCountry == null || gameOver) return;
        var engine = playerCountry.Engine;

        if (engine.CurrentRole != SimulationEngine.PlayerRole.Opposition)
        {
            ui.WriteLog("Yargi davalari sadece muhalefetteyken acilabilir.");
            return;
        }

        var options = new List<(string label, System.Action onClick)>();

        if (engine.Judiciary.CanFileCase(engine))
        {
            string constitutionalLabel = "Anayasa Davasi - Yasa iptali (" + JudicialSystem.CaseFilingCost.ToString("F0") + " sermaye)";
            string corruptionLabel = "Yolsuzluk Davasi - Bakan yargilama (" + JudicialSystem.CaseFilingCost.ToString("F0") + " sermaye)";
            string pressLabel = "Basin Davasi - Gazeteci koruma (" + JudicialSystem.CaseFilingCost.ToString("F0") + " sermaye)";

            options.Add((constitutionalLabel, () => OpenConstitutionalCaseMenu()));
            options.Add((corruptionLabel, () => OpenCorruptionCaseMenu()));
            options.Add((pressLabel, () => DoFilePressCase()));
        }
        else
        {
            string blockLabel = "Dava (uygun degil: " + engine.Judiciary.GetBlockReason(engine) + ")";
            options.Add((blockLabel, () => { }));
        }

        options.Add(("Yargi Durumu", () => ShowJudicialStatus()));
        options.Add(("Vazgec", () => { }));

        string title = "YARGI SISTEMI\n" + engine.Judiciary.GetStatusText()
            + "\nYargi Bagimsizligi: %" + engine.Judiciary.JudicialIndependence.ToString("F0");
        ui.ShowChoicePopup(title, options);
    }

    void OpenConstitutionalCaseMenu()
    {
        var engine = playerCountry.Engine;
        var activePolicies = engine.AllObjects.OfType<SimPolicy>().Where(p => p.IsActive).ToList();

        if (activePolicies.Count == 0)
        {
            ui.WriteLog("Iptal edilecek aktif yasa yok.");
            return;
        }

        var options = new List<(string label, System.Action onClick)>();
        foreach (var p in activePolicies)
        {
            var policy = p;
            string label = policy.Name + " (Ideoloji: " + policy.IdeologicalAlignment.ToString("F0") + ")";
            options.Add((label, () => DoFileConstitutionalCase(policy.Id)));
        }
        options.Add(("Vazgec", () => { }));

        ui.ShowChoicePopup("ANAYASA DAVASI\nHangi yasanin iptalini isteyeceksin?", options);
    }

    void OpenCorruptionCaseMenu()
    {
        var engine = playerCountry.Engine;
        var ministers = engine.Actors.Where(a => a.Role == ActorRole.Minister).ToList();

        if (ministers.Count == 0)
        {
            ui.WriteLog("Yargilanacak bakan yok.");
            return;
        }

        var options = new List<(string label, System.Action onClick)>();
        foreach (var m in ministers)
        {
            var minister = m;
            string label = minister.Name + " (Sadakat %" + minister.Loyalty.ToString("F0") + ")";
            options.Add((label, () => DoFileCorruptionCase(minister.Id)));
        }
        options.Add(("Vazgec", () => { }));

        ui.ShowChoicePopup("YOLSUZLUK DAVASI\nHangi bakani yargilatmak istersin?", options);
    }

    void DoFileConstitutionalCase(string policyId)
    {
        string result = playerCountry.Engine.Judiciary.FileCase(CaseType.Constitutional, policyId, playerCountry.Engine);
        ui.WriteLog("Yargi: " + result);
        UpdateUI();
    }

    void DoFileCorruptionCase(string actorId)
    {
        string result = playerCountry.Engine.Judiciary.FileCase(CaseType.Corruption, actorId, playerCountry.Engine);
        ui.WriteLog("Yargi: " + result);
        UpdateUI();
    }

    void DoFilePressCase()
    {
        string result = playerCountry.Engine.Judiciary.FileCase(CaseType.Press, "", playerCountry.Engine);
        ui.WriteLog("Yargi: " + result);
        UpdateUI();
    }

    void ShowJudicialStatus()
    {
        var j = playerCountry.Engine.Judiciary;
        var lines = new List<string>
        {
            "Yargi Bagimsizligi: %" + j.JudicialIndependence.ToString("F0"),
            "Durum: " + j.GetStatusText()
        };

        lines.Add("-- Yuksek Mahkeme --");
        foreach (var judge in j.Judges)
        {
            lines.Add(judge.Name + " (" + judge.Ideology.ToString() + ")");
        }

        ShowInfo("YARGI DURUMU", lines);
    }
    // =====================================================================
    // FAZ 3 ADIM 4: Protesto Düzenleme
    // =====================================================================
    void OpenProtestMenu()
    {
        if (playerCountry == null || gameOver) return;
        var engine = playerCountry.Engine;

        if (engine.CurrentRole != SimulationEngine.PlayerRole.Opposition)
        {
            OpenRallyMenu();
            return;
        }

        var options = new List<(string label, System.Action onClick)>();

        if (engine.Protest.CanStartProtest(engine))
        {
            string protestLabel = "Protesto Duzenle - " + ProtestManager.CapitalCost.ToString("F0") + " sermaye + " + ProtestManager.FundCost.ToString("F0") + " fon";
            options.Add((protestLabel, () => OpenProtestThemeMenu()));
        }
        else
        {
            string blockLabel = "Protesto (uygun degil: " + engine.Protest.GetBlockReason(engine) + ")";
            options.Add((blockLabel, () => { }));
        }

        options.Add(("Miting Duzenle (kampanya yatirimi)", () => OpenRallyMenu()));
        options.Add(("Vazgec", () => { }));

        string title = "MUHALEFET AKSIYONLARI\n" + engine.Protest.GetStatusText();
        ui.ShowChoicePopup(title, options);
    }

    void OpenProtestThemeMenu()
    {
        var options = new List<(string label, System.Action onClick)>();
        options.Add(("Ekonomik - Isci haklari, enflasyon", () => DoStartProtest(ProtestTheme.Economic)));
        options.Add(("Ozgurluk - Basin, ifade ozgurlugu", () => DoStartProtest(ProtestTheme.Freedom)));
        options.Add(("Adalet - Yolsuzluk, hukuk", () => DoStartProtest(ProtestTheme.Justice)));
        options.Add(("Vazgec", () => { }));

        ui.ShowChoicePopup("PROTESTO TEMASI SEC\nHer temanin farkli etkileri var.", options);
    }

    void DoStartProtest(ProtestTheme theme)
    {
        var engine = playerCountry.Engine;
        string result = engine.Protest.StartProtest(theme, engine);
        ui.WriteLog("Protesto: " + result);
        ui.Notify("Protesto basladi: " + ProtestManager.ThemeTurkish(theme), false);
        UpdateUI();
    }
    
    void OpenRallyMenu()
    {
        if (playerCountry == null || gameOver) return;
        var engine = playerCountry.Engine;
        float cost = engine.CurrentRole == SimulationEngine.PlayerRole.Governing ? RallyCostGoverning : RallyCostOpposition;
        if (engine.PoliticalCapital < cost)
        {
            ui.WriteLog($"Miting için {cost:F0} sermaye gerekir!");
            return;
        }
        ui.ShowRallyPanel(engine.Demographics, cost, DoRally);
    }

    void DoRally(string groupId, string theme)
    {
        var engine = playerCountry.Engine;
        var group = engine.Demographics.FirstOrDefault(x => x.Id == groupId);
        if (group == null) return;

        bool governing = engine.CurrentRole == SimulationEngine.PlayerRole.Governing;
        float cost = governing ? RallyCostGoverning : RallyCostOpposition;
        if (!SpendCapital(cost)) return;

        var u = engine.Universe;
        float invest = cost;
        switch (theme)
        {
            case "Öfke":
                if (governing)
                {
                    group.AdjustSatisfaction(10f);
                    foreach (var g in engine.Demographics.Where(x => x != group)) g.AdjustSatisfaction(-1.5f);
                }
                else group.AdjustSatisfaction(-8f); // muhalefette öfke, hükümete duyulan memnuniyeti düşürür
                u.Unrest = Mathf.Clamp(u.Unrest + 5f, 0f, 100f);
                break;
            case "Güven":
                if (governing)
                {
                    group.AdjustSatisfaction(4f);
                    engine.Legitimacy.AdjustLegitimacy(3f);
                }
                else invest *= 1.3f;
                break;
            default: // Umut
                if (governing) group.AdjustSatisfaction(6f);
                u.Unrest = Mathf.Clamp(u.Unrest - 3f, 0f, 100f);
                break;
        }

        // Her miting seçim kampanyasına yatırımdır; seçim günü ElectionManager bunu okur
            float stageMultiplier = engine.Campaign.GetStageMultiplier();
    float finalInvest = invest * stageMultiplier;
engine.Campaign.InvestInGroup(group.Id, finalInvest);
        float imageGain = theme switch
        {
            "Umut"  => 2f,   // Pozitif mesaj, orta etki
            "Öfke"  => 3f,   // Agresif ama etkili
            "Güven" => 4f,   // En etkili, güven veriyor
            _       => 2f
        };
        engine.Elections.AdjustLeaderImage(imageGain);
        ui.WriteLog($"📸 Lider imajı +{imageGain:F0} (şu an: %{engine.Elections.LeaderImage:F0})");


     u.CampaignInvestments = new Dictionary<string, float>(engine.Campaign.CampaignInvestments);
        
        ui.Notify($"Miting düzenlendi! Hedef: {group.Name} ({theme})");
        ui.WriteLog($"🎤 {group.Name} için '{theme}' temalı miting yapıldı. Seçim kampanyasına yatırım eklendi.");
        UpdateUI();
    }

    void OpenConstitutionMenu()
    {
        ui.ShowChoicePopup("Anayasa Değişikliği", new List<(string label, System.Action onClick)>
        {
            ($"Seçim süresini uzat (+8 tur) - {UniverseSystems.ElectionExtensionCost:F0} Sermaye", () => DoAmendment(0)),
            ($"Başkanlık yetkileri (oylamayı atla) - {UniverseSystems.PresidentialPowersCost:F0} Sermaye", () => DoAmendment(1)),
        });
    }

    void DoAmendment(int type)
    {
        ui.WriteLog(UniverseSystems.Amend(playerCountry.Engine, type));
        UpdateUI();
    }

    void OpenSurveillanceMenu()
    {
        ui.ShowChoicePopup($"Gizli Polis / Gözetim (Şu an: %{playerCountry.Engine.Universe.SurveillanceLevel:F0})", new List<(string label, System.Action onClick)>
        {
            ($"Gözetimi artır (+20) - {UniverseSystems.SurveillanceCost:F0} Sermaye", () => DoSurveillance(20f)),
            ("Gözetimi azalt (-20)", () => DoSurveillance(-20f)),
        });
    }

    void DoSurveillance(float delta)
    {
        ui.WriteLog(UniverseSystems.ChangeSurveillance(playerCountry.Engine, delta));
        UpdateUI();
    }

    void OpenTradeMenu()
    {
        var options = new List<(string label, System.Action onClick)>();
        foreach (var c in world.Countries.Where(x => x != playerCountry))
        {
            var country = c; // closure için kopya
            bool signed = playerCountry.Engine.Universe.TradePartners.Contains(country.Id);
            options.Add((signed ? $"{country.Name} (anlaşma var)" : $"{country.Name} - {UniverseSystems.TradeAgreementCost:F0} Sermaye",
                () => DoTrade(country)));
        }
        ui.ShowChoicePopup("Ticaret Anlaşması", options);
    }

    void DoTrade(Country target)
    {
        ui.WriteLog(UniverseSystems.SignTradeAgreement(playerCountry.Engine, target.Id, target.Name));
        UpdateUI();
    }

    // =====================================================================
    // MENÜLER: Yeni Yasa ve Lobi
    // =====================================================================
    void OpenCustomPolicyMenu()
    {
        if (playerCountry == null) return;
        if (!RequireGoverning("yeni yasa tasarlamak")) return;
        if (playerCountry.Engine.PoliticalCapital < CustomPolicyCost)
        {
            ui.WriteLog($"Yeni yasa taslağı için {CustomPolicyCost:F0} sermaye gerekir!");
            return;
        }
        ui.ShowChoicePopup($"Yasa Yönelimi (Maliyet: {CustomPolicyCost:F0} Sermaye)", new List<(string label, System.Action onClick)>
        {
            ("Sol (Halk Odaklı)", () => PickCustomPolicyFocus(-60f, "Sol")),
            ("Merkez (Dengeli)",  () => PickCustomPolicyFocus(0f, "Merkez")),
            ("Sağ (Piyasa Odaklı)", () => PickCustomPolicyFocus(60f, "Sağ")),
        });
    }

    void PickCustomPolicyFocus(float alignment, string orientationLabel)
    {
        var options = new List<(string label, System.Action onClick)>();
        foreach (var group in playerCountry.Engine.Demographics)
        {
            var g = group; // closure için kopya
            options.Add((g.Name, () => CreateCustomPolicyFor(alignment, orientationLabel, g.Id, g.Name)));
        }
        ui.ShowChoicePopup("Yasa Kimin İçin?", options);
    }

    void CreateCustomPolicyFor(float alignment, string orientationLabel, string focusGroupId, string focusGroupName)
    {
        // Hedef grup memnun olur; ideolojinin karşı kutbundaki grup tepki gösterir
        var impacts = new Dictionary<string, float> { { focusGroupId, 8f } };
        if (alignment < 0f)
        {
            impacts["capitalists"] = focusGroupId == "capitalists" ? 8f : -4f;
            impacts["conservatives"] = focusGroupId == "conservatives" ? 8f : -2f;
        }
        else if (alignment > 0f)
        {
            impacts["workers"] = focusGroupId == "workers" ? 8f : -4f;
            impacts["intellectuals"] = focusGroupId == "intellectuals" ? 8f : -2f;
        }

        int number = playerCountry.Engine.AllObjects.OfType<SimPolicy>().Count(p => p.Id.StartsWith("custom")) + 1;
        ProposeCustomPolicy($"{orientationLabel} {focusGroupName} Yasası #{number}", alignment, impacts);
    }

      void OpenLobbyMenu()
    {
        if (playerCountry == null) return;

        var engine = playerCountry.Engine;
        var options = new List<(string label, System.Action onClick)>();

        // Muhalefet: bağış kampanyası + devlet yardımı
        if (engine.CurrentRole == SimulationEngine.PlayerRole.Opposition)
        {
            if (engine.Party.CanCollectDonation)
            {
                options.Add(($"Bağış Kampanyası: +40 Fon / -5 Meşruiyet", () =>
                {
                    ui.WriteLog(engine.Party.CollectDonation(engine));
                    UpdateUI();
                }));
            }
            else
            {
                options.Add(("Bağış Kampanyası (bekleme süresi)", () => { }));
            }

            options.Add(($"Devlet Yardımı Al (sandalye başı +0.5)", () =>
            {
                ui.WriteLog(engine.Party.ApplyStateSubsidy(engine));
                UpdateUI();
            }));

            options.Add(("Kapat", () => { }));
            ui.ShowChoicePopup(
                $"MUHALEFET FONU\n{engine.Party.GetStatusText()}\n\nBağış kampanyası veya devlet yardımı ile fonunu artır.",
                options);
        }
        // Hükümet: eski lobi sistemi
        else
        {
            if (engine.PoliticalCapital >= LobbyCapitalThreshold)
            {
                ui.WriteLog("Henüz sermayeniz yeterli, lobi pazarlığına gerek yok.");
                return;
            }
            ui.ShowChoicePopup("Lobi Teklifleri (Meşruiyet satılır)", new List<(string label, System.Action onClick)>
            {
                ("Sermaye Lobisi: +40 Sermaye / -15 Meşruiyet", () => AcceptLobbyOffer("Sermaye Lobisi", 40f, 15f)),
                ("Enerji Şirketleri: +30 Sermaye / -10 Meşruiyet", () => AcceptLobbyOffer("Enerji Şirketleri", 30f, 10f)),
                ("Silah Üreticileri: +50 Sermaye / -20 Meşruiyet", () => AcceptLobbyOffer("Silah Üreticileri", 50f, 20f)),
            });
        }
    }

    // =====================================================================
    // FAZ 5: Öğretici ve küçük demo
    // =====================================================================
    void RunTutorial()
    {
        if (playerCountry == null) return;
        TutorialManager.Check(playerCountry.Engine, (msg, warn) => { ui.Notify(msg, warn); ui.WriteLog(msg); });
    }

    /// <summary>Ana menüdeki "Demo Oyun" butonunun OnClick'ine bağlayın. 20 turluk, 5 hedefli kısa senaryo.</summary>
    public void StartDemo()
    {
        if (availableCountries == null || availableCountries.Count == 0) return;
        var profile = availableCountries.FirstOrDefault(p => p.id == "tur") ?? availableCountries[0];
        UnityEngine.PlayerPrefs.SetInt("Tutorial", 1);
        StartGame(profile);
        var u = playerCountry.Engine.Universe;
        u.DemoMode = true;
        u.DemoStartTurn = playerCountry.Engine.CurrentTurn;
        u.DemoResult = null;
        TutorialManager.Reset(playerCountry.Engine);
        ui.WriteLog(LocalizationManager.Get("demo_start"));
        ui.WriteLog(DemoScenario.ProgressText(playerCountry.Engine));
    }

    bool CheckDemoEnd()
    {
        var e = playerCountry.Engine;
        var u = e.Universe;
        if (!u.DemoMode || u.DemoResult != null) return false;

        int elapsed = e.CurrentTurn - u.DemoStartTurn;
        if (elapsed < DemoScenario.Length)
        {
            if (elapsed > 0 && elapsed % 5 == 0) ui.WriteLog(DemoScenario.ProgressText(e));
            return false;
        }

        bool won = DemoScenario.AllGoalsMet(e);
        u.DemoResult = won ? "win" : "lose";
        gameOver = true;
        if (ui.nextTurnButton != null) ui.nextTurnButton.interactable = false;
        string text = won ? LocalizationManager.Get("demo_win", elapsed) : LocalizationManager.Get("demo_lose");
        ui.ShowChoicePopup(text + "\n\n" + DemoScenario.ProgressText(e),
            new List<(string label, System.Action onClick)>
            {
                (LocalizationManager.Get("new_game"), () => { LoadSaveOnStart = false; SceneManager.LoadScene(SceneManager.GetActiveScene().name); }),
                ("Ana Menü", () => SceneManager.LoadScene(MainMenuSceneName)),
            }, false);
        return true;
    }

    public void LoadGame()
    {
        var (loadedWorld, loadedPlayer) = SaveLoadManager.LoadGame(availableCountries);
        if (loadedWorld == null || loadedPlayer == null) { CreateCountryButtons(); ui.ShowPanel("Selection"); return; }
        world = loadedWorld;
        playerCountry = loadedPlayer;
        ui.ShowPanel("Gameplay");
        ui.PopulateCountryDropdown(world.Countries, playerCountry);
        currentState = GameState.Gameplay;
        var policies = playerCountry.Engine.AllObjects.OfType<SimPolicy>().ToList();
        ui.RefreshPolicyList(policies, ChangePolicyValue, RequestMinisterAdvice);
        UpdateUI();
        playerCountry.Engine.Cabinet.OnCabinetEvent = (msg, isWarn) => ui.Notify(msg, isWarn);

        // Kampanya yatırımları evren durumuyla birlikte kaydedildi: geri yükle
        var camp = playerCountry.Engine.Campaign;
        camp.CampaignInvestments.Clear();
        foreach (var kv in playerCountry.Engine.Universe.CampaignInvestments) camp.CampaignInvestments[kv.Key] = kv.Value;
        camp.IsCampaignActive = camp.CampaignInvestments.Count > 0;

        ui.WriteLog("Kayıtlı oyun yüklendi.");
    }

    public void NextTurn()
    {
               if (gameOver) return;
               // Hot-Seat / PBEM modunda sıra yönetimi
// Hot-Seat / PBEM modunda sıra yönetimi
// NOT: HotSeat null olabilir (eski oturum kalıntısı) — null check zorunlu
if (SessionManager.ActiveSession != null 
    && !SessionManager.ActiveSession.IsSingle 
    && HotSeat != null)
{
    if (SessionManager.ActiveSession.IsHotSeat)
    {
        HotSeat.RequestNextPlayer();
        return;
    }
    else if (SessionManager.ActiveSession.IsPBEM)
    {
        HotSeat.RequestNextPlayer();
        SessionManager.SaveSession("pbem_turn.json");
        ui.WriteLog("💾 Oturum kaydedildi. Bu dosyayı sıradaki oyuncuya gönderin: pbem_turn.json");
        return;
    }
}
else if (SessionManager.ActiveSession != null && !SessionManager.ActiveSession.IsSingle && HotSeat == null)
{
    // Eski oturum kalıntısı — temizle
    Debug.LogWarning("[GameManager] Eski oturum kalıntısı temizlendi.");
    SessionManager.EndSession();
}

        // AI ülkeler ve küresel olaylar (oyuncunun ülkesini işlemez)
        world.ProcessWorldTurn();

        var simEngine = playerCountry.Engine;   // ← EN BAŞA TAŞINDI

        // FAZ 2: Koalisyon ortakları çekilirse erken seçim tetikle
        var pm = simEngine.PartyManager;
        if (simEngine.CurrentRole == SimulationEngine.PlayerRole.Governing &&
            !pm.PlayerHasMajority() && !pm.HasCoalitionMajority())
        {
            // Çoğunluk yok ama hala iktidar → kaçak hükümet
            if (simEngine.TurnUntilElection > 3)
            {
                simEngine.Legitimacy.AdjustLegitimacy(-5f);
                ui.WriteLog("⚠️ Çoğunluğunuz yok! Erken seçim yaklaşıyor.");
                simEngine.TurnUntilElection = Math.Min(simEngine.TurnUntilElection, 3);
            }
        }

        // Meclis gündemindeki yasaları oyla
          if (simEngine.ProposedPolicies.Count > 0)
    {
        // FAZ 0 FIX: Sabit 10f yerine gerçek muhalefet baskısı hesaplanıyor
        float dynamicPressure = simEngine.CalculateOppositionPressure();
        
        if (dynamicPressure > 5f)
            ui.WriteLog($"📊 Muhalefet baskısı: %{dynamicPressure:F0}");
        
        foreach (var p in new List<SimPolicy>(simEngine.ProposedPolicies))
        {
            simEngine.ResolveVote(p.Id, dynamicPressure);
            bool isPassed = p.IsActive || simEngine.Universe.Pending.Any(x => x.PolicyId == p.Id);
            AIDecisionExplainer.GeneratePolicyPublicReaction(p, isPassed, simEngine, reaction =>
            {
                if (!string.IsNullOrEmpty(reaction)) ui.WriteLog(reaction);
            });
        }
    }

        ProcessPendingPolicies(); // Bürokrasideki yasaları ilerlet
        GameEvent currentEvent = simEngine.ProcessTurn();

        // ⬇️⬇️ YENİ (C.5): Kampanya aşamasını ilerlet ⬇️⬇️
        if (simEngine.Campaign != null && simEngine.Campaign.IsCampaignActive)
        {
            simEngine.Campaign.AdvanceTurn();
        }
        // ⬆️⬆️ YENİ ⬆️⬆️


        // FAZ 5: İstatistik güncelle
DemocracySim.Engine.Data.GlobalStatsTracker.TryUpdateMaxGDP(
    playerCountry.Engine.Registry.GetValue(ObjectRegistry.Ids.Gdp, 0f));
DemocracySim.Engine.Data.GlobalStatsTracker.TryUpdateMaxTurnsSingle(world.GlobalTurn);
DemocracySim.Engine.Data.GlobalStatsTracker.TryUpdateBestLegitimacy(
    playerCountry.Engine.Legitimacy.CurrentLegitimacy);

        UpdateUI();
        FlushUniverseMessages();
        if (CheckGameOver()) return;
        if (CheckDemoEnd()) return;   // FAZ 5

                if (currentEvent != null)
        {
            var eventId = currentEvent.Id;   // Closure icin kopya
            ui.DisplayEvent(simEngine, currentEvent, (choice) =>
            {
                choice?.Effect?.Invoke(simEngine);
                if (choice != null) ui.WriteLog($"Karar: {choice.ResultText}");

                // FAZ 2 Adim 4: Bu olayin zincir devami varsa planla
                if (simEngine.EventSys.HasChain(eventId))
                {
                    simEngine.EventSys.ScheduleChainFromEvent(eventId);
                    ui.WriteLog($"(Bu olayin devami yakinda gelecek...)");
                }

                UpdateUI();
            });
        }

             ui.WriteLog($"Tur {world.GlobalTurn} tamamlandı.");
        RunTutorial();
        ShowPendingCrisis();
        ProcessActiveCrisisPopups();   // FAZ 3: Kriz seçim popup'ı

        if (simEngine.TurnUntilElection <= 0)
        {
            HandleElectionEnd();
        }
        simEngine.LogDecision("TUR", $"Tur {world.GlobalTurn} tamamlandı", 0f);

        SaveLoadManager.SaveGame(world, playerCountry);

// FAZ R: Pipeline performans raporu (sadece editor'de)
#if UNITY_EDITOR
var report = simEngine.GetPipelinePerformanceReport();
if (report.Count > 0 && Time.frameCount % 10 == 0)   // Her 10 turda bir
{
    var sb = new System.Text.StringBuilder("[Pipeline] En yavaş sistemler:\n");
    foreach (var line in report.Take(3)) sb.AppendLine("  " + line);
    Debug.Log(sb.ToString());
}
#endif
    if (ui.nextTurnButton != null && !gameOver) ui.nextTurnButton.interactable = true;
    }

        private void HandleElectionEnd()
{
    var engine = playerCountry.Engine;
    bool wasGoverning = engine.CurrentRole == SimulationEngine.PlayerRole.Governing;

    // EK-24: Adaylık kontrolü — kriz içindeyse ve destek yetersizse oyun biter
    string candidacyBlock = CandidateManager.CheckCandidacyAtElection(engine);
    if (candidacyBlock != null)
    {
        engine.Universe.GameOverReason = candidacyBlock;
        CheckGameOver();   // Oyun sonu tetikle
        return;
    }

    bool won = engine.Elections.RunElection(engine, engine.PartyManager);
    UniverseSystems.ApplyElectionOutcome(engine, wasGoverning, won);
    // ... (rest aynı)
}

    // Devrim / iflas gibi oyun sonu koşulları
  private bool CheckGameOver()
{
    string reason = playerCountry.Engine.Universe.GameOverReason;
    if (reason == null) return false;

    gameOver = true;
    ui.WriteLog(reason);
    SaveLoadManager.DeleteSave();
    if (ui.nextTurnButton != null) ui.nextTurnButton.interactable = false;

    // FAZ 4: Ending değerlendirmesi
    var ending = EndingManager.Evaluate(playerCountry.Engine, world.GlobalTurn);
    
    // Ending'i log'a da yaz
    ui.WriteLog($"═══════════════════════════════");
    ui.WriteLog($"  {ending.Title}");
    ui.WriteLog($"═══════════════════════════════");
    ui.WriteLog(ending.Description);
    
    // Ending modal'ını göster
   // FAZ 4: Ending modal'ı doğrudan GameManager'da aç (mimari temiz)
var options = new List<(string label, System.Action onClick)>
{
    ("Yeni Oyun", () => { LoadSaveOnStart = false; SceneManager.LoadScene(SceneManager.GetActiveScene().name); }),
    ("Ana Menüye Dön", () => SceneManager.LoadScene(MainMenuSceneName)),
};
string tags = string.Join(" • ", ending.Tags);
string title = $"OYUN SONU: {ending.Title}\n\n{ending.Description}\n\n[{tags}]";
ui.ShowChoicePopup(title, options, false);
return true;
}

    void RequestMinisterAdvice(string policyId)
    {
        var minister = playerCountry.Engine.Actors.FirstOrDefault(a => a.Role == ActorRole.Minister);
        if (minister == null) { ui.WriteLog("Kabinenizde tavsiye verecek bir bakan yok."); return; }
        OnAskMinisterAdvice(minister.Id, policyId);
    }

    public void OnAskMinisterAdvice(string actorId, string policyId)
    {
        var policy = playerCountry.Engine.AllObjects.OfType<SimPolicy>().FirstOrDefault(p => p.Id == policyId);
        if (policy == null) return;
        var advice = playerCountry.Engine.GetMinisterAdvice(actorId, policy);
        ui.WriteLog($"Tavsiye: {advice.advice} (Güven: %{advice.confidence * 100:F0})");
    }

    public void OnStartSecretOp(int opTypeIndex)
    {
        string targetName = ui.GetSelectedCountryName();
        var target = world.Countries.FirstOrDefault(c => c.Name == targetName);
        if (target != null)
        {
            bool success = playerCountry.Engine.PerformSecretOperation(target.Id, (OperationType)opTypeIndex, world);
            string detail = success switch {
                true when opTypeIndex == 0 => $"{target.Name} gizli verileri ele geçirildi!",
                true when opTypeIndex == 1 => $"{target.Name} altyapı sabotajı yapıldı!",
                true when opTypeIndex == 2 => $"{target.Name} halkı manipüle edildi!",
                _ => $"OPERASYON İFŞA OLDU! {target.Name} ile ilişkiler bozuldu."
            };
            ui.WriteLog(detail);
            RefreshDiplomacyInfo();
        }
    }

    public void ChangePolicyValue(string policyId, float amount)
    {
        if (!RequireGoverning("yasa önermek")) return;
        float cost = 10f;
        if (playerCountry.Engine.PoliticalCapital < cost) { ui.WriteLog("Yeterli sermaye yok!"); return; }
        var policy = playerCountry.Engine.AllObjects.OfType<SimPolicy>().FirstOrDefault(p => p.Id == policyId);
        if (policy != null)
        {
            ui.ShowFramePicker(policy.Name, amount, (frame) => ProposePolicy(policyId, amount, frame));
        }
    }
    /// <summary>FAZ 23.1: Ana menüden çağrılır — Hot-Seat lobisi açar.</summary>
public void StartHotSeatLobby()
{
    if (_hotSeatUI == null)
    {
        Debug.LogError("[HotSeat] UI panel yok!");
        return;
    }

    var countries = availableCountries.Select(c => c.name).ToList();
    _hotSeatUI.ShowLobby(2, countries, OnHotSeatSessionCreated);
}

private void OnHotSeatSessionCreated(SessionData session)
{
    // Seed'i uygula
    KapitalistRng.Initialize(session.MasterSeed);

    // Tüm oyuncuların CountryName → CountryId eşleşmesini düzelt
    foreach (var slot in session.Players)
    {
        var prof = availableCountries.FirstOrDefault(c =>
            c.name == slot.CountryName ||
            c.id == slot.CountryId ||
            c.name == slot.CountryId ||
            c.id == slot.CountryName);

        if (prof != null)
        {
            slot.CountryId = prof.id;
            slot.CountryName = prof.name;
        }
        else
        {
            Debug.LogError($"[HotSeat] Profil bulunamadı: {slot.CountryName} / {slot.CountryId}");
        }
    }

    var firstPlayer = session.Players[0];
    var profile = availableCountries.FirstOrDefault(c => c.id == firstPlayer.CountryId);

    if (profile == null)
    {
        Debug.LogError($"[HotSeat] İlk oyuncunun ülkesi bulunamadı: {firstPlayer.CountryName}");
        return;
    }

    // Oyunu başlat
    var scenario = DemocracySim.Engine.World.Scenario.GetPresets()[0];
    StartGame(profile, scenario);

    // Hot-Seat koordinatörünü başlat
    SessionManager.StartTurnCoordination(world);
    HotSeat = new HotSeatController(world);

    // Pass screen gösterildiğinde UI aç
    HotSeat.OnShowPassScreen += (nextPlayer) =>
    {
        _hotSeatUI.ShowPassScreen(nextPlayer, () =>
        {
            // Oyuncu "HAZIRIM" tıkladı → HotSeat'e bildir
            HotSeat.ConfirmPlayerReady();
        });
    };

    // Sıra başladığında UI'ı yeni oyuncuya göre tazele
    HotSeat.OnTurnStarted += (player) =>
    {
        OnHotSeatTurnStart(player);
    };

    HotSeat.OnRoundCompleted += (turn) =>
    {
        ui.WriteLog($"=== Tur {turn} tamamlandı, yeni tur başlıyor ===");
    };

    // İlk oyuncunun pass screen'ini göster
    _hotSeatUI.ShowPassScreen(session.Players[0], () =>
    {
        HotSeat.ConfirmPlayerReady();
    });
}

// =====================================================================
// FAZ 23.1: Hot-Seat — sıradaki oyuncunun turunu başlat
// =====================================================================
private void OnHotSeatTurnStart(PlayerSlot player)
{
    if (player == null)
    {
        Debug.LogError("[HotSeat] OnHotSeatTurnStart: player null!");
        return;
    }

    // ─────────────────────────────────────────────────────────────
    // 1) Oyuncunun ülkesini bul (ID / Name arasında esnek eşleşme)
    // ─────────────────────────────────────────────────────────────
    var foundCountry = world.Countries.FirstOrDefault(c =>
        c.Id == player.CountryId ||
        c.Name == player.CountryName ||
        c.Name == player.CountryId ||
        c.Id == player.CountryName ||
        c.Name.Equals(player.CountryId,   StringComparison.OrdinalIgnoreCase) ||
        c.Name.Equals(player.CountryName, StringComparison.OrdinalIgnoreCase));

    if (foundCountry == null)
    {
        Debug.LogError($"[HotSeat] Ülke bulunamadı: '{player.CountryId}' / '{player.CountryName}'");
        Debug.LogError($"[HotSeat] Mevcut ülkeler: " +
            string.Join(", ", world.Countries.Select(c => $"{c.Id}={c.Name}")));
        return;
    }

    SimLogger.Log($"[HotSeat] Aktif ülke: {foundCountry.Name} ({foundCountry.Id})");

    // ─────────────────────────────────────────────────────────────
    // 2) KRİTİK: sınıf field'ını güncelle (local var field'ı gölgeliyor!)
    //    Bunu yapmazsak NextTurn() hep eski oyuncunun ülkesini işler.
    // ─────────────────────────────────────────────────────────────
    this.playerCountry = foundCountry;
    this.playerCountry.Engine.IsPlayerCountry = true;

    // ─────────────────────────────────────────────────────────────
    // 3) UI'ı yeni aktif oyuncuya göre tazele
    // ─────────────────────────────────────────────────────────────
    ui.PopulateCountryDropdown(world.Countries, playerCountry);

    // Politika listesini bu oyuncunun ülkesine göre yeniden çiz
    var policies = playerCountry.Engine.AllObjects
        .OfType<SimPolicy>()
        .ToList();
    ui.RefreshPolicyList(policies, ChangePolicyValue, RequestMinisterAdvice);

    // Kabine olay yayınını yeniden bağla (yeni ülkenin engine'i için)
    playerCountry.Engine.Cabinet.OnCabinetEvent =
        (msg, isWarn) => ui.Notify(msg, isWarn);

    // ─────────────────────────────────────────────────────────────
    // 4) Sıradaki oyuncu için bilgilendirme + dashboard
    // ─────────────────────────────────────────────────────────────
    ui.WriteLog($"▶️ Sıra: {player.PlayerName} — {playerCountry.Name}");
    ui.Notify($"{player.PlayerName}, sıra sende!", false);

    UpdateUI();
    RunTutorial();

    // ─────────────────────────────────────────────────────────────
    // 5) Otomatik kayıt (hot-seat'te her oyuncu geçişinde)
    // ─────────────────────────────────────────────────────────────
    SaveLoadManager.SaveGame(world, playerCountry);
}

    public void ProposePolicy(string policyId, float amount, string selectedFrame)
    {
        if (!RequireGoverning("yasa önermek")) return;
        var policy = playerCountry.Engine.AllObjects.OfType<SimPolicy>().FirstOrDefault(p => p.Id == policyId);
        if (policy == null) return;
        var engine = playerCountry.Engine;

        bool isPassed;
        if (engine.Universe.PresidentialPowers)
        {
            // Anayasa değişikliği: meclis oylaması atlanır ama her kararname meşruiyete mal olur
            isPassed = true;
            engine.Legitimacy.AdjustLegitimacy(-2f);
            ui.WriteLog("Başkanlık kararnamesi: meclis oylaması atlandı.");
        }
        else
        {
                        LegislativeChamber chamber = new LegislativeChamber();
            isPassed = chamber.ConductVote(policy, engine.Actors, 10f, engine.PartyManager, engine).IsPassed;
        }

        if (isPassed)
        {
            engine.PoliticalCapital -= 10f;

            // Yolsuzluk arttıkça bürokrasi yasayı daha çok yavaşlatır (0-5 tur)
            int delay = Mathf.FloorToInt(engine.CorruptionLevel / 20f);
            if (delay <= 0)
            {
                ApplyPolicy(policy, amount);
                ui.WriteLog($"✅ YASA KABUL: {policy.Name} ({selectedFrame})");
            }
            else
            {
                engine.Universe.Pending.Add(new PendingPolicyState { PolicyId = policyId, Amount = amount, TurnsLeft = delay });
                ui.WriteLog($"🏛️ YASA KABUL: {policy.Name} ({selectedFrame}) ama bürokrasi uygulamayı {delay} tur yavaşlatıyor.");
            }

            // Koalisyon ortakları ve radikal gruplar yasaya tepki verir
            float frameShift = 0f;
            if (!string.IsNullOrEmpty(selectedFrame) && policy.Frames != null && policy.Frames.TryGetValue(selectedFrame, out float shift))
                frameShift = shift;
            UniverseSystems.OnPolicyPassed(engine, policy, policy.IdeologicalAlignment + frameShift);
            FlushUniverseMessages();
        }
        else { engine.Legitimacy.AdjustLegitimacy(-5f); ui.WriteLog($"❌ YASA RED: {policy.Name} ({selectedFrame})"); }
        UpdateUI();
    }

    public void ToggleDiplomacy() { ui.ToggleDiplomacy(); RefreshDiplomacyInfo(); }

        void RefreshDiplomacyInfo()
    {
        string targetName = ui.GetSelectedCountryName();
        var target = world.Countries.FirstOrDefault(c => c.Name == targetName);
        if (target == null) return;
        float relation = world.Diplomacy.GetRelation(playerCountry.Id, target.Id);
        
        // FAZ 0: Seçili ülke detayı için referansı UI'ya ver
        ui.selectedCountryDetail = target;
        
        ui.UpdateDiplomacyInfo(relation, playerCountry.Engine.Intel.NetworkStrength);
    }

        void UpdateUI()
    {
        ui.UpdateDashboard(playerCountry, world.GlobalTurn);
        ui.ShowNews(playerCountry.Engine.CurrentNews);
        
        // FAZ 0: Dünya haberlerini topla (AI ülkelerin son eylemleri)
        var worldNews = new List<string>();
        foreach (var c in world.Countries)
        {
            if (c.IsPlayerControlled) continue;
            if (c.RecentActions == null) continue;
            foreach (var action in c.RecentActions.Take(2))   // Her ülkeden en fazla 2
            {
                worldNews.Add($"{c.Name}: {action}");
            }
        }
        ui.ShowWorldNews(worldNews);
        
        // FAZ 0: Seçili ülke detayını yenile
        if (ui.selectedCountryDetail != null)
        {
            var target = world.Countries.FirstOrDefault(c => c.Id == ui.selectedCountryDetail.Id);
            if (target != null)
            {
                ui.selectedCountryDetail = target;
                float relation = world.Diplomacy.GetRelation(playerCountry.Id, target.Id);
                ui.UpdateDiplomacyInfo(relation, playerCountry.Engine.Intel.NetworkStrength);
            }
        }
    }
}