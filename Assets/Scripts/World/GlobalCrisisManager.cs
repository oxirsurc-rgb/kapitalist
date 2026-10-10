using System;
using System.Collections.Generic;
using DemocracySim.Engine.Core;
using DemocracySim.Engine.World;

namespace DemocracySim.Engine.World
{
    public class GlobalCrisis
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public Action<WorldManager> Effect { get; set; }
    }

    public class GlobalCrisisManager
    {
        public List<GlobalCrisis> PotentialCrises { get; set; } = new List<GlobalCrisis>();

        public GlobalCrisisManager()
        {
            PotentialCrises.Add(new GlobalCrisis {
                Title = "KÜRESEL EKONOMİK BUHRAN",
                Description = "Dünya borsaları çöktü! Tüm ülkelerde ekonomik durgunluk başladı.",
                Effect = (world) => {
                    foreach(var c in world.Countries) {
                        var gdp = c.Engine.AllObjects.Find(o => o.Id == "gdp");
                        if (gdp != null) gdp.ActualValue -= 10f;
                        c.Engine.Economy.UpdateEconomy(c.Engine);
                    }
                }
            });

            PotentialCrises.Add(new GlobalCrisis {
                Title = "KÜRESEL PANDEMİ",
                Description = "Yeni bir virüs dünyaya yayıldı. Hükümetler zor durumda!",
                Effect = (world) => {
                    foreach(var c in world.Countries) {
                        c.Engine.Legitimacy.AdjustLegitimacy(-15f);
                    }
                }
            });

            PotentialCrises.Add(new GlobalCrisis {
                Title = "DİPLOMATİK GERGİNLİK",
                Description = "Süper güçler arasında gerginlik arttı. Ticaret yolları tehlikede.",
                Effect = (world) => {
                    foreach(var c1 in world.Countries) {
                        foreach(var c2 in world.Countries) {
                            if (c1 == c2) continue;
                            world.Diplomacy.DamageRelation(c1.Id, c2.Id, 10f);
                        }
                    }
                }
            });
        }

        public GlobalCrisis CheckForGlobalCrisis()
        {
            if (SimRng.NextDouble() < 0.15)
            {
                return PotentialCrises[SimRng.Next(PotentialCrises.Count)];
            }
            return null;
        }
    }
}
