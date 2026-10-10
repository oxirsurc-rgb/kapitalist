using System;
using System.Collections.Generic;
using System.Linq;

namespace DemocracySim.Engine.Core
{
    /// <summary>
    /// FAZ 4: Başarım sistemi — 15 başarım, oyuncunun farklı oyun tarzlarını ödüllendirir.
    /// </summary>
    [Serializable]
    public class AchievementManager
    {
        public class Achievement
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public string Icon { get; set; }   // ASCII: *, +, !, #, vs.
            public Func<SimulationEngine, bool> Check { get; set; }
        }

        // Kazanılan başarımlar
        public HashSet<string> Unlocked { get; set; } = new HashSet<string>();

        // Son kazanılan başarım (UI bildirim için)
        [NonSerialized] public string LastUnlocked = null;

        // ============================================================
        // 15 BAŞARIM TANIMI
        // ============================================================

        public static readonly List<Achievement> AllAchievements = new List<Achievement>
        {
            new Achievement
            {
                Id = "first_turn", Icon = "*",
                Name = "İlk Adım",
                Description = "İlk turu tamamla.",
                Check = e => e.CurrentTurn >= 1
            },
            new Achievement
            {
                Id = "election_win", Icon = "+",
                Name = "Sandık Zaferi",
                Description = "İlk seçimi kazan.",
                Check = e => e.Elections.ConsecutiveTerms >= 1
            },
            new Achievement
            {
                Id = "opposition", Icon = "!",
                Name = "Muhalefet Yılları",
                Description = "Muhalefete düş.",
                Check = e => e.CurrentRole == SimulationEngine.PlayerRole.Opposition
            },
            new Achievement
            {
                Id = "return_to_power", Icon = "#",
                Name = "Geri Dönüş",
                Description = "Muhalefetten iktidara geri dön.",
                Check = e => e.CurrentRole == SimulationEngine.PlayerRole.Governing
                             && e.Elections.ConsecutiveTerms >= 1
                             && e.CurrentTurn >= 20
            },
            new Achievement
            {
                Id = "economist", Icon = "$",
                Name = "Ekonomist",
                Description = "GSYİH'yı 80'in üstüne çıkar.",
               Check = e => e.Registry.GetValue(ObjectRegistry.Ids.Gdp, 0f) >= 80f
            },
            new Achievement
            {
                Id = "tech_giant", Icon = "T",
                Name = "Teknoloji Devi",
                Description = "Teknoloji seviyesini 75'in üstüne çıkar.",
                Check = e => e.Technology != null && e.Technology.TechLevel >= 75f
            },
            new Achievement
            {
                Id = "treasurer", Icon = "M",
                Name = "Hazine Bekçisi",
                Description = "20 tur boyunca bütçe dengesi pozitif kalsın.",
                Check = e => e.CalculateBudgetBalance() >= 0f && e.CurrentTurn >= 20
            },
            new Achievement
            {
                Id = "people_hero", Icon = "H",
                Name = "Halk Kahramanı",
                Description = "Tüm halk gruplarının memnuniyeti %70 üstüne çıksın.",
                Check = e => e.Demographics.Count > 0 &&
                             e.Demographics.All(g => g.Satisfaction >= 70f)
            },
            new Achievement
            {
                Id = "iron_fist", Icon = "I",
                Name = "Demir Yumruk",
                Description = "Gözetim seviyesini 70'in üstüne çıkar.",
                Check = e => e.Universe.SurveillanceLevel >= 70f
            },
            new Achievement
            {
                Id = "crisis_manager", Icon = "C",
                Name = "Kriz Yöneticisi",
                Description = "5 krizi çöz.",
                Check = e => e.CrisisChains != null && e.CrisisChains.TotalCrisesResolved >= 5
            },
            new Achievement
            {
                Id = "three_terms", Icon = "3",
                Name = "İmparator",
                Description = "3 ardışık seçim kazan.",
                Check = e => e.Elections.ConsecutiveTerms >= 3
            },
            new Achievement
            {
                Id = "reformist", Icon = "R",
                Name = "Reformist",
                Description = "20 özel yasa çıkar.",
                Check = e => e.AllObjects.OfType<SimPolicy>().Count(p => p.Id.StartsWith("custom") && p.IsActive) >= 20
            },
            new Achievement
            {
                Id = "green_vision", Icon = "G",
                Name = "Yeşil Vizyon",
                Description = "Çevre kalitesini 75'in üstüne çıkar.",
                Check = e => e.Registry.GetValue(ObjectRegistry.Ids.EnvironmentQuality, 0f) >= 75f
            },
            new Achievement
            {
                Id = "no_radicals", Icon = "0",
                Name = "Toplumsal Barış",
                Description = "Hiç radikalleşmiş grup olmasın ve 30. tura ulaş.",
                Check = e => e.CurrentTurn >= 30 &&
                             e.Universe.Radicalized != null &&
                             !e.Universe.Radicalized.Any(kv => kv.Value)
            },
            new Achievement
            {
                Id = "survivor", Icon = "S",
                Name = "Hayatta Kalan",
                Description = "50. tura ulaş.",
                Check = e => e.CurrentTurn >= 50
            }
        };

        // ============================================================
        // TUR DÖNGÜSÜ
        // ============================================================

        /// <summary>Her tur çağrılır. Yeni kazanılan başarımları kontrol eder.</summary>
        public List<Achievement> ProcessTurn(SimulationEngine e)
        {
            var newlyUnlocked = new List<Achievement>();

            foreach (var ach in AllAchievements)
            {
                if (Unlocked.Contains(ach.Id)) continue;

                try
                {
                    if (ach.Check(e))
                    {
                        Unlocked.Add(ach.Id);
                        LastUnlocked = ach.Name;
                        newlyUnlocked.Add(ach);
                        SimLogger.Log($"[Başarım] ✅ {ach.Name} kazanıldı!", SimLogger.LogLevel.Warning);
                    }
                }
                catch (Exception ex)
                {
                    SimLogger.Log($"[Başarım] {ach.Id} hatası: {ex.Message}", SimLogger.LogLevel.Error);
                }
            }

            return newlyUnlocked;
        }

        // ============================================================
        // RAPOR
        // ============================================================

        public int TotalUnlocked => Unlocked.Count;
        public int TotalAchievements => AllAchievements.Count;

        public List<string> GetReport()
        {
            var lines = new List<string>
            {
                $"KAZANILAN BAŞARIMLAR: {TotalUnlocked}/{TotalAchievements}"
            };

            foreach (var ach in AllAchievements)
            {
                bool done = Unlocked.Contains(ach.Id);
                string status = done ? "[✓]" : "[ ]";
                lines.Add($"{status} {ach.Name} — {ach.Description}");
            }

            return lines;
        }
    }
}