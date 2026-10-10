using System;

namespace DemocracySim.Engine.Core
{
    public enum AIPersonalityType { Populist, Ideologue, Technocrat, Autocrat }

    public class AIPersonality
    {
        public AIPersonalityType PersonalityType => Type;
        public AIPersonalityType Type { get; set; }
        public float Aggression { get; set; }
        public float LoyaltyToIdeology { get; set; }
        public float PopulismTendency { get; set; }

        // YENİ: AI'nın nelere önem verdiği (0.0 - 1.0 arası ağırlıklar)
        public float WeightLegitimacy { get; set; } // Meşruiyet ne kadar önemli?
        public float WeightEconomy { get; set; }     // Ekonomi ne kadar önemli?
        public float WeightStability { get; set; }   // Huzursuzluk/Sokaklar ne kadar önemli?

        public AIPersonality(AIPersonalityType type)
        {
            Type = type;
            switch (type)
            {
                case AIPersonalityType.Populist:
                    Aggression = 0.6f; LoyaltyToIdeology = 0.3f; PopulismTendency = 0.9f;
                    WeightLegitimacy = 0.8f; WeightEconomy = 0.2f; WeightStability = 0.5f;
                    break;
                case AIPersonalityType.Ideologue:
                    Aggression = 0.5f; LoyaltyToIdeology = 0.9f; PopulismTendency = 0.4f;
                    WeightLegitimacy = 0.4f; WeightEconomy = 0.4f; WeightStability = 0.4f;
                    break;
                case AIPersonalityType.Technocrat:
                    Aggression = 0.3f; LoyaltyToIdeology = 0.5f; PopulismTendency = 0.2f;
                    WeightLegitimacy = 0.3f; WeightEconomy = 0.9f; WeightStability = 0.6f;
                    break;
                case AIPersonalityType.Autocrat:
                    Aggression = 0.8f; LoyaltyToIdeology = 0.7f; PopulismTendency = 0.1f;
                    WeightLegitimacy = 0.2f; WeightEconomy = 0.5f; WeightStability = 0.9f;
                    break;
            }
        }
    }
}
