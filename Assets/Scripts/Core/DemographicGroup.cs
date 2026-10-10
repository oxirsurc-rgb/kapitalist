using System;

namespace DemocracySim.Engine.Core
{
    public class DemographicGroup
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public float Satisfaction { get; set; } = 50f; // %0 - %100
        public float Influence { get; set; } // Bu grubun meşruiyet üzerindeki ağırlığı (0.0 - 1.0)
        
        public DemographicGroup(string id, string name, float influence)
        {
            Id = id;
            Name = name;
            Influence = influence;
        }

        public void AdjustSatisfaction(float delta)
        {
            Satisfaction = Math.Clamp(Satisfaction + delta, 0f, 100f);
        }
    }
}
