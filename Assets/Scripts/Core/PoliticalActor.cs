using System;
using System.Collections.Generic;
using System.Linq;

namespace DemocracySim.Engine.Core
{
    public enum ActorRole { Minister, OppositionLeader, MP, Activist, Oligarch }
    public enum PoliticalFaction { Technocrat, Ideologue, Loyalist }
    public enum ActorTrait { Ambitious, Populist, Cautious, Corrupt, LoyalistsHeart }

    public class PoliticalActor
    {
                public string PartyId { get; set; } = "";
        public string Id { get; set; }
        public string Name { get; set; }
        public ActorRole Role { get; set; }
        public PoliticalFaction Faction { get; set; }
        public List<ActorTrait> Traits { get; set; } = new List<ActorTrait>();
        public float Ideology { get; set; } 
        public float Ambition { get; set; } 
                public string Portfolio { get; set; } = "";
        public float Loyalty { get; set; }  
        public float Competence { get; set; } 
        public float Influence { get; set; }
        public float Satisfaction { get; set; } = 50f;
        public float PersuasionBonus { get; set; } = 0f;

                public PoliticalActor(string id, string name, ActorRole role, PoliticalFaction faction = PoliticalFaction.Loyalist, string portfolio = "") 
        { 
            Id = id; Name = name; Role = role; Faction = faction; 
            Portfolio = portfolio;
        }

        public (string advice, float confidence) GetAdvice(SimPolicy policy)
        {
            float confidence = Competence / 100f;
            float alignmentDiff = Math.Abs(policy.IdeologicalAlignment - this.Ideology);
            
            if (Traits.Contains(ActorTrait.Cautious))
            {
                if (alignmentDiff > 30f) return ("Bu yasa çok riskli görünüyor, halk arasında kutuplaşmaya yol açabilir.", confidence);
            }
            
            if (Traits.Contains(ActorTrait.Populist))
            {
                if (policy.GroupImpacts.Values.Sum() > 0) return ("Halk bu yasayı çok sevecek, popülaritemiz artar!", confidence);
            }

            if (alignmentDiff < 20f) return ("Bu yasa bizim vizyonumuzla tam örtüşüyor, hemen geçirmeliyiz.", confidence);
            else if (alignmentDiff < 50f) return ("Kabul edilebilir bir yasa, ancak bazı düzenlemeler gerekebilir.", confidence);
            else return ("Bu yasa bizim ilkelerimize aykırı, ciddi tepki çekebiliriz.", confidence);
        }

        // BURASI ÇOK KRİTİK: = 0f ekleyerek parametreyi opsiyonel yapıyoruz.
        public void EvaluateWorld(List<SimObject> allObjects, float playerGlobalAlignment = 0f)
        {
            float turnSat = 0;
            int tracked = 0;

            foreach (var obj in allObjects) {
                if (obj is SimPolicy pol && pol.IsActive) {
                    float diff = Math.Abs(pol.IdeologicalAlignment - this.Ideology);
                    turnSat += (diff < 30f) ? 2f : (diff > 70f ? -2f : 0f);
                    tracked++;
                }
            }

            float leaderDiff = Math.Abs(playerGlobalAlignment - this.Ideology);
            turnSat += (leaderDiff < 20f) ? 1f : (leaderDiff > 60f ? -1f : 0f);

            Satisfaction = Math.Clamp(Satisfaction + turnSat, 0f, 100f);
            float loyaltyGain = (Satisfaction > 60f) ? 0.5f : (Satisfaction < 40f ? -1.0f : 0f);
            float ambitionPenalty = (Ambition / 100f) * 0.3f; 
            
            Loyalty = Math.Clamp(Loyalty + loyaltyGain - ambitionPenalty, 0f, 100f);
            PersuasionBonus *= 0.8f;
        }
    }
}
