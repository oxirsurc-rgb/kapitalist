using System;
using System.Collections.Generic;
using System.Linq;

namespace DemocracySim.Engine.Core
{
    /// <summary>
    /// FAZ 3: Göç yönetimi — entegrasyon, diaspora, beyin göçü/kazancı.
    /// </summary>
    [Serializable]
    public class MigrationManager
    {
        // ============================================================
        // DURUMLAR
        // ============================================================
        
        /// <summary>Göçmen entegrasyon skoru (0-100). 30 altı → sosyal gerginlik.</summary>
        public float IntegrationScore { get; set; } = 50f;
        
        /// <summary>Son 10 turda göç akışı (diaspora havalesi için).</summary>
        public List<float> MigrationHistory { get; set; } = new List<float>();
        
        /// <summary>Bu turda gelen göçmen sayısı (normalize 0-10).</summary>
        public float NewArrivalsThisTurn { get; set; } = 0f;
        
        /// <summary>Toplam göçmen nüfusu (kümülatif, normalize 0-100).</summary>
        public float TotalMigrantPopulation { get; set; } = 0f;
        
        /// <summary>Diaspora birikmiş havalesi (her 5 turda ödenir).</summary>
        public float PendingRemittance { get; set; } = 0f;

        // ============================================================
        // SABİTLER
        // ============================================================
        
        private const float IntegrationGrowthGood = 2.0f;    // İyi koşullarda entegrasyon artışı
        private const float IntegrationDecayBad = 1.5f;      // Kötü koşullarda erozyon
        private const int RemittanceInterval = 5;            // Her 5 turda havale
        
        // ============================================================
        // TUR DÖNGÜSÜ
        // ============================================================
        
        public void ProcessTurn(SimulationEngine e)
        {
            // 1) Entegrasyon güncellemesi
            UpdateIntegration(e);
            
            // 2) Diaspora havalesi (5 turda bir)
            if (e.CurrentTurn > 0 && e.CurrentTurn % RemittanceInterval == 0)
                ProcessRemittance(e);
            
            // 3) Beyin göçü/kazancı → teknoloji seviyesi
            ProcessBrainFlow(e);
            
            // 4) Göçmen iş gücü katkısı → GSYİH
            ProcessMigrantLabor(e);
            
            // 5) Sosyal gerginlik (düşük entegrasyon)
            ProcessSocialTension(e);
            
            // 6) Göç geçmişini güncelle
            MigrationHistory.Add(e.Universe.MigrationBalance);
            if (MigrationHistory.Count > 10)
                MigrationHistory.RemoveAt(0);
        }

        // ============================================================
        // 1) ENTEGRASYON
        // ============================================================
        
        private void UpdateIntegration(SimulationEngine e)
        {
            // Entegrasyon hızı: işsizlik düşükse ve muhafazakarlar mutluysa yükselir
            float unemployment = e.AllObjects.FirstOrDefault(o => o.Id == "unemployment")?.ActualValue ?? 10f;
            float conservatives = e.Demographics.FirstOrDefault(g => g.Id == "conservatives")?.Satisfaction ?? 50f;
            float workers = e.Demographics.FirstOrDefault(g => g.Id == "workers")?.Satisfaction ?? 50f;
            
            bool goodConditions = unemployment < 12f && conservatives >= 50f && workers >= 50f;
            
            if (goodConditions)
            {
                IntegrationScore = Math.Clamp(IntegrationScore + IntegrationGrowthGood, 0f, 100f);
            }
            else
            {
                // Yüksek göçmen nüfusu + kötü koşullar → entegrasyon erozyonu
                float stressFactor = Math.Max(0f, TotalMigrantPopulation / 50f);
                IntegrationScore = Math.Clamp(
                    IntegrationScore - IntegrationDecayBad * (1f + stressFactor * 0.5f), 0f, 100f);
            }
        }

        // ============================================================
        // 2) DİASPORA HAVALESİ
        // ============================================================
        
        private void ProcessRemittance(SimulationEngine e)
{
    if (MigrationHistory.Count < 3) return;

    float avgMigration = MigrationHistory.Average();
    if (avgMigration >= 0f) return;

    float remittance = Math.Abs(avgMigration) * 5f;
    PendingRemittance += remittance;

    // EK-17: Diaspora havalesi artık ekonomiye katkı sağlıyor (GSYİH + küçük sermaye).
    // Eskiden sadece PoliticalCapital'e ekleniyordu → tasarım tutarsızlığı.
    var gdp = e.Registry.Get(ObjectRegistry.Ids.Gdp);
    if (gdp != null)
        gdp.ActualValue = Math.Clamp(gdp.ActualValue + remittance * 0.3f, gdp.MinValue, gdp.MaxValue);

    // Havalenin %20'si politik sermaye olarak (hükümetin itibarı için)
    e.PoliticalCapital = Math.Clamp(e.PoliticalCapital + remittance * 0.2f, 0f, e.MaxPoliticalCapital);

    e.Universe.Messages.Add(new UniverseMessage
    {
        Text = $"💸 DİASPORA HAVALESİ: Yurt dışındaki vatandaşlar {remittance:F1} birim gönderdi. (+GSYİH, +sermaye)",
        IsWarning = false
    });

    PendingRemittance = 0f;
}
        // ============================================================
        // 3) BEYİN GÖÇÜ/KAZANCI
        // ============================================================
        
        private void ProcessBrainFlow(SimulationEngine e)
{
    var intellectuals = e.Demographics.FirstOrDefault(g => g.Id == "intellectuals");
    if (intellectuals == null) return;

    // EK-14: Teknoloji artık TechnologyManager.TechLevel üzerinden tek kaynaktan okunuyor/yazılıyor.
    // (Bu, objects.json'daki tech_level objesine de yansır.)
    if (intellectuals.Satisfaction < 40f)
    {
        float brainLoss = (40f - intellectuals.Satisfaction) / 40f * 0.5f;
        e.Technology.TechLevel = Math.Max(0f, e.Technology.TechLevel - brainLoss);
    }
    else if (intellectuals.Satisfaction > 70f)
    {
        float brainGain = (intellectuals.Satisfaction - 70f) / 30f * 0.3f;
        e.Technology.TechLevel = Math.Min(100f, e.Technology.TechLevel + brainGain);
    }
}

        // ============================================================
        // 4) GÖÇMEN İŞ GÜCÜ KATKISI
        // ============================================================
        
        private void ProcessMigrantLabor(SimulationEngine e)
        {
            if (TotalMigrantPopulation < 5f) return;
            
            // Entegre olmuş göçmenler GSYİH'ya katkı sağlar
            float integrationFactor = IntegrationScore / 100f;
            float gdpBonus = TotalMigrantPopulation * 0.02f * integrationFactor;
            
            var gdp = e.Registry.Get(ObjectRegistry.Ids.Gdp);
if (gdp != null)
    gdp.ActualValue = Math.Clamp(gdp.ActualValue + gdpBonus, gdp.MinValue, gdp.MaxValue);
        }

        // ============================================================
        // 5) SOSYAL GERGİNLİK
        // ============================================================
        
        private void ProcessSocialTension(SimulationEngine e)
        {
            if (IntegrationScore >= 30f) return;
            
            // Düşük entegrasyon → huzursuzluk
            float tension = (30f - IntegrationScore) / 30f * 2f;
            e.Universe.Unrest = Math.Clamp(e.Universe.Unrest + tension, 0f, 100f);
            
            // Ayrıca muhafazakarlar mutsuz olur
            var conservatives = e.Demographics.FirstOrDefault(g => g.Id == "conservatives");
            if (conservatives != null)
                conservatives.AdjustSatisfaction(-tension * 0.5f);
        }

        // ============================================================
        // OLAY TETİKLEME (ProcessMigration'dan çağrılır)
        // ============================================================
        
        /// <summary>Bir göç dalgası olduğunda çağrılır.</summary>
        public void OnMigrantArrival(float intensity)
        {
            NewArrivalsThisTurn += intensity;
            TotalMigrantPopulation = Math.Clamp(TotalMigrantPopulation + intensity, 0f, 100f);
            
            // Yeni göçmenler entegrasyonu düşürür
            IntegrationScore = Math.Clamp(IntegrationScore - intensity * 0.5f, 0f, 100f);
        }

        // ============================================================
        // RAPORLAMA
        // ============================================================
        
        public string GetIntegrationLabel()
        {
            if (IntegrationScore >= 70f) return "Mükemmel";
            if (IntegrationScore >= 50f) return "İyi";
            if (IntegrationScore >= 30f) return "Orta";
            if (IntegrationScore >= 15f) return "Zayıf";
            return "Kritik";
        }

        public List<string> GetReport()
        {
            return new List<string>
            {
                $"Göçmen nüfusu: %{TotalMigrantPopulation:F1}",
                $"Entegrasyon: %{IntegrationScore:F0} ({GetIntegrationLabel()})",
                $"Bu tur gelen: {NewArrivalsThisTurn:F1}",
                $"Diaspora havalesi: {PendingRemittance:F1}"
            };
        }
    }
}