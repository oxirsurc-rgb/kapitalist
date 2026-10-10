using System;
using System.Collections.Generic;
using System.Linq;

namespace DemocracySim.Engine.Core
{
    public class SimulationEngine
    {
        public CrisisChainManager CrisisChains { get; set; } = new CrisisChainManager();
        public FactionBargaining FactionBargain { get; set; } = new FactionBargaining();
        public Dictionary<string, int> ActiveAntiCampaigns { get; set; } = new Dictionary<string, int>();
        public PartyBudget Party { get; set; } = new PartyBudget();
        public ShadowCabinet ShadowCab { get; set; } = new ShadowCabinet();
                public ProtestManager Protest { get; set; } = new ProtestManager();
        public FactionManager Factions { get; set; } = new FactionManager();
        // FAZ 3: Çıkar grupları
public List<InterestGroup> InterestGroups { get; set; } = new List<InterestGroup>();
                public MinistryManager Ministry { get; set; } = new MinistryManager();
                // FAZ 3: Göç yönetimi
public MigrationManager Migration { get; set; } = new MigrationManager();
public AchievementManager Achievements { get; set; } = new AchievementManager();
                public TechnologyManager Technology { get; set; } = new TechnologyManager();
        public SectorManager Sectors { get; set; } = new SectorManager();
        public MediaManager MediaOutlets { get; set; } = new MediaManager();
        public enum PlayerRole { Governing, Opposition }
        public PlayerRole CurrentRole { get; set; } = PlayerRole.Governing;
                public JudicialSystem Judiciary { get; set; } = new JudicialSystem();
                public DemocracySim.Engine.Legislative.VoterTransitionManager VoterTransition { get; set; }
    = new DemocracySim.Engine.Legislative.VoterTransitionManager();
        public int TurnUntilElection { get; set; } = 12;

        public string OwnerCountryId { get; set; } = "";
        // EK-19: Oyuncu ülkesi mi? Achievement, UI event'leri vb. sadece oyuncu için çalışır.
public bool IsPlayerCountry { get; set; } = false;
// EK-23: Parti kurma ve yeniden aday olma
public int ConsecutiveElectionLosses { get; set; } = 0;
public bool CanRunInNextElection { get; set; } = true;
// EK-24: Adaylık krizi durumu (UI kolaylığı için)
public bool InCandidateCrisis => CandidateManager.InCandidateCrisis(this);
        public float PlayerGlobalAlignment { get; set; } = 0f;

        public List<SimObject> AllObjects { get; set; } = new List<SimObject>();
        // EK-2: Hızlı erişim için merkezi dizin.
// AllObjects geriye dönük uyumluluk için kalıyor, ama yeni kod Registry kullanmalı.
public ObjectRegistry Registry { get; private set; } = new ObjectRegistry();
        public List<PoliticalActor> Actors { get; set; } = new List<PoliticalActor>();
        public List<DemographicGroup> Demographics { get; set; } = new List<DemographicGroup>();
        
        public LegitimacyManager Legitimacy { get; set; } = new LegitimacyManager();
        /// <summary>Pipeline performans raporu.</summary>
public List<string> GetPipelinePerformanceReport() => _turnRegistry.GetPerformanceReport();
        public MediaEngine Media { get; set; } = new MediaEngine();
        public CrisisManager Crisis { get; set; } = new CrisisManager();
        public EconomyManager Economy { get; set; } = new EconomyManager();
        public DemocracySim.Engine.Legislative.PartyManager PartyManager { get; set; } 
    = new DemocracySim.Engine.Legislative.PartyManager();
        public ArmyManager Army { get; set; } = new ArmyManager();
        public IntelligenceManager Intel { get; set; } = new IntelligenceManager();
        public EventManager EventSys { get; set; } = new EventManager();
        public ElectionManager Elections { get; set; } = new ElectionManager();
        public CabinetManager Cabinet { get; set; } = new CabinetManager();
        public CampaignManager Campaign { get; set; } = new CampaignManager();

        public List<NewsArticle> CurrentNews { get; set; } = new List<NewsArticle>();
        public List<SimPolicy> ProposedPolicies { get; set; } = new List<SimPolicy>();
        public float PoliticalCapital { get; set; } = 100f;
        public float MaxPoliticalCapital { get; set; } = 200f;
        public float DeepStateStability { get; set; } = 100f;
        public float CorruptionLevel { get; set; } = 0f;
        public UniverseState Universe { get; set; } = new UniverseState();

        public int IterationsPerTurn { get; set; } = 5; 
        public float DampingFactor { get; set; } = 0.2f; 
        public int CurrentTurn { get; set; } = 0;
public void AddObject(SimObject obj)
{
    AllObjects.Add(obj);
    Registry.Register(obj);   // EK-2: Registry'e de kayıt
}

        public void AddActor(PoliticalActor actor) => Actors.Add(actor);

        public void AddEffect(string sourceId, string targetId, float strength, EffectType type, CurveType curve = CurveType.Linear)
{
    // EK-2: O(1) registry erişimi (eskiden O(n) FirstOrDefault idi)
    var source = Registry.Get(sourceId);
    var target = Registry.Get(targetId);
    
    if (source == null)
    {
        SimLogger.Log($"[AddEffect] Kaynak bulunamadi: '{sourceId}' -> '{targetId}'. Etki atlandi.", 
            SimLogger.LogLevel.Warning);
        return;
    }
    if (target == null)
    {
        SimLogger.Log($"[AddEffect] Hedef bulunamadi: '{sourceId}' -> '{targetId}'. Etki atlandi.", 
            SimLogger.LogLevel.Warning);
        return;
    }
    
    var effect = new SimEffect(source, target, strength, type) { Curve = curve };
    source.OutgoingEffects.Add(effect); 
    target.IncomingEffects.Add(effect);

}
        public void ProposePolicy(string policyId)
{
    var policy = AllObjects.OfType<SimPolicy>().FirstOrDefault(p => p.Id == policyId);
    if (policy == null) return;

    if (!ProposedPolicies.Contains(policy))
    {
        ProposedPolicies.Add(policy);
        SimLogger.Log($"📜 Meclis Gündemi: {policy.Name} yasası oylanmak üzere sunuldu!");
    }
}
public SimulationEngine()
{
    Army.BindEngine(this);
    Technology.BindEngine(this);
    RegisterAllTurnSystems();   // R-FIX: boru hattı daha önce hiç kaydedilmiyordu → tur boş çalışıyordu
}

// Muhalefetin yasaya karşı kampanya yürütmesi (Oylama öncesi baskıyı artırır)
public float CampaignAgainstPolicy(string policyId, float capital)
{
    if (CurrentRole != PlayerRole.Opposition) return 0f;
    if (PoliticalCapital < capital) return 0f;

    PoliticalCapital -= capital;
    
    // Ne kadar sermaye harcarsan o kadar baskı oluşturursun (Sanal bir baskı değeri)
    float pressure = capital * 2.0f; 
    SimLogger.Log($"📢 Muhalefet {policyId} yasasına karşı halkı sokağa döktü! Meclis üzerinde baskı artıyor.");
    return pressure;
}
/// <summary>
/// Mevcut oylama için muhalefet baskısını hesaplar.
/// Faktörler: muhalefet oyuncu rolü, huzursuzluk, meşruiyet eksikliği, fraksiyon desteği.
/// 0-100 arası bir değer döndürür (yüksek = güçlü baskı).
/// </summary>
public float CalculateOppositionPressure()
{
    float pressure = 0f;

    // 1) Oyuncu muhalefetteyse +15 baskı (kendi partin aktif)
    if (CurrentRole == PlayerRole.Opposition) pressure += 15f;

    // 2) Huzursuzluk: 40'ın üstündeki her 10 puan +5 baskı
    if (Universe.Unrest > 40f)
        pressure += Math.Min(30f, (Universe.Unrest - 40f) * 0.5f);

    // 3) Meşruiyet düşüklüğü: 50'nin altındaki her 10 puan +4 baskı
    float legit = Legitimacy.CurrentLegitimacy;
    if (legit < 50f)
        pressure += Math.Min(20f, (50f - legit) * 0.4f);

    // 4) Muhalefet fraksiyonlarının toplam desteği (iktidardaki fraksiyona zıt olanlar)
    if (Factions != null)
    {
        var ruling = Factions.Factions.FirstOrDefault(f => f.Id == Factions.RulingFactionId);
        if (ruling != null)
        {
            // Oyuncunun fraksiyonuna zıt fraksiyonların desteği muhalefet baskısına dönüşür
            foreach (var f in Factions.Factions)
            {
                if (f.Id == Factions.RulingFactionId) continue;
                float ideologyDistance = Math.Abs(f.Ideology - ruling.Ideology);
                if (ideologyDistance > 60f)
                    pressure += f.Support * 0.15f;   // Zıt fraksiyon desteği baskıya dönüşür
            }
        }
    }

    // 5) Radikalleşmiş gruplar +8 baskı
    if (Universe.Radicalized != null)
    {
        int radicalCount = Universe.Radicalized.Count(kv => kv.Value);
        pressure += radicalCount * 8f;
    }

    // 6) Koalisyon bozulduysa +10
    if (Universe.Partners != null && Universe.Partners.Any(p => !p.InGovernment))
        pressure += 10f;

        if (Universe.SanctionLevel > 30f) pressure += 5f;

        // 8) FAZ 3: Aktif anti-kampanyalar
        pressure += GetAntiCampaignPressure();

        return Math.Clamp(pressure, 0f, 100f);
}

// Oylamayı gerçekleştir ve sonucu uygula
// Oylamayı gerçekleştir ve sonucu uygula
// Oylamayı gerçekleştir ve sonucu uygula
public void ResolveVote(string policyId, float oppositionPressure)
{
    var policy = ProposedPolicies.FirstOrDefault(p => p.Id == policyId);
    if (policy == null) return;

        var chamber = new DemocracySim.Engine.Legislative.LegislativeChamber();
    var result = chamber.ConductVote(policy, Actors, oppositionPressure, PartyManager, this);

    if (result.IsPassed)
    {
        // --- FAZ 0 FIX: Bürokrasi gecikmesi burada da uygulanıyor ---
        // Oyuncunun kendi önerdiği yasalarda GameManager zaten gecikme uyguluyor.
        // Ama AI yasaları veya meclis gündemindeki diğer yasalar buradan geçiyor.
        // Artık ikisi de aynı kurallara tabi: yolsuzluk → bürokrasi gecikmesi.
        // EK-7: Bürokrasi gecikmesi formülü BalanceConfig'ten
int delay = (int)Math.Floor(CorruptionLevel / BalanceConfig.Instance.BureaucracyDelayDivisor);
        if (delay <= 0)
        {
            policy.IsActive = true;
            policy.Intensity = 1.0f;
            SimLogger.Log($"✅ YASA GEÇTİ (anında): {policy.Name}. {result.Summary}");
        }
        else
        {
            // Bürokrasi kuyruğuna ekle — yasa delay tur sonra aktif olacak
            Universe.Pending.Add(new PendingPolicyState
            {
                PolicyId = policyId,
                Amount = 5f,          // Default etki miktarı
                TurnsLeft = delay
            });
            SimLogger.Log($"🏛️ YASA GEÇTİ (bürokrasi {delay} tur): {policy.Name}. {result.Summary}");
        }
    }
    else
    {
        policy.IsActive = false;
        SimLogger.Log($"❌ YASA REDDEDİLDİ: {policy.Name}. {result.Summary}");
    }

    ProposedPolicies.Remove(policy);
}

        public (string advice, float confidence) GetMinisterAdvice(string actorId, SimPolicy policy)
        {
            var actor = Actors.FirstOrDefault(a => a.Id == actorId);
            if (actor == null) return ("Bakan bulunamadı.", 0f);
            return actor.GetAdvice(policy);
        }

        public void GivePopulistPromise(string groupId, float bonus)
        {
            var group = Demographics.FirstOrDefault(g => g.Id == groupId);
            if (group != null) {
                group.AdjustSatisfaction(bonus);
                SimLogger.Log($"\n📢 {group.Name} grubuna büyük vaatler verildi!");
            }
        }

             public bool TriggerScandal()
        {
            var cfg = BalanceConfig.Instance;
    if (SimRng.NextDouble() < cfg.ScandalSuccessChance) {
        float damage = (float)(SimRng.NextDouble() * (cfg.ScandalDamageMax - cfg.ScandalDamageMin) + cfg.ScandalDamageMin);
                Legitimacy.AdjustLegitimacy(-damage);
                foreach (var g in Demographics) g.AdjustSatisfaction(-damage * 0.3f);
                // YENİ: Skandal izi — sonraki seçimde ceza
                Elections.AddScandalScar(damage * 0.5f);
                return true;
            }
            PoliticalCapital = Math.Max(0f, PoliticalCapital - 10f);
            return false;
        }

        public bool PersuadeMinister(string actorId)
        {
            var actor = Actors.FirstOrDefault(a => a.Id == actorId);
            if (actor == null) return false;
            actor.PersuasionBonus = Math.Max(actor.PersuasionBonus, 3f);
            return true;
        }

        public bool ManipulateMinister(string actorId)
        {
            var actor = Actors.FirstOrDefault(a => a.Id == actorId);
            if (actor == null) return false;
            float chance = (100f - actor.Satisfaction) * 0.5f + (PoliticalCapital / 10f);
            if (SimRng.NextDouble() * 100 < chance) {
                actor.Influence = Math.Max(0f, actor.Influence - 20f);
                actor.Ambition = Math.Max(0f, actor.Ambition - 15f);
                return true;
            }
            Legitimacy.AdjustLegitimacy(-5f);
            return false;
        }
        public bool VetoPolicy(string policyId, float cost)
{
    if (CurrentRole != PlayerRole.Opposition) return false;
    if (PoliticalCapital < cost) return false;

    var policy = AllObjects.OfType<SimPolicy>().FirstOrDefault(p => p.Id == policyId);
    if (policy == null) return false;

    PoliticalCapital -= cost;
    
    // Yasayı tamamen kapatma şansı (Sermayeye bağlı)
    if (SimRng.NextDouble() < 0.4)
    {
        policy.IsActive = false;
        SimLogger.Log($"❌ MUHALEEFET BAŞARILI: {policy.Name} yasası veto edildi ve iptal oldu!");
    }
    else
    {
        policy.Intensity *= 0.5f; // Sadece etkisini azalt
        SimLogger.Log($"⚠️ MUHALEEFET BASKISI: {policy.Name} yasası tamamen iptal edilemedi ama etkisi yarıya indirildi.");
    }
    return true;
}
public void InitializeInterestGroups()
{
    InterestGroups.Clear();
    InterestGroups.Add(new InterestGroup("business", "İş Dünyası Derneği", 70f) { Power = 70f });
    InterestGroups.Add(new InterestGroup("unions", "Sendikalar Konfederasyonu", -60f) { Power = 55f });
    InterestGroups.Add(new InterestGroup("religious", "Dini Kurumlar Birliği", 80f) { Power = 50f });
    InterestGroups.Add(new InterestGroup("media", "Medya Sahipleri Konseyi", 40f) { Power = 45f });
    InterestGroups.Add(new InterestGroup("military_lobby", "Askeri Sanayi Lobisi", 60f) { Power = 65f });
    InterestGroups.Add(new InterestGroup("ngos", "Sivil Toplum Platformu", -40f) { Power = 35f });
}

// Koalisyon ortağını kışkırtma
public bool InciteCoalitionPartner(string partnerId, float cost)
{
    if (CurrentRole != PlayerRole.Opposition) return false;
    if (PoliticalCapital < cost) return false;

    var u = Universe;
    var partner = u.Partners.FirstOrDefault(p => p.Id == partnerId);
    if (partner == null || !partner.InGovernment) return false;

    PoliticalCapital -= cost;

    // Ortağın memnuniyetini düşürerek hükümeti düşürmeye çalış
    float damage = (float)(SimRng.NextDouble() * 20 + 10);
    partner.Satisfaction -= damage;
    
    SimLogger.Log($"🗣️ KOALİSYON OPERASYONU: {partner.Name} üzerinde baskı kuruldu, hükümetle arası açılıyor!");
    return true;
}

        public bool PerformSecretOperation(string targetCountryId, OperationType type, DemocracySim.Engine.World.WorldManager world)
        {
            var target = world.Countries.FirstOrDefault(c => c.Id == targetCountryId);
            if (target == null) return false;
            float targetDefense = (target.Engine.Legitimacy.CurrentLegitimacy + 20f); 
            float successChance = Intel.CalculateSuccessChance(type, targetDefense);
            if (SimRng.NextDouble() * 100 < successChance)
            {
                switch (type)
                {
                    case OperationType.Intel: break;
                    case OperationType.Sabotage:
                        var gdp = target.Engine.AllObjects.Find(o => o.Id == "gdp");
                        if (gdp != null) gdp.ActualValue -= 5f;
                        break;
                    case OperationType.Manipulate:
                        target.Engine.Legitimacy.AdjustLegitimacy(-10f);
                        break;
                }
                return true;
            }
            world.Diplomacy.DamageRelation(this.OwnerCountryId, target.Id, 30f);
            Legitimacy.AdjustLegitimacy(-10f);
            return false;
        }
        

        /// <summary>Toplam vergi gelirini hesaplar (yürürlükteki politikalara göre).</summary>
        /// <summary>FAZ 1 (K5): Toplam gelir — sadece Revenue kategorisindeki politikalar.</summary>
public float CalculateTotalRevenue()
{
    float revenue = 0f;
    // EK-2: OfType yerine tipli liste — daha hızlı, allocation yok
    for (int i = 0; i < Registry.Policies.Count; i++)
    {
        var p = Registry.Policies[i];
        if (!p.IsActive) continue;
        if (p.BudgetType != PolicyBudgetType.Revenue) continue;
        revenue += p.GetEffectiveValue() * 0.5f;
    }
    return revenue;
}

/// <summary>FAZ 1 (K5): Toplam gider — sadece Expense kategorisindeki politikalar.</summary>
public float CalculateTotalSpending()
{
    float spending = 0f;
    foreach (var p in AllObjects.OfType<SimPolicy>())
    {
        if (!p.IsActive) continue;
        if (p.BudgetType != PolicyBudgetType.Expense) continue;
        spending += p.GetEffectiveValue() * 0.7f;
    }
    return spending;
}

        /// <summary>Bütçe dengesi (pozitif = fazla, negatif = açık).</summary>
        public float CalculateBudgetBalance()
        {
            return CalculateTotalRevenue() - CalculateTotalSpending();
        }

        /// <summary>Kategori bazında harcama dağılımı. UI için.</summary>
        public Dictionary<string, float> GetSpendingByCategory()
        {
            var categories = new Dictionary<string, float>();
            foreach (var p in AllObjects.OfType<SimPolicy>().Where(p => !p.Id.Contains("tax") && p.IsActive))
            {
                string cat = CategorizePolicy(p.Id);
                if (!categories.ContainsKey(cat)) categories[cat] = 0f;
                categories[cat] += p.GetEffectiveValue() * 0.7f;
            }
            return categories;
        }

        /// <summary>Kategori bazında gelir dağılımı.</summary>
        public Dictionary<string, float> GetRevenueByCategory()
        {
            var categories = new Dictionary<string, float>();
            foreach (var p in AllObjects.OfType<SimPolicy>().Where(p => p.Id.Contains("tax") && p.IsActive))
            {
                string cat = CategorizePolicy(p.Id);
                if (!categories.ContainsKey(cat)) categories[cat] = 0f;
                categories[cat] += p.GetEffectiveValue() * 0.5f;
            }
            return categories;
        }

        private string CategorizePolicy(string policyId)
        {
            if (policyId.Contains("tax")) return "Vergi";
            if (policyId.Contains("edu")) return "Eğitim";
            if (policyId.Contains("health")) return "Sağlık";
            if (policyId.Contains("military") || policyId.Contains("defense")) return "Savunma";
            if (policyId.Contains("infrastructure")) return "Altyapı";
            if (policyId.Contains("police")) return "Güvenlik";
            if (policyId.Contains("environment") || policyId.Contains("renewable")) return "Çevre";
            if (policyId.Contains("labor")) return "İşçi";
            return "Diğer";
        }
                /// <summary>FAZ 3: AI yasasına karşı kampanya başlat.</summary>
        public string StartAntiCampaign(string policyId, float capital)
        {
            if (CurrentRole != PlayerRole.Opposition)
                return "Sadece muhalefetteyken karşı kampanya başlatabilirsin.";
            if (PoliticalCapital < capital)
                return $"Yeterli sermaye yok ({capital:F0} gerekir).";

            var policy = AllObjects.OfType<SimPolicy>().FirstOrDefault(p => p.Id == policyId);
            if (policy == null) return "Yasa bulunamadı.";

            // Yasa zaten aktifse uyarı
            if (policy.IsActive) return "Bu yasa zaten yürürlükte.";

            // Zaten kampanya varsa uyarı
            if (ActiveAntiCampaigns.ContainsKey(policyId))
                return "Bu yasaya karşı zaten aktif bir kampanyan var.";

            PoliticalCapital -= capital;
            // EK-7: Anti-kampanya süresi BalanceConfig'ten
int duration = BalanceConfig.Instance.AntiCampaignDurationTurns;
ActiveAntiCampaigns[policyId] = duration;

            return $"{policy.Name} yasasına karşı kampanya başlatıldı. " +
                   $"Baskı {duration} tur sürecek. (-{capital:F0} sermaye)";
        }

        /// <summary>FAZ 3: Anti-kampanyaları her tur ilerlet.</summary>
        private void ProcessAntiCampaigns()
        {
            if (CurrentRole != PlayerRole.Opposition)
            {
                ActiveAntiCampaigns.Clear();
                return;
            }

            var expired = new List<string>();
            foreach (var key in ActiveAntiCampaigns.Keys.ToList())
            {
                ActiveAntiCampaigns[key]--;
                if (ActiveAntiCampaigns[key] <= 0)
                    expired.Add(key);
            }

            foreach (var key in expired)
            {
                ActiveAntiCampaigns.Remove(key);
                var policy = AllObjects.OfType<SimPolicy>().FirstOrDefault(p => p.Id == key);
                if (policy != null)
                    SimLogger.Log($"[Anti-Kampanya] {policy.Name} yasasına karşı kampanya sona erdi.");
            }
        }

        /// <summary>FAZ 3: Aktif anti-kampanya sayısı (oy baskısına katkı).</summary>
        public float GetAntiCampaignPressure()
        {
            return ActiveAntiCampaigns.Count * 15f;   // Her kampanya +15 baskı
        }

        // FAZ 3.5: Karar günlüğü — oyuncunun son 20 kararını kaydeder
public List<DecisionLogEntry> DecisionLog { get; set; } = new List<DecisionLogEntry>();
private const int MaxDecisionLog = 20;

public void LogDecision(string category, string description, float impact)
{
    DecisionLog.Insert(0, new DecisionLogEntry
    {
        Turn = CurrentTurn,
        Category = category,
        Description = description,
        Impact = impact,
        Timestamp = DateTime.Now
    });
    while (DecisionLog.Count > MaxDecisionLog)
        DecisionLog.RemoveAt(MaxDecisionLog);
}

[Serializable]
public class DecisionLogEntry
{
    public int Turn;
    public string Category;
    public string Description;
    public float Impact;
    public DateTime Timestamp;
}

// ============================================================
// FAZ R: Motor Refactor — ProcessTurn() parçaları
// Bu metotlar eskiden ProcessTurn() içindeydi; şimdi ayrı sistemler çağırıyor.
// ============================================================

/// <summary>Anti-kampanyaları ilerlet (Pipeline için public).</summary>
public void ProcessAntiCampaignsPublic() => ProcessAntiCampaigns();

/// <summary>Simülasyon iterasyon döngüsü — denge noktasına yaklaşma.</summary>
public void RunSimulationIterations()
{
    for (int i = 0; i < IterationsPerTurn; i++)
    {
        foreach (var obj in AllObjects)
        {
            float totalImpact = 0;
            SimEffect best = null;
            float maxAbs = -1;
            foreach (var eff in obj.IncomingEffects)
            {
                float imp = eff.CalculateImpact() * Legitimacy.GetEffectMultiplier();
                totalImpact += imp;
                if (System.Math.Abs(imp) > maxAbs) { maxAbs = System.Math.Abs(imp); best = eff; }
            }
            obj.TopImpactor = best;
            float reversion = obj.ReversionRate * (obj.EquilibriumValue - obj.ActualValue);
            obj.TargetValue = obj.ActualValue + totalImpact + reversion;
        }
        foreach (var obj in AllObjects)
        {
            float inertia = System.Math.Clamp(obj.Inertia, 0.01f, 1f);
            obj.ActualValue = obj.ActualValue + (obj.TargetValue - obj.ActualValue) * inertia;
            obj.Clamp();
        }
    }
}

/// <summary>Geçmiş kaydı + bilgi filtresi (PerceivedValue).</summary>
public void RecordHistoryAndFilterInfo()
{
    foreach (var obj in AllObjects) obj.RecordHistory();

    var observer = Actors.FirstOrDefault(a => a.Role == ActorRole.Minister);
    if (observer != null)
    {
        foreach (var obj in AllObjects)
        {
            obj.PerceivedValue = InformationLayer.FilterTruth(obj.ActualValue, observer, obj);
            obj.Clamp();
        }
    }
}

/// <summary>Demografik memnuniyet etkisi + meşruiyet hesabı.</summary>
public void ApplyDemographicImpact()
{
    foreach (var group in Demographics)
    {
        float impact = 0;
        foreach (var pol in AllObjects.OfType<SimPolicy>().Where(p => p.IsActive))
            if (pol.GroupImpacts.TryGetValue(group.Id, out float val))
                impact += (pol.GetEffectiveValue() / 100f) * val;
        group.AdjustSatisfaction(impact);
    }
    Legitimacy.RecalculateFromDemographics(Demographics);
}

/// <summary>Teknoloji GSYİH bonusu + yaptırım direnci.</summary>
public void ApplyTechBonuses()
{
    var gdpObj = Registry.Get(ObjectRegistry.Ids.Gdp);
if (gdpObj != null)
{
    float techBonus = Technology.GetGdpBonus() * 0.02f;
    gdpObj.ActualValue = System.Math.Clamp(gdpObj.ActualValue + techBonus, gdpObj.MinValue, gdpObj.MaxValue);
}
    if (Universe.SanctionLevel > 0f)
    {
        float resistance = Technology.GetSanctionResistance() * 0.01f;
        Universe.SanctionLevel = System.Math.Max(0f, Universe.SanctionLevel - resistance);
    }
}

/// <summary>Tur sayaçları: seçim, sermaye, yolsuzluk, bürokrasi.</summary>
public void AdvanceTurnCounters()
{
    var cfg = BalanceConfig.Instance;
    if (TurnUntilElection > 0) TurnUntilElection--;
    CurrentTurn++;

    // EK-7: Sabitler BalanceConfig'ten
    float capitalGain = CurrentRole == PlayerRole.Governing
        ? cfg.CapitalGainGoverning
        : cfg.CapitalGainOpposition;
    PoliticalCapital = System.Math.Clamp(PoliticalCapital + capitalGain, 0f, MaxPoliticalCapital);

    CorruptionLevel = System.Math.Max(0f, CorruptionLevel - cfg.CorruptionDecayPerTurn);

    // Yolsuzluk + derin devlet → bürokrasi hızı
    float bureaucracyFactor = System.Math.Clamp(
        (DeepStateStability / 100f) * (1f - CorruptionLevel / 200f), 0.1f, 1f);
    foreach (var pol in AllObjects.OfType<SimPolicy>())
        pol.ImplementationSpeed = bureaucracyFactor;
}

        /// <summary>
/// FAZ R (Motor Refactor): Artık tüm alt sistemler TurnSystemRegistry üzerinden çalışır.
/// Metodun kendisi sadece pipeline'ı çağırır + olay kontrolü yapar.
/// </summary>
// EK-9: EventCheckSystem instance'ı sakla (Pipeline'da çalışır)
private EventCheckSystem _eventCheckSystem;

public GameEvent ProcessTurn()
{
    // EK-9: Tüm sistemler pipeline içinde çalışır — EventCheck dahil
    _turnRegistry.ProcessAll(this);
    return _eventCheckSystem?.LastTriggeredEvent;
}
// FAZ R: Tur sistemleri pipeline'ı
private readonly TurnSystemRegistry _turnRegistry = new TurnSystemRegistry();



private void RegisterAllTurnSystems()
{
    _turnRegistry.Register(new VoterTransitionSystem());
    _turnRegistry.Register(new FactionSystem());
    _turnRegistry.Register(new ShadowCabinetSystem());
    _turnRegistry.Register(new PartyBudgetSystem());
    _turnRegistry.Register(new ProtestSystem());
    _turnRegistry.Register(new JudiciarySystem());
    _turnRegistry.Register(new AntiCampaignSystem());
    _turnRegistry.Register(new SectorSystem());
    _turnRegistry.Register(new MediaOutletSystem());
    _turnRegistry.Register(new TechnologySystem());
    _turnRegistry.Register(new CrisisChainSystem());
    _turnRegistry.Register(new EconomySystem());
    _turnRegistry.Register(new ArmySystem());
    _turnRegistry.Register(new IntelSystem());
    _turnRegistry.Register(new MinisterPassiveSystem());
    _turnRegistry.Register(new SimulationIterationSystem());
    _turnRegistry.Register(new HistoryAndInfoSystem());
    _turnRegistry.Register(new DemographicImpactSystem());
    _turnRegistry.Register(new ActorEvaluationSystem());
    _turnRegistry.Register(new CabinetIntrigueSystem());
    _turnRegistry.Register(new CoalitionSystem());
    // EK-13: Muhalefet momentum sistemi
_turnRegistry.Register(new OppositionMomentumSystem());
    _turnRegistry.Register(new MediaNewsSystem());
    _turnRegistry.Register(new UniverseSystem());
    _turnRegistry.Register(new MigrationSystem());
    _turnRegistry.Register(new AchievementSystem());
    _turnRegistry.Register(new TechBonusSystem());
    _turnRegistry.Register(new TurnCounterSystem());
    // EK-24: Adaylık krizi uyarısı
_turnRegistry.Register(new CandidateCrisisSystem());
_turnRegistry.Register(new MinistryMarketSystem());
_turnRegistry.Register(new EconomicCrisisSystem()); 

// EK-9: EventCheckSystem en son çalışır
_eventCheckSystem = new EventCheckSystem();
_turnRegistry.Register(_eventCheckSystem);
    // EK-9: EventCheckSystem en son çalışır
    _eventCheckSystem = new EventCheckSystem();
    _turnRegistry.Register(_eventCheckSystem);
}
    }
}
