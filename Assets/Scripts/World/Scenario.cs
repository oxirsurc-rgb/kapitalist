using System;
using System.Collections.Generic;
using UnityEngine;

namespace DemocracySim.Engine.World
{
    /// <summary>
    /// FAZ 4: Oyun senaryosu — başlangıç koşullarını tanımlar.
    /// </summary>
    [Serializable]
    public class Scenario
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Difficulty { get; set; }   // "Kolay", "Normal", "Zor", "Çok Zor"

        // Başlangıç değerleri
        public float Legitimacy { get; set; } = 70f;
        public float PoliticalCapital { get; set; } = 100f;
        public float Unrest { get; set; } = 10f;

        // Objelere uygulanacak başlangıç değerleri (id → değer)
        public Dictionary<string, float> ObjectOverrides { get; set; } = new Dictionary<string, float>();

        // Özel notlar (oyuncuya gösterilir)
        public string SpecialNote { get; set; } = "";

        /// <summary>Önceden tanımlı 4 senaryo.</summary>
        public static List<Scenario> GetPresets()
        {
            return new List<Scenario>
            {
                new Scenario
                {
                    Id = "classic",
                    Name = "Klasik",
                    Description = "Dengeli başlangıç. Standart zorluk.",
                    Difficulty = "Normal",
                    Legitimacy = 70f,
                    PoliticalCapital = 100f,
                    Unrest = 10f
                },
                new Scenario
                {
                    Id = "crisis",
                    Name = "Ekonomik Kriz",
                    Description = "Ülke derin bir ekonomik krizin içinde. Hazine boş, halk öfkeli.",
                    Difficulty = "Zor",
                    Legitimacy = 45f,
                    PoliticalCapital = 60f,
                    Unrest = 35f,
                    SpecialNote = "Başlangıçta borç yüksek, enflasyon %25. Kemer sıkma politikaları şart.",
                    ObjectOverrides = new Dictionary<string, float>
                    {
                        { "gdp", 30f },
                        { "unemployment", 30f },
                        { "poverty_rate", 60f },
                        { "inequality", 70f },
                        { "happiness", 30f }
                    }
                },
                new Scenario
                {
                    Id = "coup",
                    Name = "Askeri Cunta Tehdidi",
                    Description = "Ordu darbe hazırlığında. Sivil otorite zayıf.",
                    Difficulty = "Çok Zor",
                    Legitimacy = 40f,
                    PoliticalCapital = 50f,
                    Unrest = 25f,
                    SpecialNote = "Ordu memnuniyeti 20. Darbe riski %60. Askeri bütçeyi artır ya da orduyu tasfiye et.",
                    ObjectOverrides = new Dictionary<string, float>
                    {
                        { "military_strength", 80f },
                        { "crime_rate", 55f },
                        { "police_funding", 30f }
                    }
                },
                new Scenario
                {
                    Id = "new_democracy",
                    Name = "Yeni Demokrasi",
                    Description = "Genç bir demokrasi. Halk iyimser, hazine dolu.",
                    Difficulty = "Kolay",
                    Legitimacy = 85f,
                    PoliticalCapital = 200f,
                    Unrest = 5f,
                    SpecialNote = "Yüksek meşruiyet ve sermaye ile başlıyorsun. Reform için altın çağ.",
                    ObjectOverrides = new Dictionary<string, float>
                    {
                        { "gdp", 65f },
                        { "happiness", 70f },
                        { "education_level", 60f }
                    }
                }
            };
        }
    }
}