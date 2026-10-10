using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using DemocracySim.Engine.Core;
using DemocracySim.Engine.Data;
using DemocracySim.Engine.World;

namespace DemocracySim.Tests
{
    /// <summary>
    /// Otomatik Denge ve İstikrar Testi (CI & QA Runner).
    /// Çoklu dünyalarda çoklu turlar boyunca AI ülkelerini koşturur;
    /// çöküş oranı, meşruiyet ve huzursuzluk dengelerini doğrular.
    /// </summary>
    public class BalanceSimulationTests
    {
        [Test]
        public void Simulation_5Worlds_20Turns_RemainsStable()
        {
            var profiles = DataManager.LoadCountryProfiles();
            Assert.IsNotEmpty(profiles, "Country profiles yüklenemedi!");

            int worlds = 5;
            int turns = 20;
            int totalCrashes = 0;
            var legitList = new List<float>();
            var unrestList = new List<float>();

            for (int w = 0; w < worlds; w++)
            {
                var world = new WorldManager();
                foreach (var p in profiles)
                {
                    var c = new Country(p.id, p.name, false)
                    {
                        GlobalAlignment = p.globalAlignment,
                        Continent = p.continent
                    };
                    DataManager.LoadWorld(c.Engine, "");
                    DataManager.AddDefaultDemographics(c.Engine, p);
                    c.Engine.Legitimacy.SetLegitimacy(p.startingLegitimacy);
                    c.Engine.PoliticalCapital = p.startingCapital;
                    world.Countries.Add(c);
                }

                for (int t = 0; t < turns; t++)
                {
                    try
                    {
                        world.ProcessWorldTurn();
                    }
                    catch (Exception ex)
                    {
                        totalCrashes++;
                        Assert.Fail($"Simülasyon Dünya {w}, Tur {t} çöktü: {ex.Message}");
                    }
                }

                foreach (var c in world.Countries)
                {
                    legitList.Add(c.Engine.Legitimacy.CurrentLegitimacy);
                    unrestList.Add(c.Engine.Universe.Unrest);
                }
            }

            Assert.AreEqual(0, totalCrashes, "Simülasyonda motor çökmesi yaşandı!");

            float avgLegit = legitList.Average();
            float avgUnrest = unrestList.Average();

            Assert.GreaterOrEqual(avgLegit, 20f, "Ortalama meşruiyet kritik seviyenin altında!");
            Assert.LessOrEqual(avgLegit, 95f, "Ortalama meşruiyet tavana yapışmış!");
            Assert.LessOrEqual(avgUnrest, 80f, "Ortalama huzursuzluk kontrol dışına çıkmış!");
        }
    }
}
