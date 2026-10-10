using System;
using System.Collections.Generic;
using System.Linq;

namespace DemocracySim.Engine.Core
{
    public class EconomyManager
    {
        public float Inflation { get; private set; } = 2.0f; // %2 başlangıç
        public float NationalDebt { get; private set; } = 1000f; // Milyar birim
        public float CreditRating { get; private set; } = 100f; // 100 = AAA, 0 = İflas

        // Kayıt/Yükleme sistemi için: özel setter'lı alanları dışarıdan geri yükleyebilmek amacıyla
        public void LoadState(float inflation, float nationalDebt, float creditRating)
        {
            Inflation = inflation;
            NationalDebt = nationalDebt;
            CreditRating = creditRating;
        }

        // Kriz sistemi ve olaylar için: enflasyonu doğrudan (delta ile) etkileme imkanı
        public void AdjustInflation(float delta) => Inflation = Math.Clamp(Inflation + delta, -2f, 100f);
        public void AdjustDebt(float delta) => NationalDebt = Math.Max(0, NationalDebt + delta);
        
                public void UpdateEconomy(SimulationEngine engine)
        {
            // FAZ 1 (K5): Tek bütçe modeli
            float totalTaxIncome = engine.CalculateTotalRevenue();
            float totalSpending = engine.CalculateTotalSpending();

            // EK-7: Bütçe sabitleri BalanceConfig'ten
var cfg = BalanceConfig.Instance;
float debtRatio = NationalDebt / cfg.DebtReference;
float interestRate = cfg.BaseInterestRate + Math.Min(cfg.MaxInterestRate - cfg.BaseInterestRate, debtRatio * 0.03f);
            float interestPayment = NationalDebt * interestRate;
            
            // Faiz gider olarak bütçeye eklenir
            float effectiveSpending = totalSpending + interestPayment;

            // 2. Bütçe Dengesi
            float budgetBalance = totalTaxIncome - effectiveSpending;
            if (budgetBalance < 0)
            {
                NationalDebt += Math.Abs(budgetBalance);
                // FAZ 1 (K6): Kredi notu açık oranına göre daha hızlı düşer
                float ratingHit = 0.3f + debtRatio * 0.7f;
                CreditRating -= Math.Min(2.0f, ratingHit);
            }
            else
            {
                NationalDebt -= budgetBalance * 0.5f;   // Fazla varsa borcun yarısını öde
                CreditRating += 0.15f;
            }
            CreditRating = Math.Clamp(CreditRating, 0f, 100f);
            NationalDebt = Math.Max(0, NationalDebt);

            // 3. Enflasyon — daha agresif geri besleme
            float inflationPressure = (NationalDebt / 5000f) + (totalSpending * 0.02f) + (interestRate * 2f);
            float inflationTarget = inflationPressure - 1.5f;
            Inflation = Math.Clamp(Inflation + inflationTarget * 0.15f, -2f, 100f);

            if (CreditRating < 30f)
{
    // EK-2: Registry O(1) erişim
    var gdp = engine.Registry.Get(ObjectRegistry.Ids.Gdp);
    if (gdp != null) gdp.ActualValue = Math.Max(0f, gdp.ActualValue - 0.3f);
}

            // 5. Ekonomik Etkiler
            var crime = engine.AllObjects.FirstOrDefault(o => o.Id == "crime_rate");
            if (crime != null && Inflation > 10f)
                crime.ActualValue += (Inflation - 10f) * 0.05f;

            // 6. İflas kontrolü — PDF K6
            if (CreditRating <= 0f)
            {
                engine.Universe.GameOverReason = "İFLAS: Devlet borçlarını ödeyemedi, ülke iflas etti.";
            }
        }
    }
}
