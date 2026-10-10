using System;
using System.Collections.Generic;
using System.Linq;
using DemocracySim.Engine.World;

namespace DemocracySim.Engine.Core
{
    /// <summary>
    /// FAZ 17: AI Director — oyunun küresel zorluğunu dinamik ayarlar.
    /// Oyuncu çok güçlüyse AI zorlaşır, çok zayıfsa AI yumuşar.
    /// Solitaire Mahjong ve Left 4 Dead'deki "Director" sisteminden esinlenmiştir.
    /// </summary>
    public class AIDirector
    {
        // ═══════════════════════════════════════════════════════════
        // AYARLAR
        // ═══════════════════════════════════════════════════════════
        public float DifficultyFactor { get; private set; } = 1.0f;   // 0.5 (kolay) .. 2.0 (zor)
        public int TurnsTracked { get; private set; } = 0;

        private const int HistorySize = 10;
        private readonly Queue<float> _playerPowerHistory = new Queue<float>();

        // ═══════════════════════════════════════════════════════════
        // HER TUR ÇAĞRILIR
        // ═══════════════════════════════════════════════════════════
        public void ProcessTurn(WorldManager world)
        {
            var player = world.Countries.FirstOrDefault(c => c.IsPlayerControlled);
            if (player == null) return;

            float playerPower = CalculatePlayerPower(player);
            _playerPowerHistory.Enqueue(playerPower);
            while (_playerPowerHistory.Count > HistorySize)
                _playerPowerHistory.Dequeue();

            if (++TurnsTracked < HistorySize) return;

            float avgPower = _playerPowerHistory.Average();

            // Hedef: ortalama güç 0.5 (dengeli) olsun.
            // Oyuncu 0.7+ ise AI zorlaşır, 0.3- ise AI yumuşar.
            float targetDifficulty = Math.Clamp(0.5f + (avgPower - 0.5f) * 1.5f, 0.5f, 2.0f);

            // Yumuşak geçiş (ani sıçrama yok)
            DifficultyFactor = Math.Clamp(
                DifficultyFactor * 0.9f + targetDifficulty * 0.1f,
                0.5f, 2.0f);

            SimLogger.Log($"[AI Director] Oyuncu gücü: {avgPower:F2}, Zorluk: {DifficultyFactor:F2}");
        }

        /// <summary>Oyuncunun gücünü 0.0 (zayıf) .. 1.0 (güçlü) olarak ölçer.</summary>
        private float CalculatePlayerPower(Country player)
        {
            var e = player.Engine;
            float power = 0f;
            int factors = 0;

            // Meşruiyet
            power += e.Legitimacy.CurrentLegitimacy / 100f; factors++;

            // Siyasi sermaye
            power += Math.Clamp(e.PoliticalCapital / e.MaxPoliticalCapital, 0f, 1f); factors++;

            // GSYİH
            power += e.Registry.GetValue(ObjectRegistry.Ids.Gdp, 50f) / 100f; factors++;

            // Huzursuzluk (düşükse güçlü)
            power += 1f - (e.Universe.Unrest / 100f); factors++;

            // Halk memnuniyeti
            if (e.Demographics.Count > 0)
            {
                power += e.Demographics.Average(g => g.Satisfaction) / 100f;
                factors++;
            }

            // Askeri sadakat
            power += e.Army.ArmySatisfaction / 100f; factors++;

            return factors > 0 ? power / factors : 0.5f;
        }

        /// <summary>AI ülkelerinin kararlarına uygulanacak çarpan.</summary>
        public float GetAIStatMultiplier() => DifficultyFactor;

        /// <summary>AI ülkelerinin kaynak kazanç çarpanı.</summary>
        public float GetAIResourceMultiplier() => 0.8f + DifficultyFactor * 0.2f;

        public string GetStatusText()
        {
            string label = DifficultyFactor < 0.7f ? "KOLAY"
                         : DifficultyFactor < 1.2f ? "NORMAL"
                         : DifficultyFactor < 1.6f ? "ZOR"
                         : "ÇOK ZOR";
            return $"[{label}] x{DifficultyFactor:F2}";
        }
    }
}