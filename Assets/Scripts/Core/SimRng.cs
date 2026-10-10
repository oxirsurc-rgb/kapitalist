using System;

namespace DemocracySim.Engine.Core
{
    /// <summary>
    /// Geriye dönük uyumluluk katmanı.
    /// Mevcut tüm SimRng.NextDouble(), SimRng.Chance() çağrıları çalışmaya devam eder,
    /// ama artık deterministik KapitalistRng'ye delege eder.
    /// 
    /// YENİ kod yazarken KapitalistRng.For("sistemAdı") kullanın.
    /// </summary>
    public static class SimRng
    {
        private const string DefaultStream = "default";

        public static int Seed { get; private set; } = 0;

        public static void SetSeed(int seed)
        {
            Seed = seed;
            KapitalistRng.Initialize((uint)seed);
            SimLogger.Log($"[SimRng] Tohum ayarlandı: {seed}");
        }

        public static void Reseed()
        {
            uint newSeed = (uint)DateTime.UtcNow.Ticks;
            Seed = unchecked((int)newSeed);
            KapitalistRng.Initialize(newSeed);
            SimLogger.Log($"[SimRng] Yeni tohum: {newSeed}");
        }

        // Mevcut çağrılar varsayılan akışı kullanır
        private static GameRandom Default => KapitalistRng.For(DefaultStream);

        public static double NextDouble() => Default.NextDouble();
        public static int Next(int max) => Default.Next(max);
        public static int Next(int min, int max) => Default.Next(min, max);
        public static float NextFloat() => Default.NextFloat();
        public static bool Chance(float probability) => Default.Chance(probability);
        public static float Range(float min, float max) => Default.Range(min, max);
    }
}