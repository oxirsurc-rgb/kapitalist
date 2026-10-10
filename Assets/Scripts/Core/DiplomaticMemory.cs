using System;
using System.Collections.Generic;
using System.Linq;

namespace DemocracySim.Engine.Core
{
    /// <summary>
    /// FAZ 4: Diplomasi Hafızası.
    /// Her ülke, diğer ülkelere karşı "Trust Score" (güven skoru) tutar.
    /// Skor; geçmiş eylemlere göre değişir, zamanla sönümlenir.
    /// Araştırma: Suzerain trust system, Crusader Kings opinion system.
    /// </summary>
    [Serializable]
    public class DiplomaticMemory
    {
        /// <summary>Ülke ID'si → Trust Score (0-100).</summary>
        public Dictionary<string, float> TrustScores { get; set; } = new Dictionary<string, float>();

        /// <summary>Ülke ID'si → son eylemler listesi (en yeni başta, max 5).</summary>
        public Dictionary<string, List<TrustEvent>> TrustHistory { get; set; } = new Dictionary<string, List<TrustEvent>>();

        /// <summary>Ülke ID'si → o ülkeyle yapılan son olayların kronolojisi.</summary>
        public Dictionary<string, int> InteractionCounts { get; set; } = new Dictionary<string, int>();

        private const int MaxHistoryPerCountry = 5;
        private const float NeutralTrust = 50f;

        // ============================================================
        // SABİT ETKİLER
        // ============================================================

        public const float SpySuccess = -10f;
        public const float SpyCaught = -30f;
        public const float SabotageFailed = -25f;
        public const float SabotageSuccess = -40f;
        public const float ManipulationCaught = -20f;
        public const float TradeAgreement = +15f;
        public const float OrgInvite = +20f;
        public const float SameOrg = +1f;
        public const float WarThreat = -50f;
        public const float CulturalExchange = +10f;
        public const float ForeignAidGiven = +8f;
        public const float BorderIncident = -15f;

        // ============================================================
        // API
        // ============================================================

        /// <summary>Bir ülkeye karşı mevcut güven skorunu döndürür.</summary>
        public float GetTrust(string countryId)
        {
            if (string.IsNullOrEmpty(countryId)) return NeutralTrust;
            return TrustScores.TryGetValue(countryId, out float score) ? score : NeutralTrust;
        }

        /// <summary>Bir ülkeye karşı güven skorunu değiştirir ve geçmişe kaydeder.</summary>
        public void ChangeTrust(string countryId, float delta, string reason)
        {
            if (string.IsNullOrEmpty(countryId)) return;

            float current = GetTrust(countryId);
            float newScore = Math.Clamp(current + delta, 0f, 100f);
            TrustScores[countryId] = newScore;

            // Geçmişe ekle
            if (!TrustHistory.ContainsKey(countryId))
                TrustHistory[countryId] = new List<TrustEvent>();

            TrustHistory[countryId].Insert(0, new TrustEvent
            {
                Delta = delta,
                Reason = reason,
                Turn = -1,   // Çağıran taraf set eder
                ScoreAfter = newScore
            });
            while (TrustHistory[countryId].Count > MaxHistoryPerCountry)
                TrustHistory[countryId].RemoveAt(MaxHistoryPerCountry);

            // Etkileşim sayacı
            if (!InteractionCounts.ContainsKey(countryId)) InteractionCounts[countryId] = 0;
            InteractionCounts[countryId]++;

            string sign = delta >= 0 ? "+" : "";
            SimLogger.Log($"[Diplomasi] Trust {countryId}: {sign}{delta:F0} ({reason}) → {newScore:F0}");
        }

        /// <summary>Her turda çağrılır. Trust skorları zamanla nötre doğru sönümlenir.</summary>
        public void ProcessTurn(int currentTurn)
        {
            var toUpdate = new List<string>(TrustScores.Keys);

            foreach (var countryId in toUpdate)
            {
                float current = TrustScores[countryId];

                // -20 altındaysa düşmanlık kalıcı (decay yok)
                if (current < 20f) continue;

                // Nötre doğru yavaş kayma
                float target = NeutralTrust;
                float diff = target - current;
                float decay = Math.Sign(diff) * Math.Min(Math.Abs(diff), 0.5f);
                TrustScores[countryId] = Math.Clamp(current + decay, 0f, 100f);
            }
        }

        // ============================================================
        // AI DAVRANIŞ KARARI
        // ============================================================

        /// <summary>Belirli bir ülkeye karşı AI'ın tutumunu döndürür.</summary>
        public TrustTier GetTier(string countryId)
        {
            float t = GetTrust(countryId);
            if (t < 20f) return TrustTier.Enemy;
            if (t < 40f) return TrustTier.Cold;
            if (t < 60f) return TrustTier.Neutral;
            if (t < 80f) return TrustTier.Friendly;
            return TrustTier.Ally;
        }

        /// <summary>AI'ın bu ülkeyle ticaret yapıp yapmayacağını belirler.</summary>
        public bool WillTradeWith(string countryId)
        {
            return GetTrust(countryId) >= 35f;   // Soğuk ve üstü → evet
        }

        /// <summary>AI'ın bu ülkeye örgüt daveti gönderip göndermeyeceğini belirler.</summary>
        public bool WillInviteToOrg(string countryId)
        {
            return GetTrust(countryId) >= 65f;   // Dostane ve üstü → evet
        }

        /// <summary>AI'ın bu ülkeye casusluk yapıp yapmayacağını belirler.</summary>
        public bool WillSpyOn(string countryId)
        {
            return GetTrust(countryId) <= 30f;   // Soğuk ve altı → evet
        }

        // ============================================================
        // RAPORLAMA
        // ============================================================

        public List<string> GetReport()
        {
            var lines = new List<string>();
            lines.Add("DİPLOMATİK GÜVEN SKORLARI:");
            foreach (var kv in TrustScores.OrderBy(kv => kv.Value))
            {
                TrustTier tier = GetTier(kv.Key);
                string tierStr = tier switch
                {
                    TrustTier.Enemy => "DÜŞMAN",
                    TrustTier.Cold => "Soğuk",
                    TrustTier.Neutral => "Nötr",
                    TrustTier.Friendly => "Dostane",
                    TrustTier.Ally => "MÜTTEFİK",
                    _ => "?"
                };
                lines.Add($"  {kv.Key}: {kv.Value:F0} ({tierStr})");
            }
            return lines;
        }
    }

    // ============================================================
    // YARDIMCI SINIFLAR
    // ============================================================

    public enum TrustTier
    {
        Enemy,      // 0-20: Düşman
        Cold,       // 20-40: Soğuk
        Neutral,    // 40-60: Nötr
        Friendly,   // 60-80: Dostane
        Ally        // 80-100: Müttefik
    }

    [Serializable]
    public class TrustEvent
    {
        public float Delta { get; set; }
        public string Reason { get; set; }
        public int Turn { get; set; }
        public float ScoreAfter { get; set; }
    }
}