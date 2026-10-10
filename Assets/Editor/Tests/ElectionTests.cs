using NUnit.Framework;
using DemocracySim.Engine.Core;
using DemocracySim.Engine.Legislative;

namespace DemocracySim.Tests
{
    /// <summary>
    /// FAZ 18: Seçim sistemi testleri.
    /// </summary>
    public class ElectionTests
    {
        [Test]
        public void DHondt_Allocates100Seats()
        {
            var pm = new PartyManager();
            var votes = new System.Collections.Generic.Dictionary<string, float>
            {
                { "workers", 30f },
                { "conservative", 25f },
                { "nationalist", 20f },
                { "liberal", 15f },
                { "green", 10f }
            };

            pm.AllocateSeats(votes);

            int totalSeats = 0;
            foreach (var p in pm.Parties) totalSeats += p.Seats;

            Assert.AreEqual(100, totalSeats, "Toplam sandalye her zaman 100 olmalı");
        }

        [Test]
        public void DHondt_HigherVotes_GetMoreSeats()
        {
            var pm = new PartyManager();
            var votes = new System.Collections.Generic.Dictionary<string, float>
            {
                { "workers", 40f },
                { "conservative", 30f },
                { "nationalist", 15f },
                { "liberal", 10f },
                { "green", 5f }
            };

            pm.AllocateSeats(votes);

            var workers = pm.Parties.Find(p => p.Id == "workers");
            var green = pm.Parties.Find(p => p.Id == "green");

            Assert.Greater(workers.Seats, green.Seats,
                "Yüksek oy alan parti daha çok sandalye almalı");
        }

        [Test]
        public void DHondt_EmptyVotes_DoesNotCrash()
        {
            var pm = new PartyManager();
            Assert.DoesNotThrow(() => pm.AllocateSeats(new System.Collections.Generic.Dictionary<string, float>()));
        }
    }
}