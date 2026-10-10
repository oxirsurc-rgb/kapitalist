using System;
using System.Collections.Generic;
using System.Linq;

namespace DemocracySim.Engine.Core
{
    [Serializable]
    public class EconomicSector
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public float GdpShare { get; set; }
        public float Employment { get; set; }
        public float Productivity { get; set; }
        public float TaxBurden { get; set; }

        public EconomicSector(string id, string name, float gdpShare, float employment)
        {
            Id = id; Name = name; GdpShare = gdpShare; Employment = employment;
            Productivity = 50f;
        }
    }

    public class SectorManager
    {
        public List<EconomicSector> Sectors { get; set; } = new List<EconomicSector>();

        public SectorManager()
        {
            Sectors.Add(new EconomicSector("agriculture", "Tarım", 15f, 20f));
            Sectors.Add(new EconomicSector("industry", "Sanayi", 30f, 25f));
            Sectors.Add(new EconomicSector("services", "Hizmet", 40f, 35f));
            Sectors.Add(new EconomicSector("technology", "Teknoloji", 15f, 20f));
        }

        public void ProcessTurn(SimulationEngine e)
{
    float totalProductivity = Sectors.Sum(s => s.Productivity);
    float totalShare = Sectors.Sum(s => s.GdpShare);

    foreach (var s in Sectors)
    {
        // Vergi yükü verimliliği etkiler
        float taxEffect = -(s.TaxBurden - 50f) * 0.05f;
        s.Productivity = Math.Clamp(s.Productivity + taxEffect, 0f, 100f);

        // FAZ 3: Sektör payı, verimlilik ve GSYİH ile gerçekten bağlantılı
        float gdp = e.AllObjects.FirstOrDefault(o => o.Id == "gdp")?.ActualValue ?? 50f;
        float targetShare = (s.Productivity / 100f) * gdp * 0.4f;
        s.GdpShare += (targetShare - s.GdpShare) * 0.02f;
        s.GdpShare = Math.Clamp(s.GdpShare, 5f, 60f);

        // İstihdam, sektör payına göre güncellenir
        s.Employment = Math.Clamp(s.GdpShare * 0.8f + s.Productivity * 0.2f, 5f, 60f);
    }

    // Normalize
    float total = Sectors.Sum(s => s.GdpShare);
    if (total > 0)
        foreach (var s in Sectors) s.GdpShare = (s.GdpShare / total) * 100f;

    // FAZ 3: Sektör verimliliği GSYİH'ya katkı sağlar
    float avgProductivity = Sectors.Average(s => s.Productivity);
    var gdpObj = e.AllObjects.FirstOrDefault(o => o.Id == "gdp");
    if (gdpObj != null)
    {
        float gdpBonus = (avgProductivity - 50f) * 0.02f;
        gdpObj.ActualValue = Math.Clamp(gdpObj.ActualValue + gdpBonus, gdpObj.MinValue, gdpObj.MaxValue);
    }
}

        public string InvestInSector(string sectorId, float amount, SimulationEngine e)
        {
            var s = Sectors.FirstOrDefault(x => x.Id == sectorId);
            if (s == null) return "Sektör bulunamadı.";
            if (e.PoliticalCapital < amount) return "Yeterli siyasi sermaye yok.";
            e.PoliticalCapital -= amount;
            s.Productivity = Math.Clamp(s.Productivity + amount * 0.5f, 0f, 100f);
            return $"{s.Name} sektörüne yatırım yapıldı (verimlilik: {s.Productivity:F0}).";
        }
    }
}