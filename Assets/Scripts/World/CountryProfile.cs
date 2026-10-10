using System;

namespace DemocracySim.Engine.World
{
    [Serializable] // Unity'nin bu veriyi tanıması için şart
    public class CountryProfile
    {
        public string id;
        public string name;
        public string continent;
        public float startingLegitimacy;
        public float startingCapital;
        public float globalAlignment;
        
        // Başlangıçtaki nüfus/etki ağırlıkları
        public float workerInfluence;
        public float capitalistInfluence;
        public float intellectualInfluence;
        public float conservativeInfluence;
    }
}
