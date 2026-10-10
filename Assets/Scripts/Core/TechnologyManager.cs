using System;
using System.Collections.Generic;
using System.Linq;

namespace DemocracySim.Engine.Core
{
    /// <summary>
    /// FAZ 2: Teknoloji sistemi. Ar-Ge yatırımı, patent birikimi ve
    /// teknolojinin ekonomiye/askeriye etkisini yönetir.
    /// </summary>
    [Serializable]
    public class TechnologyManager
    {

   

    // EK-14: Teknoloji artık objects.json'daki tech_level objesinde tutulur.
    // Bu manager sadece bir facade — iki yerde tutulma sorununu çözer.
    private SimulationEngine _engine;
    private float _fallbackTechLevel = 30f;   // Engine bağlanmadan önce kullanılır

    public void BindEngine(SimulationEngine engine) => _engine = engine;

    /// <summary>
    /// Teknoloji seviyesi — objects.json'daki tech_level objesinden okunur/yazılır.
    /// Tek doğruluk kaynağı budur.
    /// </summary>
    public float TechLevel
    {
        get
        {
            if (_engine?.Registry != null)
            {
                var obj = _engine.Registry.Get("tech_level");
                if (obj != null) return obj.ActualValue;
            }
            return _fallbackTechLevel;
        }
                set
        {
            _fallbackTechLevel = Math.Clamp(value, 0f, 100f);
            if (_engine?.Registry != null)
            {
                var obj = _engine.Registry.Get("tech_level");
                if (obj != null) obj.ActualValue = _fallbackTechLevel;
            }
        }
    }

    // --- Mevcut property'ler (PatentCount, RndBudget vs) aynen kalır ---



        // ============================================================
        // ANA GÖSTERGELER
        // ============================================================

        /// <summary>Genel teknoloji seviyesi (0-100).</summary>
        

        /// <summary>Birikmiş patent sayısı (0-N). Ne kadar yüksekse o kadar yenilikçi.</summary>
        public int PatentCount { get; set; } = 5;

        /// <summary>Ar-Ge bütçesi (0-100). Politika değeri buradan okunur.</summary>
        public float RndBudget { get; set; } = 30f;

        /// <summary>Beyin göçü dengesi (-5 .. +5). Pozitif = beyin kazanıyor.</summary>
        public float BrainGain { get; set; } = 0f;

        /// <summary>Bir sonraki patent için gereken birikim (0-100).</summary>
        public float PatentProgress { get; set; } = 0f;

        // ============================================================
        // SABITLER
        // ============================================================

        private const float TechGrowthPerTurn = 0.15f;
        private const float PatentThresholdBase = 100f;
        private const float TechDecay = 0.05f;          // Bakımsız teknoloji geriler
        private const float MaxTechLevel = 100f;
        private const int MaxPatents = 200;

        // ============================================================
        // TUR DÖNGÜSÜ
        // ============================================================

        public void ProcessTurn(SimulationEngine e)
        {
            // 1. Ar-Ge bütçesini politika değerinden al
            var rndPolicy = e.AllObjects.OfType<SimPolicy>().FirstOrDefault(p => p.Id == "rnd_budget");
            if (rndPolicy != null && rndPolicy.IsActive)
                RndBudget = rndPolicy.GetEffectiveValue();
            else
                RndBudget = Math.Max(0f, RndBudget - 2f);   // Politika pasifse yavaş erir

            // 2. Eğitim seviyesi teknolojiyi besler
float eduLevel = e.Registry.GetValue(ObjectRegistry.Ids.EducationLevel, 50f);
            // 3. Ar-Ge bütçesi + eğitim → teknoloji büyümesi
            float growthInput = (RndBudget * 0.6f + eduLevel * 0.4f) / 100f;   // 0-1 arası
            float growth = growthInput * TechGrowthPerTurn * 10f;                // 0-1.5 / tur

            // 4. Beyin göçü: entelektüeller mutlu değilse teknoloji büyümesi yavaşlar
            var intellectuals = e.Demographics.FirstOrDefault(g => g.Id == "intellectuals");
            if (intellectuals != null)
            {
                if (intellectuals.Satisfaction < 30f) growth *= 0.4f;    // Beyin kaçıyor
                else if (intellectuals.Satisfaction > 70f) growth *= 1.2f; // Beyin akıyor
            }

            // 5. Yolsuzluk teknolojiyi yavaşlatır
            growth *= (1f - e.CorruptionLevel / 200f);   // %50 yolsuzluk = %25 yavaşlama

            // 6. Teknoloji seviyesini güncelle
            TechLevel = Math.Clamp(TechLevel + growth - TechDecay, 0f, MaxTechLevel);

            // 7. Patent birikimi (yeni patent eşiği teknolojiyle birlikte artar)
            PatentProgress += growth * 2f;
            float threshold = PatentThresholdBase * (1f + PatentCount * 0.1f);
            while (PatentProgress >= threshold && PatentCount < MaxPatents)
            {
                PatentProgress -= threshold;
                PatentCount++;
                threshold = PatentThresholdBase * (1f + PatentCount * 0.1f);

                SimLogger.Log($"[Teknoloji] Yeni patent alındı! Toplam: {PatentCount}");
            }

            // 8. Beyin göçü göstergesi
            if (intellectuals != null)
            {
                BrainGain = (intellectuals.Satisfaction - 50f) / 10f;   // -5 .. +5
                BrainGain = Math.Clamp(BrainGain, -5f, 5f);
            }
        }

        // ============================================================
        // ETKİLERİ
        // ============================================================

        /// <summary>Teknolojinin GSYİH'ya katkısı (%0 .. +30).</summary>
        public float GetGdpBonus() => TechLevel * 0.30f;

        /// <summary>Teknolojinin askeri güce katkısı (%0 .. +25).</summary>
        public float GetMilitaryBonus() => TechLevel * 0.25f;

        /// <summary>Teknolojinin yaptırım direncine katkısı (%0 .. +50).</summary>
        public float GetSanctionResistance() => Math.Min(50f, TechLevel * 0.5f);

        /// <summary>Teknolojinin yabancı yatırım çekimine katkısı (%0 .. +100).</summary>
        public float GetForeignInvestment() => PatentCount * 2f;

        /// <summary>Yeni patentler için harcanan toplam kaynak (bilgi amaçlı).</summary>
        public float GetTotalRndInvestment() => PatentCount * PatentThresholdBase;

        // ============================================================
        // RAPORLAMA
        // ============================================================

        public List<string> GetReport()
        {
            var lines = new List<string>
            {
                $"Teknoloji Seviyesi: %{TechLevel:F1}",
                $"Patent Sayısı: {PatentCount}",
                $"Ar-Ge Bütçesi: %{RndBudget:F0}",
                $"Beyin Dengesi: {BrainGain:+0.0;-0.0;0}",
                $"Patent Birikimi: %{PatentProgress:F0}",
                "",
                $"GSYİH Bonusu: +%{GetGdpBonus():F1}",
                $"Askeri Bonus: +%{GetMilitaryBonus():F1}",
                $"Yaptırım Direnci: +%{GetSanctionResistance():F0}",
            };
            return lines;
        }
    }
}