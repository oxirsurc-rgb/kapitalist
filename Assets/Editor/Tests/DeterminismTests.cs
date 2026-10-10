using NUnit.Framework;
using DemocracySim.Engine.Core;
using System.Collections.Generic;

namespace DemocracySim.Tests
{
    /// <summary>
    /// FAZ 18 + FAZ 23: Deterministik RNG testleri.
    /// Multiplayer için kritik — aynı seed + aynı girdi = aynı çıktı.
    /// </summary>
    public class DeterminismTests
    {
        [Test]
        public void SameSeed_ProducesSameSequence()
        {
            // Arrange
            var rng1 = new GameRandom(12345u);
            var rng2 = new GameRandom(12345u);

            // Act & Assert
            for (int i = 0; i < 100; i++)
            {
                Assert.AreEqual(rng1.NextUInt(), rng2.NextUInt(),
                    $"Aynı seed farklı değer üretti. Adım: {i}");
            }
        }

        [Test]
        public void DifferentSeed_ProducesDifferentSequence()
        {
            var rng1 = new GameRandom(11111u);
            var rng2 = new GameRandom(22222u);

            int sameCount = 0;
            for (int i = 0; i < 100; i++)
            {
                if (rng1.NextUInt() == rng2.NextUInt()) sameCount++;
            }

            Assert.Less(sameCount, 5, "Farklı seed'ler çok fazla aynı değer üretti.");
        }

        [Test]
public void Rng_StatePersistence_Works()
{
    // 1. RNG oluştur, 10 değer al (state ileri gitsin)
    var rng1 = new GameRandom(42u);
    for (int i = 0; i < 10; i++)
        rng1.NextUInt();

    // 2. State'i kaydet (bu noktadan sonra: v11, v12, ...)
    var savedState = rng1.GetState();

    // 3. Sonraki 10 değeri al (v11..v20)
    var expectedNext = new List<uint>();
    for (int i = 0; i < 10; i++)
        expectedNext.Add(rng1.NextUInt());

    // 4. Bir sürü daha değer al (state iyice ilerlesin)
    for (int i = 0; i < 20; i++)
        rng1.NextUInt();

    // 5. Kaydedilen state'ten yeni RNG oluştur
    var rng2 = new GameRandom(savedState);

    // 6. rng2'nin sonraki 10 değeri, kaydedilen state'ten sonraki
    //    ilk 10 değerle AYNI olmalı (v11..v20)
    for (int i = 0; i < 10; i++)
    {
        Assert.AreEqual(expectedNext[i], rng2.NextUInt(),
            $"State restore başarısız. Adım: {i}");
    }
}

        [Test]
        public void KapitalistRng_StreamIsolation_Works()
        {
            // Aynı master seed
            KapitalistRng.Initialize(9999u);

            // İki farklı stream
            var streamA1 = KapitalistRng.For("economy").NextUInt();
            var streamB1 = KapitalistRng.For("military").NextUInt();

            // Stream'ler birbirinden bağımsız olmalı
            KapitalistRng.Initialize(9999u);
            var streamA2 = KapitalistRng.For("economy").NextUInt();
            var streamB2 = KapitalistRng.For("military").NextUInt();

            Assert.AreEqual(streamA1, streamA2, "Aynı stream deterministik olmalı");
            Assert.AreEqual(streamB1, streamB2, "Aynı stream deterministik olmalı");
            Assert.AreNotEqual(streamA1, streamB1, "Farklı stream'ler farklı değer üretmeli");
        }

        [Test]
        public void FnvHash_IsStable()
        {
            // FNV-1a hash testi
            int hash1 = StableHash.Fnv1aInt("turkiye");
            int hash2 = StableHash.Fnv1aInt("turkiye");
            int hash3 = StableHash.Fnv1aInt("usa");

            Assert.AreEqual(hash1, hash2, "Aynı string aynı hash üretmeli");
            Assert.AreNotEqual(hash1, hash3, "Farklı string farklı hash üretmeli");
        }
    }
}