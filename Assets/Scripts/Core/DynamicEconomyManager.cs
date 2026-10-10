using UnityEngine;
using System.Collections.Generic;
using System;

namespace DemocracySim.Engine.Core
{
    public enum SectorType { Agriculture, Industry, Energy, Tech, Finance }

    [Serializable]
    public class SectorData
    {
        public SectorType type;
        public float productionCapacity;
        public float currentOutput;
        public float demand;
        public float priceMultiplier = 1.0f;
    }

    public class DynamicEconomyManager : MonoBehaviour
    {
        public static DynamicEconomyManager Instance { get; private set; }
        public List<SectorData> sectors = new List<SectorData>();

        void Awake()
        {
            Instance = this;
            InitializeSectors();
        }

        void InitializeSectors()
        {
            foreach (SectorType type in Enum.GetValues(typeof(SectorType)))
            {
                sectors.Add(new SectorData { type = type, productionCapacity = 100, demand = 100 });
            }
        }

        public void UpdateEconomy()
        {
            foreach (var sector in sectors)
            {
                // Dinamik Fiyatlandırma: Talep > Arz ise fiyat artar
                float ratio = sector.demand / Math.Max(sector.currentOutput, 1f);
                sector.priceMultiplier = Mathf.Clamp(ratio, 0.5f, 2.5f);

                // Üretim kapasitesini güncelle (Siyasi kararlara göre)
                sector.currentOutput = sector.productionCapacity * (1f + UnityEngine.Random.Range(-0.05f, 0.05f));
            }
            
            EventBus.Publish(new EconomyUpdatedEvent());
        }
    }

    public struct EconomyUpdatedEvent { }
}
