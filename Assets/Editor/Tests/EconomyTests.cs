using NUnit.Framework;
using DemocracySim.Engine.Core;

namespace DemocracySim.Tests
{
    /// <summary>
    /// FAZ 18: Ekonomi sistemi testleri.
    /// </summary>
    public class EconomyTests
    {
        [Test]
        public void Inflation_AdjustClampsToRange()
        {
            var econ = new EconomyManager();

            econ.AdjustInflation(200f);
            Assert.LessOrEqual(econ.Inflation, 100f, "Enflasyon max 100 olmalı");

            econ.AdjustInflation(-500f);
            Assert.GreaterOrEqual(econ.Inflation, -2f, "Enflasyon min -2 olmalı");
        }

        [Test]
        public void Debt_CannotGoNegative()
        {
            var econ = new EconomyManager();
            econ.AdjustDebt(-10000f);
            Assert.GreaterOrEqual(econ.NationalDebt, 0f, "Borç negatif olamaz");
        }

        [Test]
        public void LoadState_RestoresValues()
        {
            var econ = new EconomyManager();
            econ.LoadState(15f, 3000f, 45f);

            Assert.AreEqual(15f, econ.Inflation);
            Assert.AreEqual(3000f, econ.NationalDebt);
            Assert.AreEqual(45f, econ.CreditRating);
        }

        [Test]
        public void CreditRating_ClampedTo0_100()
        {
            var econ = new EconomyManager();
            var engine = new SimulationEngine();

            // Sürekli borç açığı → kredi notu düşmeli
            for (int i = 0; i < 100; i++)
            {
                econ.LoadState(50f, 50000f, 100f);
                econ.UpdateEconomy(engine);
            }

            Assert.GreaterOrEqual(econ.CreditRating, 0f);
            Assert.LessOrEqual(econ.CreditRating, 100f);
        }
    }
}