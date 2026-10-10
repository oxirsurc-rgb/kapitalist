using System.Linq;

namespace DemocracySim.Engine.Core
{
    /// <summary>
    /// EK-24: Adaylık sistemi.
    /// Seçim kaybı sonrası oyuncu parti içi fraksiyonlardan adaylık onayı almalı.
    /// </summary>
    public static class CandidateManager
    {
        public const int CrisisWarningTurns = 4;     // Seçime 4 tur kala kriz başlar
        public const float MinSupportForCandidacy = 50f;  // %50 ortalama fraksiyon desteği
        public const float ConcessionCost = 25f;     // Fraksiyona taviz maliyeti
        public const float ConcessionSupportGain = 15f; // Taviz başına destek artışı

        /// <summary>
        /// Oyuncu adaylık krizi içinde mi?
        /// Muhalefette + en az 1 seçim kaybı + seçime 4 tur veya daha az.
        /// </summary>
        public static bool InCandidateCrisis(SimulationEngine e)
        {
            if (e == null) return false;
            if (e.CurrentRole == SimulationEngine.PlayerRole.Governing) return false;
            if (e.ConsecutiveElectionLosses < 1) return false;
            return e.TurnUntilElection <= CrisisWarningTurns;
        }

        /// <summary>Oyuncunun adaylık desteği (fraksiyonların ortalaması)</summary>
        public static float GetCandidateSupport(SimulationEngine e)
        {
            if (e?.Factions?.Factions == null || e.Factions.Factions.Count == 0) return 50f;

            float total = 0f;
            foreach (var f in e.Factions.Factions) total += f.Support;
            return total / e.Factions.Factions.Count;
        }

        /// <summary>Oyuncu aday olabilir mi? (destek yeterliyse)</summary>
        public static bool CanRun(SimulationEngine e)
        {
            return GetCandidateSupport(e) >= MinSupportForCandidacy;
        }

        /// <summary>Fraksiyona taviz ver — adaylık desteğini artır</summary>
        public static string ConcedeToFaction(SimulationEngine e, string factionId)
        {
            var faction = e.Factions.Factions.FirstOrDefault(f => f.Id == factionId);
            if (faction == null) return "Fraksiyon bulunamadı.";

            if (e.PoliticalCapital < ConcessionCost)
                return $"Yeterli siyasi sermaye yok ({ConcessionCost:F0} gerekir).";

            e.PoliticalCapital -= ConcessionCost;
            e.Factions.AdjustSupport(factionId, ConcessionSupportGain);

            return $"✅ {faction.Name} fraksiyonuna taviz verildi. " +
                   $"Destek +{ConcessionSupportGain:F0} (yeni: %{faction.Support:F0}). " +
                   $"Kalan sermaye: {e.PoliticalCapital:F0}";
        }

        /// <summary>Seçim günü adaylık kontrolü.</summary>
        /// <returns>null = aday olabilir. Mesaj = aday olamadı (oyun biter).</returns>
        public static string CheckCandidacyAtElection(SimulationEngine e)
        {
            if (!InCandidateCrisis(e)) return null;   // Kriz yoksa serbest

            float support = GetCandidateSupport(e);
            if (support < MinSupportForCandidacy)
            {
                // Aday olamadı → oyun biter
                return $"PARTİ İÇİ DARBE: {e.ConsecutiveElectionLosses} seçim yenilgisinden sonra " +
                       $"fraksiyonlar sizi aday göstermedi!\n\n" +
                       $"Adaylık desteği: %{support:F0} (gereken: %{MinSupportForCandidacy:F0})\n" +
                       $"Parti içi rakipleriniz yönetimi devraldı.";
            }
            return null;   // Aday olabilir
        }

        /// <summary>Her tur çağrılır — durum raporu mesajı üretir.</summary>
        public static string GetStatusMessage(SimulationEngine e)
        {
            if (!InCandidateCrisis(e)) return null;

            float support = GetCandidateSupport(e);
string emoji = support >= MinSupportForCandidacy ? "[OK]" : "[!!]";
            return $"{emoji} ADAYLIK: Fraksiyon desteği %{support:F0} " +
                   $"(seçime {e.TurnUntilElection} tur, gereken %{MinSupportForCandidacy:F0})";
        }
    }
}