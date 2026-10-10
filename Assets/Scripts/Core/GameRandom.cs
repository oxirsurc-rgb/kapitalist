using System;
using System.Runtime.CompilerServices;
using System.Collections.Generic;

namespace DemocracySim.Engine.Core
{
    /// <summary>
    /// Deterministik Rastgele Sayı Üreteci (xoshiro128** tabanlı).
    /// System.Random yerine kullanılır; sürümler arası kararlı, durumu kaydedilebilir.
    /// </summary>
    [Serializable]
    public struct GameRandomState
    {
        public uint s0, s1, s2, s3;

        public static GameRandomState FromSeed(uint seed)
        {
            ulong x = seed;
            uint Next()
            {
                x += 0x9E3779B97F4A7C15UL;
                ulong z = x;
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                return (uint)((z ^ (z >> 31)) & 0xFFFFFFFF);
            }
            return new GameRandomState { s0 = Next(), s1 = Next(), s2 = Next(), s3 = Next() };
        }
    }

    public class GameRandom
    {
        private uint _s0, _s1, _s2, _s3;

        public GameRandom(uint seed)
        {
            var state = GameRandomState.FromSeed(seed);
            _s0 = state.s0; _s1 = state.s1; _s2 = state.s2; _s3 = state.s3;
            if ((_s0 | _s1 | _s2 | _s3) == 0) _s0 = 1;
        }

        public GameRandom(GameRandomState state)
        {
            _s0 = state.s0; _s1 = state.s1; _s2 = state.s2; _s3 = state.s3;
        }

        public GameRandomState GetState() => new GameRandomState { s0 = _s0, s1 = _s1, s2 = _s2, s3 = _s3 };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint Rotl(uint x, int k) => (x << k) | (x >> (32 - k));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint NextUInt()
        {
            uint result = Rotl(_s1 * 5, 7) * 9;
            uint t = _s1 << 9;
            _s2 ^= _s0;
            _s3 ^= _s1;
            _s1 ^= _s2;
            _s0 ^= _s3;
            _s2 ^= t;
            _s3 = Rotl(_s3, 11);
            return result;
        }

        public double NextDouble() => (NextUInt() >> 8) * (1.0 / 16777216.0);
        public float NextFloat() => (float)NextDouble();
        public int Next(int max) => max <= 0 ? 0 : (int)(NextUInt() % (uint)max);
        public int Next(int min, int max) => min + Next(max - min);
        public bool Chance(float probability) => NextDouble() < probability;
        public float Range(float min, float max) => min + NextFloat() * (max - min);
        public T Pick<T>(IList<T> list) => (list == null || list.Count == 0) ? default : list[Next(list.Count)];
    }

    /// <summary>Sabit FNV-1a hash — string.GetHashCode() yerine.</summary>
    public static class StableHash
    {
        private const uint FnvOffsetBasis = 2166136261;
        private const uint FnvPrime = 16777619;

        public static uint Fnv1a(string text)
        {
            if (string.IsNullOrEmpty(text)) return FnvOffsetBasis;
            uint hash = FnvOffsetBasis;
            for (int i = 0; i < text.Length; i++)
            {
                hash ^= text[i];
                hash *= FnvPrime;
            }
            return hash;
        }

        public static int Fnv1aInt(string text) => unchecked((int)Fnv1a(text));
    }

    /// <summary>Merkezi RNG yöneticisi — her sistem için ayrı akış.</summary>
    public static class KapitalistRng
    {
        private static readonly Dictionary<string, GameRandom> _streams = new Dictionary<string, GameRandom>();
        private static uint _masterSeed;

        public static uint MasterSeed => _masterSeed;

        public static void Initialize(uint? seed = null)
        {
            _masterSeed = seed ?? (uint)DateTime.UtcNow.Ticks;
            _streams.Clear();
        }

        public static void Restore(uint seed, Dictionary<string, GameRandomState> states)
        {
            _masterSeed = seed;
            _streams.Clear();
            if (states == null) return;
            foreach (var kv in states)
                _streams[kv.Key] = new GameRandom(kv.Value);
        }

        public static Dictionary<string, GameRandomState> ExportStates()
        {
            var dict = new Dictionary<string, GameRandomState>();
            foreach (var kv in _streams) dict[kv.Key] = kv.Value.GetState();
            return dict;
        }

        public static GameRandom For(string streamName)
        {
            if (!_streams.TryGetValue(streamName, out var rng))
            {
                uint streamSeed = _masterSeed ^ StableHash.Fnv1a(streamName);
                rng = new GameRandom(streamSeed);
                _streams[streamName] = rng;
            }
            return rng;
        }
    }
}