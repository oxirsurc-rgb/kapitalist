using System.Linq;

namespace DemocracySim.Engine.Core
{
    /// <summary>
    /// EK-23: Parti kurma mekanikleri.
    /// Temsil krizi tetiklendiğinde oyuncu kendi partisini kurabilir.
    /// </summary>
    public static class PartyFounder
    {
        public const float FoundCost = 60f;
        public const float FundCost = 40f;
        public const float ActorTransferCost = 15f;

        /// <summary>
        /// Temsil krizi var mı? Parti kurma sebebi.
        /// </summary>
        public static string RepresentationCrisis(SimulationEngine e)
        {
            if (e.CurrentRole != SimulationEngine.PlayerRole.Opposition)
                return "Sadece muhalefetteyken parti kurabilirsin.";

            if (PlayerProfile.IsCustomParty)
                return "Zaten kendi partinizi kurdunuz.";

            var reasons = new System.Collections.Generic.List<string>();

            // 1) Oyuncunun ideolojisi tüm partilerden uzak mı?
            float playerIdeo = e.PlayerGlobalAlignment;
            var closest = e.PartyManager.Parties
                .Where(p => p.Id != "player")
                .OrderBy(p => System.Math.Abs(p.Ideology - playerIdeo))
                .FirstOrDefault();
            if (closest != null && System.Math.Abs(closest.Ideology - playerIdeo) > 60f)
                reasons.Add($"İdeolojinizi temsil eden parti yok (en yakın: {closest.Name}, {System.Math.Abs(closest.Ideology - playerIdeo):F0} puan uzak)");

            // 2) Halk memnuniyeti düşük mü?
            if (e.Demographics.Count > 0)
            {
                float avg = e.Demographics.Average(g => g.Satisfaction);
                if (avg < 30f)
                    reasons.Add($"Halk çaresiz (ortalama memnuniyet: %{avg:F0}) — yeni bir ses arıyor");
            }

            // 3) 3+ seçim kaybı
            if (e.ConsecutiveElectionLosses >= 3)
                reasons.Add($"{e.ConsecutiveElectionLosses} seçim üst üste kaybedildi — bu partide yükselmen imkansız");

            // 4) Mevcut partilerin hepsi aynı yönde mi?
            bool allSameSide = e.PartyManager.Parties
                .Where(p => p.Seats > 0 && p.Id != "player")
                .All(p => p.Ideology > 0 || p.Ideology < 0);
            if (allSameSide)
                reasons.Add("Meclisteki tüm partiler aynı yönde — gerçek muhalefet yok");

            if (reasons.Count < 2)
                return null;   // En az 2 sebep gerekli

            return string.Join("\n• ", reasons);
        }

        /// <summary>
        /// Parti kurulabilir mi? (maliyet + sebep kontrolü)
        /// </summary>
        public static string CanFound(SimulationEngine e)
        {
            var reason = RepresentationCrisis(e);
            if (reason == null && !PlayerProfile.IsCustomParty)
                return "Şu an parti kurmak için yeterli sebebiniz yok (temsil krizi yok).";

            if (PlayerProfile.IsCustomParty)
                return "Zaten kendi partinizi kurdunuz.";

            if (e.PoliticalCapital < FoundCost)
                return $"Yeterli siyasi sermaye yok ({FoundCost:F0} gerekir).";

            if (e.Party == null || e.Party.Fund < FundCost)
                return $"Yeterli parti fonu yok ({FundCost:F0} gerekir).";

            return null;   // Kurabilir
        }

        /// <summary>
        /// Partiyi kur.
        /// </summary>
        public static string Found(SimulationEngine e, string partyName, string slogan, float ideology)
        {
            string blocker = CanFound(e);
            if (blocker != null) return blocker;

            e.PoliticalCapital -= FoundCost;
            e.Party.Fund -= FundCost;

            // Parti bilgilerini güncelle
            PlayerProfile.PartyName = partyName;
            PlayerProfile.PartySlogan = slogan;
            PlayerProfile.IsCustomParty = true;

            // Mevcut "player" partisini güncelle
            var playerParty = e.PartyManager.Parties.FirstOrDefault(p => p.Id == "player");
            if (playerParty != null)
            {
                playerParty.Name = partyName;
                playerParty.Ideology = System.Math.Clamp(ideology, -100f, 100f);
            }

            // Oy bonusu (yeni parti heyecanı)
            e.Elections.OppositionMomentum = System.Math.Min(
                e.Elections.OppositionMomentum + 15f,
                ElectionManager.MaxOppositionMomentum);

            return $"✅ {partyName} kuruldu! {slogan}\n" +
                   $"-{FoundCost:F0} sermaye, -{FundCost:F0} fon. Muhalefet momentumu +15.";
        }

        /// <summary>
        /// İdeolojik yakın AI aktörü partinize transfer et.
        /// </summary>
        public static string TransferActor(SimulationEngine e, string actorId)
        {
            var actor = e.Actors.FirstOrDefault(a => a.Id == actorId);
            if (actor == null) return "Aktör bulunamadı.";

            if (actor.PartyId == "player") return $"{actor.Name} zaten partinizde.";

            if (e.PoliticalCapital < ActorTransferCost)
                return $"Yeterli sermaye yok ({ActorTransferCost:F0} gerekir).";

            // İdeolojik yakınlık kontrolü
            var playerParty = e.PartyManager.Parties.FirstOrDefault(p => p.Id == "player");
            float playerIdeo = playerParty?.Ideology ?? 0f;
            float distance = System.Math.Abs(actor.Ideology - playerIdeo);

            if (distance > 40f)
                return $"{actor.Name} sizinle ideolojik olarak uyumsuz ({distance:F0} puan uzak).";

            e.PoliticalCapital -= ActorTransferCost;
            actor.PartyId = "player";

            return $"{actor.Name} partinize katıldı! (-{ActorTransferCost:F0} sermaye)";
        }
    }
}