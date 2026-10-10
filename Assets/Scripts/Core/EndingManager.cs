using System;
using System.Collections.Generic;
using System.Linq;

namespace DemocracySim.Engine.Core
{
    /// <summary>
    /// FAZ 4: Çoklu oyun sonu — oyuncunun final durumuna göre farklı ending'ler.
    /// </summary>
    public static class EndingManager
    {
        public class Ending
        {
            public string Id;
            public string Title;
            public string Description;
            public string[] Tags;
        }
        
        /// <summary>Final durumunu analiz eder ve uygun ending'i döndürür.</summary>
        public static Ending Evaluate(SimulationEngine e, int totalTurns)
        {
            float legit = e.Legitimacy.CurrentLegitimacy;
            float unrest = e.Universe.Unrest;
float gdp = e.Registry.GetValue(ObjectRegistry.Ids.Gdp, 50f);
            float corruption = e.CorruptionLevel;
            float inequality = e.Registry.GetValue(ObjectRegistry.Ids.Inequality, 50f);
float environment = e.Registry.GetValue(ObjectRegistry.Ids.EnvironmentQuality, 50f);
            float tech = e.Technology?.TechLevel ?? 50f;
            int electionCount = e.Elections?.ConsecutiveTerms ?? 0;
            
            // 1) Totaliter Dönüşüm
            if (e.Universe.SurveillanceLevel > 70f && e.Universe.PresidentialPowers && legit > 60f)
            {
                return new Ending
                {
                    Id = "totalitarian",
                    Title = "TOTALİTER DÖNÜŞÜM",
                    Description = $"Rejiminiz gözetim ve kararname gücüyle tam bir otoriter yapıya dönüştü. " +
                                  $"{totalTurns} tur boyunca ülkeyi 'düzen' adına yönettiniz. " +
                                  "Tarih sizi ya 'kurtarıcı' ya 'tiran' olarak anacak.",
                    Tags = new[] { "Otoriter", "Gözetim", "Kararname" }
                };
            }
            
            // 2) Teknokrat Cennet
            if (tech > 75f && gdp > 70f && inequality < 40f && legit > 60f)
            {
                return new Ending
                {
                    Id = "technocratic_utopia",
                    Title = "TEKNOKRAT CENNET",
                    Description = $"Ülkeniz teknoloji ve refahta zirveye ulaştı. Eğitimli, eşit ve müreffeh " +
                                  $"bir toplum kurdunuz. {totalTurns} turda bir çağ atladınız.",
                    Tags = new[] { "Teknoloji", "Eşitlik", "Refah" }
                };
            }
            
            // 3) Demokratik Zafer
            if (legit > 70f && unrest < 20f && electionCount >= 3)
            {
                return new Ending
                {
                    Id = "democratic_triumph",
                    Title = "DEMOKRATİK ZAFER",
                    Description = $"Sandık başında defalarca kazandınız ve halkın güvenini korudunuz. " +
                                  $"{electionCount} ardışık seçim zaferi bir demokrasi klasiği olarak yazıldı.",
                    Tags = new[] { "Demokrasi", "Sandık", "İstikrar" }
                };
            }
            
            // 4) İsyancı Devrim (zaten oyun sonu — GameOver'dan sonra çağrılır)
            if (unrest > 85f || legit < 10f)
            {
                return new Ending
                {
                    Id = "revolutionary",
                    Title = "İSYANCI DEVRİM",
                    Description = "Halk sokakları ele geçirdi ve hükümetiniz devrildi. " +
                                  $"Tarih {totalTurns} turluk iktidarınızı bir uyarı olarak yazacak.",
                    Tags = new[] { "Devrim", "İsyan", "Çöküş" }
                };
            }
            
            // 5) Ekonomik Çöküş
            if (gdp < 30f || corruption > 70f)
            {
                return new Ending
                {
                    Id = "economic_collapse",
                    Title = "EKONOMİK ÇÖKÜŞ",
                    Description = $"Ekonomi yönetilemez hale geldi. {totalTurns} tur boyunca harcadınız " +
                                  "ama hazine boşaldı. Ülke iflastan kurtulamadı.",
                    Tags = new[] { "İflas", "Yolsuzluk", "Çöküş" }
                };
            }
            
            // 6) Popülist Diktatörlük
            if (legit > 55f && corruption > 40f && e.Universe.SurveillanceLevel > 40f && electionCount >= 2)
            {
                return new Ending
                {
                    Id = "populist_dictatorship",
                    Title = "POPÜLİST DİKTATÖRLÜK",
                    Description = "Halk desteği ve yolsuzluk el ele yürüdü. " +
                                  $"{totalTurns} turda bir 'seçimli otorite' kurdunuz.",
                    Tags = new[] { "Popülizm", "Yolsuzluk", "Otorite" }
                };
            }
            
            // 7) Yeşil Vizyon
            if (environment > 75f && tech > 60f && gdp > 55f)
            {
                return new Ending
                {
                    Id = "green_vision",
                    Title = "YEŞİL VİZYON",
                    Description = "Çevre ve teknolojiyi dengede tuttunuz. " +
                                  $"Ülkeniz sürdürülebilir kalkınmanın öncüsü oldu.",
                    Tags = new[] { "Çevre", "Sürdürülebilirlik", "Gelecek" }
                };
            }
            
            // 8) Dengeli Yönetim (default)
            return new Ending
            {
                Id = "balanced",
                Title = "DENGELİ YÖNETİM",
                Description = $"Ne tam zafer ne tam çöküş. {totalTurns} tur boyunca ülkeyi " +
                              "ılımlı bir çizgide yönettiniz. Tarih sizi 'istikrarlı' olarak anacak.",
                Tags = new[] { "Denge", "Istikrar", "Ilımlılık" }
            };
        }
        
        /// <summary>Ending'i UI'da göstermek için modal aç.</summary>
      
    }
}