using System;
using System.Collections.Generic;
using System.Linq;

namespace DemocracySim.Engine.Core
{
    /// <summary>
    /// FAZ 3: Çıkar grubu — belirli bir politik gündemi olan örgütlü yapı.
    /// Halk gruplarından farklı olarak, doğrudan siyasi baskı uygular.
    /// </summary>
    [Serializable]
    public class InterestGroup
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public float Ideology { get; set; }        // -100 (sol) .. +100 (sağ)
        public float Power { get; set; } = 50f;     // 0-100: ne kadar etkili
        public float Satisfaction { get; set; } = 50f;
        public List<string> LinkedGroupIds { get; set; } = new List<string>();
        public float LobbyBudget { get; set; } = 100f;

        public InterestGroup(string id, string name, float ideology)
        {
            Id = id; Name = name; Ideology = ideology;
        }

        /// <summary>Her tur çağrılır. Çıkar grubu baskı uygular.</summary>
        public void ProcessTurn(SimulationEngine e)
        {
            // Mevcut politikaların ideolojik uyumu
            float avgAlign = 0f;
            int activeCount = 0;
            foreach (var p in e.AllObjects.OfType<SimPolicy>().Where(p => p.IsActive))
            {
                avgAlign += p.IdeologicalAlignment;
                activeCount++;
            }
            if (activeCount > 0) avgAlign /= activeCount;

            float diff = Math.Abs(avgAlign - Ideology);
            if (diff < 30f) Satisfaction = Math.Clamp(Satisfaction + 1.5f, 0f, 100f);
            else if (diff > 60f) Satisfaction = Math.Clamp(Satisfaction - 2f, 0f, 100f);

            // Memnuniyet düşükse lobi faaliyeti artar
            if (Satisfaction < 30f)
                LobbyBudget += 5f;
            else
                LobbyBudget = Math.Max(0f, LobbyBudget - 2f);
        }

        /// <summary>Lobi faaliyeti: politik sermaye veya meşruiyet üzerinden baskı.</summary>
        public string ApplyLobbyPressure(SimulationEngine e)
        {
            if (LobbyBudget < 20f) return $"{Name}: Lobi bütçesi yetersiz.";

            LobbyBudget -= 20f;
            float impact = Power / 100f * 5f;

            if (Satisfaction < 30f)
            {
                e.Legitimacy.AdjustLegitimacy(-impact);
                return $"{Name} hükümete baskı uyguluyor! Meşruiyet -{impact:F1}.";
            }
            else if (Satisfaction > 70f)
            {
                e.PoliticalCapital += impact;
                return $"{Name} hükümeti destekliyor. +{impact:F1} politik sermaye.";
            }
            return $"{Name} lobi faaliyeti yürütüyor.";
        }
    }
}