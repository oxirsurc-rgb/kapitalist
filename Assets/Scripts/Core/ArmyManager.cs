using System;
using System.Linq;
using DemocracySim.Engine.Core;

namespace DemocracySim.Engine.Core
{
    public class ArmyManager
    {
        public float ArmySatisfaction { get; private set; } = 60f;
        public float LoyaltyToLeader { get; private set; } = 50f;

        // FAZ 1: MilitaryStrength artık SimStatistic'ten geliyor
        private SimulationEngine _engine;

        public float MilitaryStrength
        {
            get
            {
                if (_engine == null) return 50f;
                var stat = _engine.AllObjects.FirstOrDefault(o => o.Id == "military_strength");
                return stat?.ActualValue ?? 50f;
            }
        }

        public void BindEngine(SimulationEngine engine)
        {
            _engine = engine;
        }

        public float CoupRiskPercent { get; private set; } = 0f;
        public string CoupRiskLabel => CoupRiskPercent < 20f ? "Dusuk" : (CoupRiskPercent < 50f ? "Orta" : "Yuksek");

        public void LoadState(float satisfaction, float militaryStrength, float loyaltyToLeader)
        {
            ArmySatisfaction = satisfaction;
            LoyaltyToLeader = loyaltyToLeader;
            // FAZ 1: MilitaryStrength artık SimStatistic — SaveLoadManager Objects üzerinden yükler
        }

        public void UpdateArmy(SimulationEngine engine, float militaryBudget)
        {
            // 1. Bütçe Etkisi
            float budgetImpact = (militaryBudget - 50f) * 0.1f;
            ArmySatisfaction = Math.Clamp(ArmySatisfaction + budgetImpact, 0f, 100f);

            // 2. Meşruiyet Etkisi
            float legitImpact = (engine.Legitimacy.CurrentLegitimacy - 50f) * 0.05f;
            LoyaltyToLeader = Math.Clamp(LoyaltyToLeader + legitImpact, 0f, 100f);

            // 3. FAZ 1: Askeri güç — SimStatistic üzerinden
            var stat = engine.AllObjects.FirstOrDefault(o => o.Id == "military_strength");
            if (stat != null)
            {
                if (ArmySatisfaction > 70f)
                    stat.ActualValue = Math.Clamp(stat.ActualValue + 0.1f, 0f, 100f);
                else if (ArmySatisfaction < 30f)
                    stat.ActualValue = Math.Clamp(stat.ActualValue - 0.1f, 0f, 100f);
            }

            // 4. Darbe riski
            CoupRiskPercent = ArmySatisfaction < 30f
                ? Math.Clamp((100f - engine.Legitimacy.CurrentLegitimacy) * 0.5f + (100f - ArmySatisfaction) * 0.5f, 0f, 100f)
                : Math.Clamp((100f - engine.Legitimacy.CurrentLegitimacy) * 0.15f, 0f, 100f);
        }

        public bool CheckForCoup(SimulationEngine engine)
        {
            if (ArmySatisfaction < 30f)
            {
                float coupChance = (100f - engine.Legitimacy.CurrentLegitimacy) * 0.5f + (100f - ArmySatisfaction) * 0.5f;
                if (SimRng.NextDouble() * 100 < coupChance)
                {
                    return true;
                }
            }
            return false;
        }
    }
}