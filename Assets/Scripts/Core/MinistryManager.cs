using System;
using System.Collections.Generic;
using System.Linq;

namespace DemocracySim.Engine.Core
{
    /// <summary>
    /// FAZ 2.5: Bakanlık yönetimi — yeni bakanlık açma/kapatma.
    /// Araştırma: PT: Reborn (anayasa değişikliği ile yeni bakanlık).
    /// </summary>
    public class MinistryManager
    {
        public const float NewMinistryCost = 50f;        // Politik Sermaye
        public const float CloseMinistryCost = 30f;      // Kapatma maliyeti
        public const int MaxMinistries = 10;             // Üst sınır

        public List<string> ActiveMinistries { get; set; } = new List<string>
        {
            "Ekonomi", "Sağlık", "Savunma", "Eğitim", "Adalet"
        };

        // Açılabilecek bakanlıklar ve gereksinimleri
        public static readonly Dictionary<string, MinistryRequirement> AvailableMinistries = new()
        {
            ["Dışişleri"] = new MinistryRequirement(30f, 3),
            ["İçişleri"] = new MinistryRequirement(30f, 3),
            ["Çevre"] = new MinistryRequirement(40f, 5),
            ["Teknoloji"] = new MinistryRequirement(60f, 8),
            ["Uzay"] = new MinistryRequirement(80f, 10),
            ["Kültür"] = new MinistryRequirement(35f, 4),
            ["Enerji"] = new MinistryRequirement(50f, 6),
            ["Tarım"] = new MinistryRequirement(25f, 2),
            ["Ulaştırma"] = new MinistryRequirement(40f, 5),
        };

        /// <summary>Oyuncu yeni bir bakanlık açabilir mi?</summary>
        public string CanOpenMinistry(string ministryName, SimulationEngine e)
        {
            if (!AvailableMinistries.ContainsKey(ministryName))
                return "Geçersiz bakanlık.";
            if (ActiveMinistries.Contains(ministryName))
                return "Bu bakanlık zaten açık.";
            if (ActiveMinistries.Count >= MaxMinistries)
                return $"En fazla {MaxMinistries} bakanlık açabilirsiniz.";
            if (e.CurrentRole != SimulationEngine.PlayerRole.Governing)
                return "Sadece iktidardayken bakanlık açabilirsiniz.";

            var req = AvailableMinistries[ministryName];
            if (e.PoliticalCapital < req.CapitalCost)
                return $"Yeterli sermaye yok ({req.CapitalCost:F0} gerekir).";
            if (e.CurrentTurn < req.RequiredTurn)
                return $"Bu bakanlık için en az {req.RequiredTurn} tur gerekir (şu an: {e.CurrentTurn}).";

            return null;   // Açılabilir
        }

        /// <summary>Yeni bakanlık aç.</summary>
        public string OpenMinistry(string ministryName, SimulationEngine e)
        {
            string blocker = CanOpenMinistry(ministryName, e);
            if (blocker != null) return blocker;

            var req = AvailableMinistries[ministryName];
            e.PoliticalCapital -= req.CapitalCost;
            ActiveMinistries.Add(ministryName);

            return $"{ministryName} Bakanlığı kuruldu! (-{req.CapitalCost:F0} sermaye)";
        }

        /// <summary>Bakanlığı kapat (30 sermaye).</summary>
        public string CloseMinistry(string ministryName, SimulationEngine e)
        {
            if (!ActiveMinistries.Contains(ministryName))
                return "Bu bakanlık zaten kapalı.";
            if (ActiveMinistries.Count <= 3)
                return "En az 3 bakanlık açık kalmalı.";
            if (e.PoliticalCapital < CloseMinistryCost)
                return $"Yeterli sermaye yok ({CloseMinistryCost:F0} gerekir).";

            e.PoliticalCapital -= CloseMinistryCost;
            ActiveMinistries.Remove(ministryName);

            // Bu bakanlıktaki bakanı görevden al
            var minister = e.Actors.FirstOrDefault(a => a.Portfolio == ministryName);
            if (minister != null) e.Actors.Remove(minister);

            e.Legitimacy.AdjustLegitimacy(-3f);
            return $"{ministryName} Bakanlığı kapatıldı. (-{CloseMinistryCost:F0} sermaye, -3 meşruiyet)";
        }
    }

    public class MinistryRequirement
    {
        public float CapitalCost { get; }
        public int RequiredTurn { get; }

        public MinistryRequirement(float capitalCost, int requiredTurn)
        {
            CapitalCost = capitalCost;
            RequiredTurn = requiredTurn;
        }
    }
}