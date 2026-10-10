using System;

namespace DemocracySim.Engine.Core
{
    public static class InformationLayer
    {
        // Eskiden her çağrıda new Random() üretiliyordu; aynı milisaniyede aynı "rastgele" değer çıkıyordu.

        public static float FilterTruth(float actualValue, PoliticalActor observer, SimObject obj)
        {
                if (observer == null || obj == null) return actualValue;
            float bias = (observer.Ideology > 50 && obj.Id == "inequality") ? -10f : 0f;
            return actualValue + bias + (float)(SimRng.NextDouble() * 2 - 1);
        }
    }
}
