namespace DemocracySim.Engine.Core
{
    public enum RadicalizationTier
    {
        Satisfied,      // Memnun (Satisfaction >= 50)
        Dissatisfied,   // Memnuniyetsiz (30-50)
        Angry,          // Öfkeli (20-30)
        Radical,        // Radikal (10-20)
        Insurgent       // İsyancı (< 10)
    }

    public static class RadicalizationHelper
    {
        public static RadicalizationTier GetTier(float satisfaction)
        {
            if (satisfaction >= 50f) return RadicalizationTier.Satisfied;
            if (satisfaction >= 30f) return RadicalizationTier.Dissatisfied;
            if (satisfaction >= 20f) return RadicalizationTier.Angry;
            if (satisfaction >= 10f) return RadicalizationTier.Radical;
            return RadicalizationTier.Insurgent;
        }

        public static float GetUnrestContribution(RadicalizationTier tier)
        {
            return tier switch
            {
                RadicalizationTier.Satisfied    => 0f,
                RadicalizationTier.Dissatisfied => 0.5f,
                RadicalizationTier.Angry        => 1.0f,
                RadicalizationTier.Radical      => 2.0f,
                RadicalizationTier.Insurgent    => 5.0f,
                _ => 0f
            };
        }

        public static string GetLabel(RadicalizationTier tier)
        {
            return tier switch
            {
                RadicalizationTier.Satisfied    => "Memnun",
                RadicalizationTier.Dissatisfied => "Memnuniyetsiz",
                RadicalizationTier.Angry        => "Öfkeli",
                RadicalizationTier.Radical      => "RADİKAL",
                RadicalizationTier.Insurgent    => "İSYANCI",
                _ => "?"
            };
        }
    }
}