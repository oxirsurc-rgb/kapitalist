using System;
using System.Collections.Generic;
using System.Linq;

namespace DemocracySim.Engine.Core
{
    public enum ProtestTheme
    {
        Economic,   // Ekonomik — işçi hakları, enflasyon
        Freedom,    // Özgürlük — basın, ifade
        Justice     // Adalet — yolsuzluk, hukuk
    }

    /// <summary>FAZ 3: Muhalefet protestolarını yönetir.</summary>
    public class ProtestManager
    {
        public class ActiveProtest
        {
            public ProtestTheme Theme { get; set; }
            public int TurnsLeft { get; set; }
            public float InitialIntensity { get; set; }
            public int StartTurn { get; set; }
        }

        public ActiveProtest Current { get; private set; }
        public int CooldownTurns { get; set; } = 0;
        // Sabitler
        public const float CapitalCost = 20f;
        public const float FundCost = 10f;
        public const int Duration = 3;
        public const int Cooldown = 4;


        public bool CanStartProtest(SimulationEngine e)
        {
            if (e.CurrentRole != SimulationEngine.PlayerRole.Opposition) return false;
            if (Current != null) return false;
            if (CooldownTurns > 0) return false;
            if (e.PoliticalCapital < CapitalCost) return false;
            if (e.Party == null || e.Party.Fund < FundCost) return false;
            return true;
        }

        public string GetBlockReason(SimulationEngine e)
        {
            if (e.CurrentRole != SimulationEngine.PlayerRole.Opposition)
                return "Protesto sadece muhalefetteyken düzenlenebilir.";
            if (Current != null)
                return $"Zaten devam eden bir protesto var ({Current.TurnsLeft} tur kaldı).";
            if (CooldownTurns > 0)
                return $"Protesto için {CooldownTurns} tur beklemelisin.";
            if (e.PoliticalCapital < CapitalCost)
                return $"Yeterli sermaye yok ({CapitalCost:F0} gerekir).";
            if (e.Party == null || e.Party.Fund < FundCost)
                return $"Yeterli parti fonu yok ({FundCost:F0} gerekir).";
            return "";
        }

        public string StartProtest(ProtestTheme theme, SimulationEngine e)
        {
            if (!CanStartProtest(e)) return GetBlockReason(e);

            e.PoliticalCapital -= CapitalCost;
            e.Party.Fund -= FundCost;

            float intensity = 50f + (float)(SimRng.NextDouble() * 30f);   // 50-80 arası
            Current = new ActiveProtest
            {
                Theme = theme,
                TurnsLeft = Duration,
                InitialIntensity = intensity,
                StartTurn = e.CurrentTurn
            };

            // Başlangıç etkisi (anlık)
            ApplyImmediateEffect(e, theme);

            string themeStr = ThemeTurkish(theme);
            return $"{themeStr} temalı protesto başlatıldı. Yoğunluk: %{intensity:F0}. {Duration} tur sürecek.";
        }

        /// <summary>Her tur çağrılır. Devam eden protesto etki üretir.</summary>
        public void ProcessTurn(SimulationEngine e)
        {
            if (CooldownTurns > 0) CooldownTurns--;

            if (Current == null) return;

            Current.TurnsLeft--;

            // Her tur etki (orta şiddet)
            ApplyTurnEffect(e, Current.Theme, Current.InitialIntensity);

            // İfşa riski
            if (SimRng.NextDouble() < 0.15)
            {
                e.Legitimacy.AdjustLegitimacy(-3f);
                SimLogger.Log("[Protesto] Küçük çaplı polis müdahalesi — meşruiyet -3.", SimLogger.LogLevel.Warning);
            }

            if (Current.TurnsLeft <= 0)
            {
                // Bitiş raporu
                ApplyEndingEffect(e, Current.Theme);
                SimLogger.Log($"[Protesto] {ThemeTurkish(Current.Theme)} protestosu sona erdi.", SimLogger.LogLevel.Info);
                Current = null;
                CooldownTurns = Cooldown;
            }
        }

        private void ApplyImmediateEffect(SimulationEngine e, ProtestTheme theme)
        {
            // Başlangıç: herkes haberdar olur, sokak hareketlenir
            e.Universe.Unrest = Math.Clamp(e.Universe.Unrest + 10f, 0f, 100f);
            SimLogger.Log("[Protesto] Sokaklar hareketlendi (+10 huzursuzluk).", SimLogger.LogLevel.Info);
        }

        private void ApplyTurnEffect(SimulationEngine e, ProtestTheme theme, float intensity)
        {
            float scale = intensity / 50f;   // 1.0 .. 1.6 arası

            switch (theme)
            {
                case ProtestTheme.Economic:
                    // İşçi sınıfı + entelektüeller hükümete kızgın
                    e.Legitimacy.AdjustLegitimacy(-2f * scale);
                    e.Universe.Unrest = Math.Clamp(e.Universe.Unrest + 5f, 0f, 100f);
                    // Hükümetin ekonomi politikalarına karşı baskı
                    SimLogger.Log("[Protesto] Ekonomik protesto sürüyor.", SimLogger.LogLevel.Info);
                    break;

                case ProtestTheme.Freedom:
                    // Basın özgürlüğü vurgusu
                    e.Legitimacy.AdjustLegitimacy(-1.5f * scale);
                    e.Universe.Unrest = Math.Clamp(e.Universe.Unrest + 3f, 0f, 100f);
                    e.DeepStateStability = Math.Max(0f, e.DeepStateStability - 2f);
                    SimLogger.Log("[Protesto] Özgürlük protestosu sürüyor.", SimLogger.LogLevel.Info);
                    break;

                case ProtestTheme.Justice:
                    // Yolsuzluk eleştirisi
                    e.Legitimacy.AdjustLegitimacy(-2.5f * scale);
                    e.Universe.Unrest = Math.Clamp(e.Universe.Unrest + 4f, 0f, 100f);
                    e.CorruptionLevel = Math.Max(0f, e.CorruptionLevel - 0.5f);  // Yolsuzluk ortaya dökülüyor
                    SimLogger.Log("[Protesto] Adalet protestosu sürüyor.", SimLogger.LogLevel.Info);
                    break;
            }
        }

        private void ApplyEndingEffect(SimulationEngine e, ProtestTheme theme)
        {
            // Bitiş: kısa vadeli kazanç — halk desteği + meşruiyet
            switch (theme)
            {
                case ProtestTheme.Economic:
                    e.Legitimacy.AdjustLegitimacy(3f);
                    foreach (var g in e.Demographics.Where(g => g.Id == "workers" || g.Id == "intellectuals"))
                        g.AdjustSatisfaction(3f);
                    break;
                case ProtestTheme.Freedom:
                    e.Legitimacy.AdjustLegitimacy(4f);
                    foreach (var g in e.Demographics.Where(g => g.Id == "intellectuals"))
                        g.AdjustSatisfaction(5f);
                    break;
                case ProtestTheme.Justice:
                    e.Legitimacy.AdjustLegitimacy(2f);
                    e.CorruptionLevel = Math.Max(0f, e.CorruptionLevel - 3f);
                    break;
            }
        }
                /// <summary>Save/Load için: mevcut protestoyu geri yükle.</summary>
        public void RestoreProtest(ProtestTheme theme, int turnsLeft, float intensity)
        {
            Current = new ActiveProtest
            {
                Theme = theme,
                TurnsLeft = turnsLeft,
                InitialIntensity = intensity,
                StartTurn = 0
            };
        }

        public static string ThemeTurkish(ProtestTheme theme)
        {
            switch (theme)
            {
                case ProtestTheme.Economic: return "Ekonomik";
                case ProtestTheme.Freedom:  return "Özgürlük";
                case ProtestTheme.Justice:  return "Adalet";
                default: return theme.ToString();
            }
        }

        public string GetStatusText()
        {
            if (Current != null)
                return $"Protesto: {ThemeTurkish(Current.Theme)} — {Current.TurnsLeft} tur kaldı";
            if (CooldownTurns > 0)
                return $"Protesto cooldown: {CooldownTurns} tur";
            return "Protesto hazır";
        }
    }
}