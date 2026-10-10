using System;
using System.Collections.Generic;
using System.Linq;
using DemocracySim.Engine.Core;

namespace DemocracySim.Engine.Legislative
{
    public class VoteResult
    {
        public bool IsPassed { get; set; }
        public int YesVotes { get; set; }
        public int NoVotes { get; set; }
        public int AbstainVotes { get; set; }
        public string Summary { get; set; }
    }

    public class LegislativeChamber
    {
        public const float QuorumThreshold = 0.5f;
        private const float InfluencePerSeat = 5f;
        private const float YesThreshold = 0.15f;
        private const float NoThreshold = -0.15f;

        /// <summary>FAZ 2: Parti disiplinli oylama.</summary>
        public VoteResult ConductVote(SimPolicy policy, List<PoliticalActor> actors,
                                      float oppositionPressure, PartyManager partyManager,
                                      SimulationEngine engine)
        {
            if (actors == null || actors.Count == 0)
            {
                return new VoteResult
                {
                    IsPassed = false,
                    Summary = "Meclis boş, oylama yapılamadı."
                };
            }

            int yesSeats = 0, noSeats = 0, abstainSeats = 0, totalSeats = 0;
            float pressureFactor = -(oppositionPressure / 100f) * 0.5f;

            // Her parti için önceden parti kararını hesapla
            var partyDirections = new Dictionary<string, int>();
            if (partyManager != null)
            {
                foreach (var party in partyManager.Parties)
                {
                    partyDirections[party.Id] = partyManager.GetPartyVoteDirection(party, policy, engine);
                }
            }

            foreach (var actor in actors)
            {
                // Sadece milletvekilleri oy verir (bakanlar dahil — kabine üyeleri)
                if (actor.Role == ActorRole.Activist || actor.Role == ActorRole.Oligarch) continue;

                int seats = Math.Max(1, (int)(actor.Influence / InfluencePerSeat));
                totalSeats += seats;

                // 1. Kişisel skor (mevcut formül)
                float ideoDistance = Math.Abs(policy.IdeologicalAlignment - actor.Ideology);
                float ideoMatch = 1f - (ideoDistance / 200f);
                ideoMatch = (ideoMatch - 0.5f) * 0.8f;

                float loyaltyFactor = ((actor.Loyalty - 50f) / 100f) * 0.6f;
                float satisfactionFactor = ((actor.Satisfaction - 50f) / 100f) * 0.3f;

                float publicFactor = 0f;
                if (policy.GroupImpacts != null && policy.GroupImpacts.Count > 0)
                {
                    float avgImpact = policy.GroupImpacts.Values.Average();
                    publicFactor = Math.Clamp(avgImpact * 0.02f, -0.2f, 0.2f);
                }

                float personalScore = ideoMatch + loyaltyFactor + satisfactionFactor + publicFactor + pressureFactor;

                // 2. FAZ 2: Parti disiplini — parti kararına uyma
                float finalScore = personalScore;
                if (!string.IsNullOrEmpty(actor.PartyId) &&
                    partyDirections.TryGetValue(actor.PartyId, out int partyDirection))
                {
                    var party = partyManager.Parties.FirstOrDefault(p => p.Id == actor.PartyId);
                    float discipline = party?.Discipline ?? 0.85f;

                    // Parti disiplini: %85 parti kararı, %15 kişisel
                    float partyScore = partyDirection * 0.5f;
                    finalScore = partyScore * discipline + personalScore * (1f - discipline);
                }

                // 3. Karar
                if (finalScore > YesThreshold) yesSeats += seats;
                else if (finalScore < NoThreshold) noSeats += seats;
                else abstainSeats += seats;
            }
            // FAZ 3.5: Nisap kontrolü
int totalPossibleSeats = actors
    .Where(a => a.Role != ActorRole.Activist && a.Role != ActorRole.Oligarch)
    .Sum(a => Math.Max(1, (int)(a.Influence / InfluencePerSeat)));

int castVotes = yesSeats + noSeats + abstainSeats;
bool quorumMet = castVotes >= totalPossibleSeats * QuorumThreshold;

if (!quorumMet)
{
    return new VoteResult
    {
        IsPassed = false,
        YesVotes = yesSeats,
        NoVotes = noSeats,
        AbstainVotes = abstainSeats,
        Summary = $"NİSAP SAĞLANAMADI: {castVotes}/{totalPossibleSeats} katılım. Oylama geçersiz."
    };
}

            bool passed = castVotes > 0 && yesSeats > (castVotes / 2f);

            string abstainStr = abstainSeats > 0 ? $", {abstainSeats} Çekimser" : "";
            string pressureStr = oppositionPressure > 10f ? $" | Muhalefet: %{oppositionPressure:F0}" : "";
            string resultStr = passed ? "KABUL" : "RED";

            return new VoteResult
            {
                IsPassed = passed,
                YesVotes = yesSeats,
                NoVotes = noSeats,
                AbstainVotes = abstainSeats,
                Summary = $"{yesSeats} Evet, {noSeats} Hayır{abstainStr} ({totalSeats} sandalye){pressureStr} → {resultStr}"
            };
        }
    }
}