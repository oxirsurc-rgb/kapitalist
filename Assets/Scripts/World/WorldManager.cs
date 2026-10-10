using System;
using System.Collections.Generic;
using System.Linq;
using DemocracySim.Engine.Core;
using DemocracySim.Engine.World;

namespace DemocracySim.Engine.World
{
    public class WorldManager
    {
        // A.2 DÜZELTMESİ: Tek bir static Random — tüm çağrılar bunu kullanacak.
        // Eski kodda her yerde "new Random()" vardı ve aynı milisaniyede
        // aynı seed'i aldıkları için "rastgele" davranış deterministik oluyordu.

        public List<Country> Countries { get; set; } = new List<Country>();
    
        public DiplomacyManager Diplomacy { get; set; } = new DiplomacyManager();
        public GlobalCrisisManager GlobalCrisis { get; set; } = new GlobalCrisisManager();
        public List<GlobalOrganization> Organizations { get; set; } = new List<GlobalOrganization>();
        public float GlobalTension { get; set; } = 10f;
        public int GlobalTurn { get; set; } = 0;

        // FAZ 4: ortak küresel durum ve ticaret ağı
        public TradeNetwork Trade { get; set; } = new TradeNetwork();
        public float GlobalGrowth { get; set; } = 0f;    // -1.5 .. +1.0 : tüm ülkelerin tur başı GSYİH eğilimi
        public float OilPrice { get; set; } = 100f;      // 40 .. 220 : yüksekse küresel büyüme düşer

        public WorldManager()
        {
            InitializeOrganizations();
        }

        private void InitializeOrganizations()
        {
            // NATO Benzeri: Batı odaklı askeri blok
            var nato = new GlobalOrganization("nato", "Kuzey Atlantik Paktı", OrgType.Military, -70f)
            {
                RequiredLegitimacy = 40f,
                RequiredGdp = 55f,          // FAZ 0/4 FIX: GSYİH 0-100 ölçeğinde; eski 200 eşiği hiç ulaşılamazdı
                DuesPerTurn = 1f
            };

            // Avrasya Birliği Benzeri: Doğu odaklı askeri/ekonomik blok
            var eurasian = new GlobalOrganization("eurasian", "Avrasya Birliği", OrgType.Military, 70f)
            {
                RequiredLegitimacy = 40f,
                RequiredGdp = 55f,
                DuesPerTurn = 1f
            };

            // BM Benzeri: Küresel diplomatik yapı
            var un = new GlobalOrganization("un", "Birleşmiş Milletler", OrgType.Diplomatic, 0f)
            {
                RequiredLegitimacy = 30f,
                RequiredGdp = 0f,
                JoinTolerance = 80f         // BM herkese açık (ideolojik filtre gevşek)
            };

            Organizations.Add(nato);
            Organizations.Add(eurasian);
            Organizations.Add(un);

            // --- YENİ (C.4): Ekonomik örgüt örneği ---
            var wto = new GlobalOrganization("wto", "Dünya Ticaret Örgütü", OrgType.Economic, 0f)
            {
                RequiredLegitimacy = 50f,
                RequiredGdp = 60f,
                JoinTolerance = 80f
            };
            Organizations.Add(wto);
        }

        // ---------------- Yardımcılar (FAZ 4) ----------------
        public float GdpOf(Country c)
    => c.Engine.Registry.GetValue(ObjectRegistry.Ids.Gdp, 0f);

public void AddGdp(Country c, float delta)
{
    var gdp = c.Engine.Registry.Get(ObjectRegistry.Ids.Gdp);
    if (gdp == null) return;
    gdp.ActualValue += delta;
    gdp.Clamp();
}

        /// <summary>Oyuncu ülkesiyse haberi turun mesajlarına kuyruklar; her durumda loglar.</summary>
        public void NotifyCountry(Country c, string text, bool warn = false)
        {
            if (c.IsPlayerControlled) UniverseSystems.QueueWorldNews(c.Engine, text, warn);
            else SimLogger.Log($"[{c.Name}] {text}");
        }

        /// <summary>Küresel konjonktür: ortalamaya döner, gerginlik ve petrol fiyatı büyümeyi baskılar.</summary>
        private void UpdateGlobalState()
        {
            float noise = (float)(SimRng.NextDouble() * 2 - 1);
            OilPrice = Math.Clamp(OilPrice + (100f - OilPrice) * 0.08f + (GlobalTension - 20f) * 0.1f + noise * 4f, 40f, 220f);
            GlobalGrowth += (0f - GlobalGrowth) * 0.1f + noise * 0.15f
                            - (GlobalTension - 30f) * 0.003f - (OilPrice - 100f) * 0.002f;
            GlobalGrowth = Math.Clamp(GlobalGrowth, -1.5f, 1.0f);
            GlobalTension = Math.Clamp(GlobalTension - 0.2f, 0f, 100f);   // yavaş yatışma

            foreach (var c in Countries)
            {
                c.Engine.Universe.GlobalGrowthModifier = GlobalGrowth * 0.5f;
                c.Engine.Universe.GlobalTensionPressure = GlobalTension;
            }
        }

        /// <summary>AI ülkelerin yasa oylamaları ve bürokrasi kuyruğu (eskiden hiç işlenmiyordu).</summary>
        private static void ResolveAIPolicies(Country c)
        {
            var e = c.Engine;
            var pending = e.Universe.Pending;
            for (int i = pending.Count - 1; i >= 0; i--)
            {
                var p = pending[i];
                p.TurnsLeft--;
                if (p.TurnsLeft > 0) continue;
                var pol = e.AllObjects.OfType<SimPolicy>().FirstOrDefault(x => x.Id == p.PolicyId);
                if (pol != null)
                {
                    pol.IsActive = true;
                    if (pol.Intensity <= 0f) pol.Intensity = 1f;
                    pol.ActualValue = Math.Clamp(pol.ActualValue + p.Amount, 0f, 100f);
                }
                pending.RemoveAt(i);
            }
            if (e.ProposedPolicies.Count > 0)
            {
                float pressure = e.CalculateOppositionPressure();
                foreach (var pol in new List<SimPolicy>(e.ProposedPolicies)) e.ResolveVote(pol.Id, pressure);
            }
        }

        public void ProcessWorldTurn()
        {
            GlobalTurn++;

            // 0. Ortak küresel durum
            UpdateGlobalState();

            // 1. Küresel Kriz Kontrolü
            var crisis = GlobalCrisis.CheckForGlobalCrisis();
            if (crisis != null)
            {
                SimLogger.Log($"🌍 !!! KÜRESEL OLAY: {crisis.Title} !!! - {crisis.Description}", SimLogger.LogLevel.Warning);
                crisis.Effect(this);
                // Krizler küresel konjonktüre de yansır
                if (crisis.Title.Contains("EKONOMİK")) GlobalGrowth = Math.Max(-1.5f, GlobalGrowth - 1.0f);
                else if (crisis.Title.Contains("PANDEMİ")) GlobalGrowth = Math.Max(-1.5f, GlobalGrowth - 0.6f);
                else if (crisis.Title.Contains("GERGİNLİK")) GlobalTension = Math.Min(100f, GlobalTension + 8f);
                foreach (var c in Countries) NotifyCountry(c, $"🌍 {crisis.Title}: {crisis.Description}", true);
            }

            // 2. Organizasyon ve Blok Etkileri
            ProcessOrganizationDynamics();

            // 3. Ticaret ağı
            Trade.ProcessTurn(this);

            // 4. Süper Güçlerin Hamleleri
            ProcessSuperpowerMoves();

            // 5. Devrim bulaşması ve göç (ülke motorlarından önce)
            WorldDynamics.ProcessContagion(this);
            WorldDynamics.ProcessMigration(this);

            // 6. Ülkelerin İç İşleri
            foreach (var country in Countries)
            {
                if (country.IsPlayerControlled) continue;
                ResolveAIPolicies(country);
                country.Engine.ProcessTurn();
                AIStateController.ExecuteAITurn(country, this);
            }

            // 7. Diplomatik Güncellemeler
            Diplomacy.UpdateGlobalRelations(this);
                        // FAZ 4: Diplomatik hafıza güncellemesi
            foreach (var c in Countries)
            {
                c.Memory.ProcessTurn(GlobalTurn);
            }

                // FAZ 17: AI Director — küresel zorluk
if (Director == null) Director = new AIDirector();
Director.ProcessTurn(this);

foreach (var c in Countries.Where(c => !c.IsPlayerControlled))
{
    c.Engine.Universe.GlobalGrowthModifier *= Director.GetAIStatMultiplier();
}
        }
        public AIDirector Director { get; set; }

        private void ProcessOrganizationDynamics()
        {
            foreach (var org in Organizations)
            {
                // Üyelik sonuçları (ihraç, aidat, lider, türe özgü etkiler)
                org.ProcessTurn(this);

                foreach (var country in Countries)
                {
                    if (org.MemberIds.Contains(country.Id)) continue;
                    // Oyuncu menüden kendi katılır; AI burada otomatik başvurur
                    if (country.IsPlayerControlled) continue;

                    if (org.CanJoin(country) && SimRng.NextDouble() < 0.05)
                    {
                        org.Join(country, this);
                        SimLogger.Log($"🌍 DİPLOMASİ: {country.Name}, {org.Name} organizasyonuna katıldı!");
                        foreach (var p in Countries.Where(x => x.IsPlayerControlled))
                            NotifyCountry(p, $"🌍 {country.Name}, {org.Name} örgütüne katıldı.");
                    }
                }

                if (org.Type == OrgType.Military) UpdateBlockTension(org);
            }
        }

        private void UpdateBlockTension(GlobalOrganization org)
        {
            if (org.MemberIds.Count > 3)
            {
                GlobalTension += 0.1f;
            }
            GlobalTension = Math.Clamp(GlobalTension, 0f, 100f);
        }

        public void ApplyBlockSanctions(string targetCountryId, string orgId)
        {
            var org = Organizations.FirstOrDefault(o => o.Id == orgId);
            if (org == null) return;

            foreach (var memberId in org.MemberIds)
            {
                Diplomacy.DamageRelation(memberId, targetCountryId, 20f);
            }

            GlobalTension += 5f;
            SimLogger.Log($"🚫 YAPTIRIM: {org.Name} üyeleri, {targetCountryId} ülkesine karşı ortak yaptırım başlattı!");
        }

        // ================================================================
        // JEOPOLİTİK MÜDAHALE SİSTEMİ (A.3 DÜZELTİLDİ)
        // ================================================================
        public void ForeignIntervention(string superpowerId, string targetCountryId, bool supportOpposition)
        {
            var target = Countries.FirstOrDefault(c => c.Id == targetCountryId);
            if (target == null) return;

            var superpower = Countries.FirstOrDefault(c => c.Id == superpowerId);
            if (superpower == null) return;

            if (supportOpposition)
            {
                // Süper güç muhalefeti destekliyor: hükümetin meşruiyeti düşer
                target.Engine.Legitimacy.AdjustLegitimacy(-5f);

                if (target.IsPlayerControlled
                    && target.Engine.CurrentRole == SimulationEngine.PlayerRole.Opposition)
                {
                    target.Engine.PoliticalCapital += 20f;
                    SimLogger.Log($"🌍 DIŞ DESTEK: Süper güç {superpower.Name}, sizin partinizi fonladı! (+20 Sermaye)");
                }
            }
            else
            {
                // A.3 DÜZELTMESİ: Süper güç İKTİDARI destekliyor.
                // Eski kod burada da +20 sermaye veriyordu (kopyala-yapıştır hatası).
                // Artık iktidarı destekleyen süper güç, muhalefetteki oyuncunun
                // meşruiyetini dolaylı olarak düşürür — sermaye VERMEZ.
                target.Engine.Legitimacy.AdjustLegitimacy(5f);
                foreach (var g in target.Engine.Demographics) g.AdjustSatisfaction(-2f);

                if (target.IsPlayerControlled
                    && target.Engine.CurrentRole == SimulationEngine.PlayerRole.Opposition)
                {
                    target.Engine.Legitimacy.AdjustLegitimacy(-3f);
                    // EK-6 FIX: UIManager.Instance yerine EventBus
EventBus.Publish(new NotificationEvent
{
    Message = $"🌍 {superpower.Name} iktidarı destekliyor; muhalefet olarak baskı altındasınız.",
    IsWarning = true
});
                }
            }

            Diplomacy.DamageRelation(superpowerId, targetCountryId, 10f);
            GlobalTension += 1f;
             // FAZ 4: Trust skoru etkisi — BURAYA EKLE
            if (target.IsPlayerControlled)
                superpower.Memory.ChangeTrust(target.Id, -20f, "Dış müdahale yapıldı");
            if (superpower.IsPlayerControlled)
                target.Memory.ChangeTrust(superpower.Id, -25f, "Dış müdahale edildi");
        }
        

        public void ProcessSuperpowerMoves()
        {
            // FAZ 0 FIX: GSYİH 0-100 ölçeğindedir; süper güç = GSYİH'si en az 70 olan en güçlü 2 ülke
            var superpowers = Countries.Where(c => GdpOf(c) >= 70f)
                                       .OrderByDescending(c => GdpOf(c)).Take(2).ToList();

            foreach (var sp in superpowers)
            {
                // A.2: static Rng
                if (SimRng.NextDouble() < 0.10)
                {
                    if (Countries.Count == 0) continue;
                    var target = Countries[SimRng.Next(Countries.Count)];
                    if (target == null || target.Id == sp.Id) continue;

                    bool supportOpp = Math.Abs(sp.GlobalAlignment - target.GlobalAlignment) > 50f;
                    ForeignIntervention(sp.Id, target.Id, supportOpp);
                }
            }
        }
    }
}