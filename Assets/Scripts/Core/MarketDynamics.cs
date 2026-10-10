using System;
using System.Collections.Generic;
using System.Linq;

namespace DemocracySim.Engine.Core
{
    /// <summary>
    /// FAZ 22: Dinamik arz-talep dengesi.
    /// Her sektörün fiyatı, üretimi, talebi zamanla değişir.
    /// Enflasyon ve varlık fiyatları bu döngüden etkilenir.
    /// </summary>
    public class MarketDynamics
    {
        public class MarketSector
        {
            public string Id;
            public float Supply;      // 0-100
            public float Demand;      // 0-100
            public float Price;       // 50 = denge
            public float InflationPressure;
        }

        public List<MarketSector> Sectors = new List<MarketSector>();
        private SimulationEngine _engine;

        public void BindEngine(SimulationEngine engine)
        {
            _engine = engine;
        }

        public void ProcessTurn(SimulationEngine e)
        {
            if (Sectors.Count == 0) Initialize();

            float gdp = e.Registry.GetValue(ObjectRegistry.Ids.Gdp, 50f);
            float unemployment = e.Registry.GetValue(ObjectRegistry.Ids.Unemployment, 10f);

            foreach (var s in Sectors)
            {
                // Arz: GSYİH ile doğru orantılı
                float targetSupply = gdp * 0.8f + 20f;
                s.Supply += (targetSupply - s.Supply) * 0.1f;

                // Talep: halk refahı + işsizlik ters orantılı
                float targetDemand = 50f + (gdp - 50f) * 0.4f - (unemployment - 10f) * 0.5f;
                s.Demand += (targetDemand - s.Demand) * 0.15f;

                // Fiyat = arz-talep oranı
                float ratio = s.Demand / Math.Max(1f, s.Supply);
                float targetPrice = 50f * ratio;
                s.Price += (targetPrice - s.Price) * 0.2f;
                s.Price = Math.Clamp(s.Price, 10f, 200f);

                // Enflasyon baskısı (fiyat 50 üstüne çıkınca)
                s.InflationPressure = Math.Max(0f, (s.Price - 50f) * 0.05f);
            }

            // Toplam enflasyon baskısı ekonomiye yansır
            float totalPressure = Sectors.Sum(s => s.InflationPressure) / Sectors.Count;
            if (totalPressure > 0.5f)
                e.Economy.AdjustInflation(totalPressure * 0.3f);
            else if (totalPressure < -0.5f)
                e.Economy.AdjustInflation(totalPressure * 0.2f);
        }

        private void Initialize()
        {
            Sectors = new List<MarketSector>
            {
                new MarketSector { Id = "food",      Supply = 60, Demand = 60, Price = 50 },
                new MarketSector { Id = "energy",    Supply = 50, Demand = 55, Price = 55 },
                new MarketSector { Id = "housing",   Supply = 40, Demand = 70, Price = 70 },
                new MarketSector { Id = "tech",      Supply = 30, Demand = 60, Price = 65 },
                new MarketSector { Id = "luxury",    Supply = 70, Demand = 40, Price = 40 },
            };
        }

        public string GetReport()
        {
            return string.Join("\n", Sectors.Select(s =>
                $"{s.Id,-10} Arz:{s.Supply:F0,3} Talep:{s.Demand:F0,3} Fiyat:{s.Price:F0,3} Enflasyon:{s.InflationPressure:F1}"));
        }
    }
}