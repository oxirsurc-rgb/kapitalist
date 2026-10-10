using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using DemocracySim.Engine.World;
using System.Text.Json.Serialization;

namespace DemocracySim.Engine.Core
{
    // =====================================================================
    // KAYDEDİLEN DURUM (SaveLoadManager bunu JSON'a yazar)
    // =====================================================================
    public class PendingPolicyState
    {
        public string PolicyId { get; set; }
        public float Amount { get; set; }
        public int TurnsLeft { get; set; }
    }

    public class CoalitionPartner
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public float Ideology { get; set; }          // -100 (sol) .. +100 (sağ)
        public float Satisfaction { get; set; } = 70f;
        public bool InGovernment { get; set; } = true;
        public int LeftTurn { get; set; } = 0;
    }

    public class UniverseMessage
    {
        public string Text { get; set; }
        public bool IsWarning { get; set; }
    }

    public class UniverseCrisis
    {
        public string Title;
        public string Description;
        public List<CrisisOption> Options = new List<CrisisOption>();
    }

    public class UniverseState
    {
        // Halk dinamikleri
        public Dictionary<string, int> LowSatTurns { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, bool> Radicalized { get; set; } = new Dictionary<string, bool>();
        public Dictionary<string, RadicalizationTier> RadicalizationTiers { get; set; }
    = new Dictionary<string, RadicalizationTier>();
        public int BrainDrainTurns { get; set; } = 0;
        public float Unrest { get; set; } = 0f;                 // Sokak huzursuzluğu 0-100

        // Devlet aygıtı ve dış baskı
        public float SurveillanceLevel { get; set; } = 0f;      // Gizli polis / gözetim 0-100
        public float SanctionLevel { get; set; } = 0f;          // Uluslararası yaptırım baskısı 0-100
        public int RepressionCount { get; set; } = 0;
        public int SanctionWarnLevel { get; set; } = 0;

        // Anayasa
        public bool PresidentialPowers { get; set; } = false;
        public int ElectionExtensions { get; set; } = 0;

        // Koalisyon, ticaret, bürokrasi kuyruğu
        public List<CoalitionPartner> Partners { get; set; } = new List<CoalitionPartner>();
        public List<string> TradePartners { get; set; } = new List<string>();
        public List<PendingPolicyState> Pending { get; set; } = new List<PendingPolicyState>();

        public int CrisisCooldown { get; set; } = 0;
        // FAZ 9: Ekonomi kriz durumu
public int ImfBailoutUsedTurn { get; set; } = 0;
public int EmergencyTaxUsedTurn { get; set; } = 0;
public int StructuralReformTurnsLeft { get; set; } = 0;
public int EconomicWarningLevel { get; set; } = 0;   // 0=normal, 1=uyarı, 2=kritik
        // EK-25: Bakan piyasası adayları
[JsonIgnore] public List<MinistryCandidate> MinistryMarket { get; set; } = new List<MinistryCandidate>();

        // FAZ 4: ortak küresel durum, devrim bulaşması, göç (WorldManager her tur yazar)
        public float GlobalTensionPressure { get; set; } = 0f;  // 0-100, WorldManager.GlobalTension kopyası
        public float GlobalGrowthModifier { get; set; } = 0f;   // tur başına GSYİH etkisi
        public float ContagionPressure { get; set; } = 0f;      // komşu isyanlarından gelen baskı 0-25
        public float MigrationBalance { get; set; } = 0f;       // + net göç alan, - net göç veren

        // FAZ 5: öğretici ve demo senaryosu
        public int TutorialStep { get; set; } = 0;
        public bool TutorialDone { get; set; } = false;
        public bool DemoMode { get; set; } = false;
        public int DemoStartTurn { get; set; } = 0;
        public string DemoResult { get; set; } = null;

        // Seçim kampanyası yatırımları (CampaignManager kaydedilmediği için burada saklanır)
        public Dictionary<string, float> CampaignInvestments { get; set; } = new Dictionary<string, float>();

        // Propaganda (algı-gerçek farkı) ve oyun sonu
        public float PropagandaGap { get; set; } = 0f;
        public int RevoltTurns { get; set; } = 0;
        public string GameOverReason { get; set; } = null;

        // Kaydedilmez: her tur yeniden üretilir
        [JsonIgnore] public List<UniverseMessage> Messages { get; set; } = new List<UniverseMessage>();
        [JsonIgnore] public UniverseCrisis PendingCrisis { get; set; }
        // Dünya turunda üretilen haberler (Messages her tur temizlendiği için ayrı kuyruk)
        [JsonIgnore] public List<UniverseMessage> WorldNews { get; set; } = new List<UniverseMessage>();
    }

    // =====================================================================
    // EVREN SİSTEMLERİ
    // =====================================================================
    public static class UniverseSystems
    {
        public const float ElectionExtensionCost = 80f;
        public const float PresidentialPowersCost = 120f;
        public const float SurveillanceCost = 15f;
        public const float TradeAgreementCost = 20f;
        public const float PropagandaCost = 15f;


        // ---------------- yardımcılar ----------------
        public static void Say(SimulationEngine e, string text, bool warn = false)
            => e.Universe.Messages.Add(new UniverseMessage { Text = text, IsWarning = warn });

        static float Clamp(float v, float min, float max) => Math.Max(min, Math.Min(max, v));

        static DemographicGroup Group(SimulationEngine e, string id)
            => e.Demographics.FirstOrDefault(g => g.Id == id);

        static void AddGdp(SimulationEngine e, float delta)
{
    var gdp = e.Registry.Get(ObjectRegistry.Ids.Gdp);
    if (gdp == null) return;
    gdp.ActualValue += delta;
    gdp.Clamp();
}

        /// <summary>FAZ 4: Dünya turunda (WorldManager) oyuncuya haber bırakır.</summary>
        public static void QueueWorldNews(SimulationEngine e, string text, bool warn = false)
            => e.Universe.WorldNews.Add(new UniverseMessage { Text = text, IsWarning = warn });

        static float Inflation(SimulationEngine e) => (float)e.Economy.Inflation;

        static void EnsureCoalition(UniverseState u)
        {
            if (u.Partners.Count > 0) return;
            u.Partners.Add(new CoalitionPartner { Id = "greens", Name = "Yeşil Cephe", Ideology = -45f });
            u.Partners.Add(new CoalitionPartner { Id = "national", Name = "Ulusal Birlik Partisi", Ideology = 50f });
        }

        // ---------------- ANA TUR DÖNGÜSÜ ----------------
        public static void ProcessTurn(SimulationEngine e)
        {
            var u = e.Universe;
            u.Messages.Clear();
            u.PendingCrisis = null;
            if (e.Demographics.Count == 0) return; // yalnızca oyuncu ülkesi (halk grupları olan)

            // FAZ 4: dünya turunda kuyruğa alınan haberleri bu turun mesajlarına ekle
            if (u.WorldNews.Count > 0) { u.Messages.AddRange(u.WorldNews); u.WorldNews.Clear(); }

            EnsureCoalition(u);
            if (u.CrisisCooldown > 0) u.CrisisCooldown--;

            ProcessPopulation(e, u);
            ProcessCoalition(e, u);
            ProcessStateApparatus(e, u);
            ProcessPropaganda(e, u);
            UpdateUnrest(e, u);
            CheckGameOver(e, u);

            // Haftalık (4 turda bir) anket
            if (e.CurrentTurn > 0 && e.CurrentTurn % 4 == 0)
                foreach (var line in BuildPoll(e)) Say(e, "[ANKET] " + line);

            TryTriggerCrisis(e, u);
        }

        // ---------------- 1) Halk: radikalleşme, beyin göçü, alışkanlık ----------------
        static void ProcessPopulation(SimulationEngine e, UniverseState u)
        {
            foreach (var g in e.Demographics)
            {
                u.LowSatTurns.TryGetValue(g.Id, out int low);
                bool radical = u.Radicalized.TryGetValue(g.Id, out bool r) && r;

                if (g.Satisfaction < 20f) low++;
                else if (g.Satisfaction > 40f) low = Math.Max(0, low - 1);
                u.LowSatTurns[g.Id] = low;

               var newTier = RadicalizationHelper.GetTier(g.Satisfaction);
                 u.RadicalizationTiers[g.Id] = newTier;

// Eski bool sistemiyle uyumluluk
              u.Radicalized[g.Id] = newTier >= RadicalizationTier.Radical;

                if (newTier >= RadicalizationTier.Radical && !radical)
             {
                 Say(e, $"{g.Name} {RadicalizationHelper.GetLabel(newTier)} seviyesine yükseldi!", true);
               }

                if (radical)
                {
                    g.AdjustSatisfaction(-1.5f);
                    e.Legitimacy.AdjustLegitimacy(-1.5f);
                }

                // Muhalefetteyken yönetim sende değil: gruplar zamanla nötr duruma (50) doğru kayar
                if (e.CurrentRole == SimulationEngine.PlayerRole.Opposition)
                    g.AdjustSatisfaction((50f - g.Satisfaction) * 0.05f);

                // Alışkanlık (Democracy 4 "complacency"): aşırı memnun grup artık bunu normal sayar
                if (g.Satisfaction > 85f) g.AdjustSatisfaction(-1.5f);
            }

            // Beyin göçü
            var intel = Group(e, "intellectuals");
            if (intel != null)
            {
                if (intel.Satisfaction < 25f)
                {
                    u.BrainDrainTurns++;
                    if (u.BrainDrainTurns >= 3)
                    {
                        AddGdp(e, -1.5f);
                        if (u.BrainDrainTurns % 3 == 0)
                            Say(e, "BEYİN GÖÇÜ: Entelektüeller ülkeyi terk ediyor, büyüme yavaşlıyor.", true);
                    }
                }
                else u.BrainDrainTurns = 0;
            }
        }

        /// <summary>FAZ 3: Genişletilmiş halk grupları — 8 grup.</summary>
public static void InitializeExtendedDemographics(SimulationEngine e, CountryProfile profile)
{
    // Mevcut 4 grup zaten eklendi; üzerine 4 yeni grup ekle
    e.Demographics.Add(new DemographicGroup("youth", "Gençler", 0.15f));
    e.Demographics.Add(new DemographicGroup("rural", "Kırsal Kesim", 0.12f));
    e.Demographics.Add(new DemographicGroup("retirees", "Emekliler", 0.10f));
    e.Demographics.Add(new DemographicGroup("students", "Öğrenciler", 0.08f));
}

        // ---------------- 2) Koalisyon ----------------
        static void ProcessCoalition(SimulationEngine e, UniverseState u)
        {
            if (e.CurrentRole != SimulationEngine.PlayerRole.Governing) return; // muhalefette koalisyon yönetimi yok
            foreach (var p in u.Partners)
            {
                if (!p.InGovernment)
                {
                    if (e.CurrentTurn - p.LeftTurn >= 8)
                    {
                        p.InGovernment = true;
                        p.Satisfaction = 50f;
                        Say(e, $"{p.Name} yeniden koalisyona katıldı.");
                    }
                    continue;
                }
                p.Satisfaction += (50f - p.Satisfaction) * 0.05f; // zamanla nötre döner
            }

            var active = u.Partners.Where(p => p.InGovernment).ToList();
            if (active.Count > 0 && active.All(p => p.Satisfaction >= 60f))
                e.PoliticalCapital = Math.Min(e.MaxPoliticalCapital, e.PoliticalCapital + 2f); // uyumlu koalisyon bonusu
        }

        /// <summary>Bir yasa kabul edilince çağrılır: koalisyon ortakları ve radikal gruplar tepki verir.</summary>
        public static void OnPolicyPassed(SimulationEngine e, SimPolicy policy, float effectiveAlignment)
        {
            var u = e.Universe;
            if (u == null) return;
            EnsureCoalition(u);

            if (u.Partners == null) return;
            foreach (var p in u.Partners.Where(x => x != null && x.InGovernment).ToList())
            {
                float diff = Math.Abs(effectiveAlignment - p.Ideology);
                if (diff > 60f) p.Satisfaction -= Math.Min(25f, (diff - 60f) * 0.5f);
                else if (diff < 30f) p.Satisfaction += 3f;
                p.Satisfaction = Clamp(p.Satisfaction, 0f, 100f);

                if (p.Satisfaction <= 0f)
                {
                    p.InGovernment = false;
                    p.LeftTurn = e.CurrentTurn;
                    e.TurnUntilElection = Math.Min(e.TurnUntilElection, 1);
                    e.Legitimacy.AdjustLegitimacy(-15f);
                    e.PoliticalCapital = Math.Max(0f, e.PoliticalCapital - 30f);
                    u.Unrest = Clamp(u.Unrest + 15f, 0f, 100f);
                    Say(e, $"HÜKÜMET DÜŞTÜ! {p.Name}, '{policy.Name}' yüzünden koalisyondan çekildi. Erken seçime gidiliyor!", true);
                }
                else if (p.Satisfaction < 25f)
                {
                    Say(e, $"{p.Name} '{policy.Name}' yasasına çok tepkili. Koalisyondan çekilmekle tehdit ediyor!", true);
                }
            }

            // Radikal gruplar sadece uç politikalarla sakinleşir
            if (Math.Abs(effectiveAlignment) >= 60f && policy.GroupImpacts != null)
            {
                if (e.Demographics == null) return;
                foreach (var g in e.Demographics)
                {
                    if (g == null) continue;
                    if (u.Radicalized != null && u.Radicalized.TryGetValue(g.Id, out bool rad) && rad &&
                        policy.GroupImpacts.TryGetValue(g.Id, out float impact) && impact > 0f)
                    {
                        g.AdjustSatisfaction(15f);
                        Say(e, $"{g.Name} bu sert yasayı sahiplendi, öfkeleri yatışıyor.");
                    }
                }
            }
            // --- YENİ (1.3): Oyuncunun ideolojik konumu zamanla yaptığı politikalara doğru kayar.
            // Ağırlık 0.1: Bir yasa oyuncunun konumunu %10 kaydırır, eski konum %90 korunur.
            // Bu sayede tek bir aykırı yasa oyuncuyu anında zıt kutba atmaz; kademeli kayma olur.
            e.PlayerGlobalAlignment = Math.Clamp(
                e.PlayerGlobalAlignment * 0.9f + effectiveAlignment * 0.1f,
                -100f, 100f);
        }

        // ---------------- 3) Devlet aygıtı, yaptırımlar, ticaret ----------------
        static void ProcessStateApparatus(SimulationEngine e, UniverseState u)
        {
            if (u.SurveillanceLevel > 0f)
            {
                e.Legitimacy.AdjustLegitimacy(-(u.SurveillanceLevel / 100f) * 1.5f);
                if (u.SurveillanceLevel > 50f)
                    u.SanctionLevel = Clamp(u.SanctionLevel + (u.SurveillanceLevel - 50f) * 0.05f, 0f, 100f);
            }

            u.SanctionLevel = Clamp(u.SanctionLevel - 0.5f, 0f, 100f);
            if (u.SanctionLevel > 0f)
            {
                AddGdp(e, -u.SanctionLevel * 0.03f);
                e.Legitimacy.AdjustLegitimacy(-u.SanctionLevel * 0.02f);
            }

            if (u.SanctionLevel >= 60f && u.SanctionWarnLevel < 2)
            {
                u.SanctionWarnLevel = 2;
                Say(e, "ULUSLARARASI YAPTIRIM: Ambargo kararı alındı! Ekonomi ve meşruiyet ağır darbe alıyor.", true);
            }
            else if (u.SanctionLevel >= 30f && u.SanctionWarnLevel < 1)
            {
                u.SanctionWarnLevel = 1;
                Say(e, "BM benzeri kurumlar insan hakları ihlalleri nedeniyle yaptırım uyarısı yaptı.", true);
            }
            else if (u.SanctionLevel < 15f) u.SanctionWarnLevel = 0;

            if (u.TradePartners.Count > 0) AddGdp(e, 0.3f * u.TradePartners.Count);

            // FAZ 4: küresel konjonktür (ticaret ortakları krizi hafifletir / bonusu büyütür)
            if (Math.Abs(u.GlobalGrowthModifier) > 0.01f)
            {
                float tradeShield = 1f + 0.1f * u.TradePartners.Count;
                AddGdp(e, u.GlobalGrowthModifier >= 0f ? u.GlobalGrowthModifier * tradeShield
                                                       : u.GlobalGrowthModifier / tradeShield);
            }
        }

       // ---------------- 4) Sokak huzursuzluğu ----------------
static void UpdateUnrest(SimulationEngine e, UniverseState u)
{
    float dissat = 0f;
    foreach (var g in e.Demographics) dissat += (100f - g.Satisfaction);
    dissat /= Math.Max(1, e.Demographics.Count);

    float target = dissat * 0.7f;
foreach (var kv in u.RadicalizationTiers)
    target += RadicalizationHelper.GetUnrestContribution(kv.Value);    
    // YENİ: Küresel Gerginlik Etkisi
    // WorldManager'dan GlobalTension değerini almamız lazım. 
    // (Bunu yapmak için WorldManager referansını UniverseSystems'e geçirmeliyiz 
    // veya basitçe bir statik değişkene bağlayabiliriz).
    
    // Şimdilik basitleştirmek için: Eğer ülkede yaptırımlar varsa gerginlik artar
    target += u.SanctionLevel * 0.1f;

    // FAZ 4: küresel gerginlik (30 üstü) ve komşu isyanlarının bulaşması
    if (u.GlobalTensionPressure > 30f) target += (u.GlobalTensionPressure - 30f) * 0.15f;
    target += u.ContagionPressure;
    u.ContagionPressure = Math.Max(0f, u.ContagionPressure - 1.5f);
    target -= u.SurveillanceLevel * 0.4f;

    u.Unrest = Clamp(u.Unrest + (target - u.Unrest) * 0.3f, 0f, 100f);
}

        // ---------------- 5) Sistemik krizler ----------------
        static void TryTriggerCrisis(SimulationEngine e, UniverseState u)
        {
            if (u.CrisisCooldown > 0 || u.PendingCrisis != null) return;
            if (e.CurrentRole != SimulationEngine.PlayerRole.Governing) return; // krizleri hükümet çözer

            var workers = Group(e, "workers");
            var capitalists = Group(e, "capitalists");

            if (u.Unrest >= 55f) u.PendingCrisis = StreetCrisis();
            else if (workers != null && capitalists != null && Math.Abs(workers.Satisfaction - capitalists.Satisfaction) > 45f)
                u.PendingCrisis = StrikeCrisis();
                        else if (Inflation(e) > 20f) u.PendingCrisis = FoodCrisis();   // K6: 35 → 20
            {
                // CrisisManager'daki koşullu krizler (yakıt, kabine, sınır olayı) artık buradan tetikleniyor
                var ce = e.Crisis.CheckForCrisis(e);
                if (ce != null) u.PendingCrisis = FromCrisisEvent(ce);
            }

            if (u.PendingCrisis != null) u.CrisisCooldown = 4;
        }

        // CrisisManager'ın CrisisEvent'ini oyuncuya gösterilen UniverseCrisis'e çevirir
        static UniverseCrisis FromCrisisEvent(CrisisEvent ce)
        {
            var c = new UniverseCrisis { Title = ce.Title, Description = ce.Description };
            foreach (var o in ce.Options)
            {
                var opt = o; // closure için kopya
                c.Options.Add(new CrisisOption(opt.Label, x =>
                {
                    opt.Effect?.Invoke(x);
                    if (!string.IsNullOrEmpty(opt.ResultText)) Say(x, opt.ResultText);
                }));
            }
            return c;
        }

        // ---------------- Propaganda (algı-gerçek farkı) ----------------
        static void ProcessPropaganda(SimulationEngine e, UniverseState u)
        {
            if (u.PropagandaGap <= 0f) return;

            // Propaganda kısa vadede meşruiyeti şişirir...
            e.Legitimacy.AdjustLegitimacy(u.PropagandaGap * 0.1f);

            // ...ama fark büyüdükçe ifşa riski artar
            float exposeChance = Math.Min(0.35f, u.PropagandaGap / 200f);
            if (SimRng.NextDouble() < exposeChance)
            {
                e.Legitimacy.AdjustLegitimacy(-u.PropagandaGap * 0.5f);
                u.Unrest = Clamp(u.Unrest + 10f, 0f, 100f);
                Say(e, "PROPAGANDA İFŞA OLDU! Halk gerçekle algı arasındaki uçurumu gördü; güven ve düzen sarsıldı.", true);
                u.PropagandaGap = 0f;
            }
            else
            {
                u.PropagandaGap = Math.Max(0f, u.PropagandaGap - 1.5f);
            }
        }

        public static string RunPropaganda(SimulationEngine e)
        {
            var u = e.Universe;
            if (u.PropagandaGap >= 60f) return "Propaganda ağı zaten doygunlukta; daha fazlası ifşa riskini artırır.";
            if (e.PoliticalCapital < PropagandaCost) return $"Yeterli sermaye yok ({PropagandaCost:F0} gerekir).";
            e.PoliticalCapital -= PropagandaCost;
            u.PropagandaGap = Clamp(u.PropagandaGap + 20f, 0f, 60f);
            return $"Propaganda kampanyası başladı (algı farkı: {u.PropagandaGap:F0}). Meşruiyet kısa vadede artar ama ifşa riski büyür.";
        }

        // ---------------- Oyun sonu ----------------
        static void CheckGameOver(SimulationEngine e, UniverseState u)
        {
            if (u.GameOverReason != null) return;
            if (e.CurrentRole != SimulationEngine.PlayerRole.Governing) { u.RevoltTurns = 0; return; }

            bool revolt = u.Unrest >= 85f || e.Legitimacy.CurrentLegitimacy <= 5f;
            u.RevoltTurns = revolt ? u.RevoltTurns + 1 : Math.Max(0, u.RevoltTurns - 1);

            if (revolt && u.RevoltTurns < 3)
                Say(e, $"HALK İSYANIN EŞİĞİNDE! Durumu {3 - u.RevoltTurns} tur içinde düzeltmezseniz hükümet devrilecek.", true);

            if (u.RevoltTurns >= 3)
            {
                u.GameOverReason = "DEVRİM: Halk sokakları ele geçirdi ve hükümetiniz devrildi.";
                return;
            }
            if (e.Economy.CreditRating <= 0f)
            {
                u.GameOverReason = "İFLAS: Devlet borçlarını ödeyemedi, ülke iflas etti ve hükümet çöktü.";
            }
        }

        // ---------------- Seçim sonrası ödül / ceza ----------------
        public static void ApplyElectionOutcome(SimulationEngine e, bool wasGoverning, bool playerWon)
        {
            var u = e.Universe;
            EnsureCoalition(u);

            if (playerWon && wasGoverning)
            {
                e.PoliticalCapital = Math.Min(e.MaxPoliticalCapital, e.PoliticalCapital + 30f);
                e.Legitimacy.AdjustLegitimacy(10f);
                u.Unrest = Clamp(u.Unrest - 10f, 0f, 100f);
                foreach (var p in u.Partners.Where(x => x.InGovernment)) p.Satisfaction = Clamp(p.Satisfaction + 10f, 0f, 100f);
                Say(e, "SEÇİM ZAFERİ! Yeni bir mandat aldınız: +30 sermaye, meşruiyet yükseldi.");
            }
            else if (playerWon)
            {
                // Muhalefetten iktidara geçiş
                e.PoliticalCapital = Math.Min(e.MaxPoliticalCapital, Math.Max(e.PoliticalCapital, 60f));
                e.Legitimacy.AdjustLegitimacy(15f);
                u.Unrest = Clamp(u.Unrest - 15f, 0f, 100f);
                e.DeepStateStability = Math.Max(0f, e.DeepStateStability - 10f);
                foreach (var p in u.Partners) { p.InGovernment = true; p.Satisfaction = 60f; }
                Say(e, "İKTİDARA GELDİNİZ! Balayı dönemi: meşruiyet yükseldi, ancak derin devlet yeni yönetime mesafeli.");
            }
            else if (wasGoverning)
            {
                // İktidarı kaybetti: yeni hükümet yasaları geri alır
                e.PoliticalCapital = Math.Min(e.PoliticalCapital, 60f);
                e.Legitimacy.AdjustLegitimacy(-10f);
                int reverted = 0;
                foreach (var pol in e.AllObjects.OfType<SimPolicy>().Where(x => x.IsActive))
                    if (SimRng.NextDouble() < 0.5) { pol.IsActive = false; reverted++; }
                u.Pending.Clear();
                foreach (var p in u.Partners) { p.InGovernment = false; p.LeftTurn = e.CurrentTurn; }
                Say(e, $"SEÇİMİ KAYBETTİNİZ! Muhalefete düştünüz. Yeni hükümet {reverted} yasanızı iptal etti.", true);
            }
            else
            {
                e.PoliticalCapital = Math.Min(e.MaxPoliticalCapital, e.PoliticalCapital + 10f);
                Say(e, "Seçimi kazanamadınız; muhalefette kalıyorsunuz. Bir sonraki seçim için yeniden örgütlenin.", true);
            }

            u.ElectionExtensions = 0;
            e.TurnUntilElection = 12;
        }

        static UniverseCrisis StreetCrisis()
        {
            var c = new UniverseCrisis { Title = "SOKAK OLAYLARI", Description = "Memnuniyetsizlik protestolara dönüştü. Meydanlar doluyor." };
            c.Options.Add(new CrisisOption("Sert müdahale (Meşruiyet -12, uluslararası tepki)", x =>
            {
                var u = x.Universe;
                u.Unrest = Clamp(u.Unrest - 30f, 0f, 100f);
                x.Legitimacy.AdjustLegitimacy(-12f);
                u.RepressionCount++;
                u.SanctionLevel = Clamp(u.SanctionLevel + 12f, 0f, 100f);
                Say(x, "Protestolar bastırıldı ama dünya sizi izliyor.", true);
            }));
            c.Options.Add(new CrisisOption("Taviz ver (20 Sermaye)", x =>
            {
                var u = x.Universe;
                if (x.PoliticalCapital >= 20f)
                {
                    x.PoliticalCapital -= 20f;
                    u.Unrest = Clamp(u.Unrest - 25f, 0f, 100f);
                    foreach (var g in x.Demographics) g.AdjustSatisfaction(4f);
                    Say(x, "Taviz verildi, sokaklar sakinleşti.");
                }
                else
                {
                    u.Unrest = Clamp(u.Unrest + 5f, 0f, 100f);
                    Say(x, "Taviz için sermayeniz yetmedi, protestolar sürüyor!", true);
                }
            }));
            c.Options.Add(new CrisisOption("Görmezden gel (Meşruiyet -6)", x =>
            {
                x.Universe.Unrest = Clamp(x.Universe.Unrest + 5f, 0f, 100f);
                x.Legitimacy.AdjustLegitimacy(-6f);
                Say(x, "Hükümet sessiz kaldı, öfke büyüyor.", true);
            }));
            return c;
        }

        static UniverseCrisis StrikeCrisis()
        {
            var c = new UniverseCrisis { Title = "GENEL GREV", Description = "İşçi ve sermaye kesimi arasındaki uçurum sınıf çatışmasına dönüştü. Fabrikalar durdu." };
            c.Options.Add(new CrisisOption("İşçi lehine arabuluculuk", x =>
            {
                Group(x, "workers")?.AdjustSatisfaction(10f);
                Group(x, "capitalists")?.AdjustSatisfaction(-8f);
                AddGdp(x, -2f);
                Say(x, "Grev uzlaşmayla bitti; sermaye tarafı homurdanıyor.");
            }));
            c.Options.Add(new CrisisOption("Sermaye lehine grevi kır", x =>
            {
                Group(x, "workers")?.AdjustSatisfaction(-10f);
                Group(x, "capitalists")?.AdjustSatisfaction(6f);
                x.Universe.Unrest = Clamp(x.Universe.Unrest + 10f, 0f, 100f);
                x.Legitimacy.AdjustLegitimacy(-5f);
                Say(x, "Grev kırıldı, işçi mahalleleri öfkeli.", true);
            }));
            c.Options.Add(new CrisisOption("Bekle (Ekonomi zarar görür)", x =>
            {
                AddGdp(x, -6f);
                x.Universe.Unrest = Clamp(x.Universe.Unrest + 8f, 0f, 100f);
                Say(x, "Grev sürüyor, üretim çöküyor.", true);
            }));
            return c;
        }

        static UniverseCrisis FoodCrisis()
        {
            var c = new UniverseCrisis { Title = "GIDA KRİZİ", Description = "Enflasyon kontrolden çıktı, market raflarında kuyruklar var." };
            c.Options.Add(new CrisisOption("Gıda sübvansiyonu (25 Sermaye)", x =>
            {
                if (x.PoliticalCapital >= 25f)
                {
                    x.PoliticalCapital -= 25f;
                    x.Universe.Unrest = Clamp(x.Universe.Unrest - 15f, 0f, 100f);
                    Group(x, "workers")?.AdjustSatisfaction(5f);
                    Say(x, "Sübvansiyonla fiyatlar geçici olarak dizginlendi.");
                }
                else Say(x, "Sübvansiyon için sermayeniz yetmedi!", true);
            }));
            c.Options.Add(new CrisisOption("Acil ithalat (Ticaret ortağı varsa daha etkili)", x =>
            {
                float relief = x.Universe.TradePartners.Count > 0 ? 20f : 10f;
                AddGdp(x, -3f);
                x.Universe.Unrest = Clamp(x.Universe.Unrest - relief, 0f, 100f);
                Say(x, x.Universe.TradePartners.Count > 0 ? "Ticaret ortaklarınızdan acil gıda sevkiyatı geldi." : "Zayıf ithalat kanalları yetersiz kaldı.");
            }));
            c.Options.Add(new CrisisOption("Piyasaya bırak", x =>
            {
                Group(x, "workers")?.AdjustSatisfaction(-8f);
                x.Universe.Unrest = Clamp(x.Universe.Unrest + 12f, 0f, 100f);
                Say(x, "Hükümet müdahale etmedi, halk aç.", true);
            }));
            return c;
        }

        // ---------------- 6) Oyuncu eylemleri ----------------
        public static string Amend(SimulationEngine e, int type)
        {
            var u = e.Universe;
            if (type == 0)
            {
                if (u.ElectionExtensions >= 2) return "Seçim süresi en fazla 2 kez uzatılabilir.";
                if (e.PoliticalCapital < ElectionExtensionCost) return $"Yeterli sermaye yok ({ElectionExtensionCost:F0} gerekir).";
                e.PoliticalCapital -= ElectionExtensionCost;
                e.TurnUntilElection += 8;
                u.ElectionExtensions++;
                e.Legitimacy.AdjustLegitimacy(-8f);
                u.Unrest = Clamp(u.Unrest + 10f, 0f, 100f);
                return "Anayasa değişti: seçim 8 tur ertelendi. Muhalefet bunu 'iktidara tutunma' olarak yorumluyor.";
            }
            if (type == 1)
            {
                if (u.PresidentialPowers) return "Başkanlık yetkileri zaten genişletilmiş.";
                if (e.PoliticalCapital < PresidentialPowersCost) return $"Yeterli sermaye yok ({PresidentialPowersCost:F0} gerekir).";
                e.PoliticalCapital -= PresidentialPowersCost;
                u.PresidentialPowers = true;
                e.Legitimacy.AdjustLegitimacy(-12f);
                e.DeepStateStability = Math.Max(0f, e.DeepStateStability - 15f);
                return "Başkanlık yetkileri genişletildi: artık yasalar meclis oylaması olmadan çıkarılabilir. Derin devlet rahatsız.";
            }
            return "Geçersiz değişiklik.";
        }

        public static string ChangeSurveillance(SimulationEngine e, float delta)
        {
            var u = e.Universe;
            if (delta > 0f)
            {
                if (u.SurveillanceLevel >= 100f) return "Gözetim ağı zaten maksimumda.";
                if (e.PoliticalCapital < SurveillanceCost) return $"Yeterli sermaye yok ({SurveillanceCost:F0} gerekir).";
                e.PoliticalCapital -= SurveillanceCost;
                u.SurveillanceLevel = Clamp(u.SurveillanceLevel + delta, 0f, 100f);
                e.Legitimacy.AdjustLegitimacy(-4f);
                return $"Gizli polis güçlendirildi (Gözetim: %{u.SurveillanceLevel:F0}). Sokak sakinleşir ama meşruiyet ve dış itibar zarar görür.";
            }
            if (u.SurveillanceLevel <= 0f) return "Gözetim ağı zaten kapalı.";
            u.SurveillanceLevel = Clamp(u.SurveillanceLevel + delta, 0f, 100f);
            e.Legitimacy.AdjustLegitimacy(2f);
            return $"Gözetim gevşetildi (Gözetim: %{u.SurveillanceLevel:F0}).";
        }

        public static string SignTradeAgreement(SimulationEngine e, string targetId, string targetName)
        {
            var u = e.Universe;
            if (u.TradePartners.Contains(targetId)) return $"{targetName} ile zaten ticaret anlaşmanız var.";
            if (e.PoliticalCapital < TradeAgreementCost) return $"Yeterli sermaye yok ({TradeAgreementCost:F0} gerekir).";
            e.PoliticalCapital -= TradeAgreementCost;
            u.TradePartners.Add(targetId);
            AddGdp(e, 4f);
            Group(e, "capitalists")?.AdjustSatisfaction(3f);
            Group(e, "workers")?.AdjustSatisfaction(-3f);
            Group(e, "conservatives")?.AdjustSatisfaction(-2f);
            return $"{targetName} ile ticaret anlaşması imzalandı: GSYİH arttı, ama yerli üreticiler 'ucuz ithalat bizi bitiriyor' diyor.";
        }

        // ---------------- 7) Raporlar (arayüz için) ----------------
        public static List<string> BuildPoll(SimulationEngine e)
        {
            var lines = new List<string>();
            var rated = new List<KeyValuePair<string, float>>();

            foreach (var p in e.AllObjects.OfType<SimPolicy>())
            {
                if (!p.IsActive || p.GroupImpacts == null || p.GroupImpacts.Count == 0) continue;
                float approval = 50f;
                foreach (var g in e.Demographics)
                    if (p.GroupImpacts.TryGetValue(g.Id, out float v)) approval += v * 3f;
                rated.Add(new KeyValuePair<string, float>(p.Name, Clamp(approval, 3f, 97f)));
            }

            foreach (var x in rated.OrderByDescending(k => Math.Abs(k.Value - 50f)).Take(3))
            {
                lines.Add(x.Value >= 50f
                    ? $"Halkın %{x.Value:F0}'i '{x.Key}' yasanızı destekliyor."
                    : $"Halkın %{100f - x.Value:F0}'i '{x.Key}' yasanızdan nefret ediyor.");
            }
            if (rated.Count == 0) lines.Add("Yürürlükte anketlenecek yasa yok.");

            var worst = e.Demographics.OrderBy(g => g.Satisfaction).FirstOrDefault();
            if (worst != null) lines.Add($"En mutsuz grup: {worst.Name} (%{worst.Satisfaction:F0})");
            lines.Add($"Genel destek (meşruiyet): %{e.Legitimacy.CurrentLegitimacy:F1}");
            return lines;
        }

        public static List<string> CoalitionReport(SimulationEngine e)
        {
            var u = e.Universe;
            EnsureCoalition(u);
            var lines = new List<string>();
            foreach (var p in u.Partners)
            {
                string pos = p.Ideology < -20f ? "sol" : (p.Ideology > 20f ? "sağ" : "merkez");
                lines.Add(p.InGovernment
                    ? $"{p.Name} ({pos}): memnuniyet %{p.Satisfaction:F0}"
                    : $"{p.Name} ({pos}): koalisyon dışı");
            }
            lines.Add("İpucu: ortağın ideolojisine çok zıt yasa çıkarırsan hükümet düşer.");
            return lines;
        }

        public static List<string> StatusReport(SimulationEngine e)
        {
            var u = e.Universe;
            var lines = new List<string>
            {
                $"Sokak huzursuzluğu: %{u.Unrest:F0}",
                $"Gözetim (gizli polis): %{u.SurveillanceLevel:F0}",
                $"Uluslararası yaptırım baskısı: %{u.SanctionLevel:F0}",
                $"Yolsuzluk: %{e.CorruptionLevel:F0} | Derin devlet memnuniyeti: %{e.DeepStateStability:F0}",
                $"Ticaret anlaşması: {u.TradePartners.Count} ülke",
                $"Propaganda algı farkı: {u.PropagandaGap:F0} (yüksekse ifşa riski)",
                $"Başkanlık yetkileri: {(u.PresidentialPowers ? "Genişletildi" : "Normal")}"
            };
            var rad = u.Radicalized.Where(kv => kv.Value).Select(kv => Group(e, kv.Key)?.Name ?? kv.Key).ToList();
            lines.Add(rad.Count > 0 ? "Radikalleşmiş gruplar: " + string.Join(", ", rad) : "Radikalleşmiş grup yok.");
            return lines;
        }
    }
}