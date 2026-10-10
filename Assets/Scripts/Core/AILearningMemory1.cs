using System;
using System.Collections.Generic;
using System.Linq;

namespace DemocracySim.Engine.Core
{
    /// <summary>
    /// FAZ 0: AI'ın geçmiş deneyimlerinden öğrenmesini sağlar.
    /// </summary>
    [Serializable]
    public class AILearningMemory
    {
        // HEURISTIC LEARNING
        public Dictionary<string, float> ActionScores { get; set; } = new Dictionary<string, float>();
        public Dictionary<string, int> ActionAttempts { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, float> ActionDecay { get; set; } = new Dictionary<string, float>();

        // COUNTER-PLAY
        public Dictionary<string, int> PlayerPolicyCounts { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> PlayerOperationCounts { get; set; } = new Dictionary<string, int>();
        public float LastObservedPlayerAlignment { get; set; } = 0f;
        public List<float> PlayerLegitimacyHistory { get; set; } = new List<float>();

        private const int MaxPlayerPolicyTracked = 20;
        private const float ActionScoreDecayPerTurn = 0.02f;
        private const int MaxLegitimacyHistory = 5;


        // HEURISTIC LEARNING
        public void RecordAction(string actionId, float success)
        {
            if (string.IsNullOrEmpty(actionId)) return;
            success = Math.Clamp(success, 0f, 1f);

            if (!ActionScores.ContainsKey(actionId)) ActionScores[actionId] = 0.5f;
            if (!ActionAttempts.ContainsKey(actionId)) ActionAttempts[actionId] = 0;
            if (!ActionDecay.ContainsKey(actionId)) ActionDecay[actionId] = 1f;

            ActionScores[actionId] = ActionScores[actionId] * 0.8f + success * 0.2f;
            ActionAttempts[actionId]++;
        }

        public float GetActionScore(string actionId)
        {
            if (ActionScores.TryGetValue(actionId, out float score))
            {
                float decay = ActionDecay.TryGetValue(actionId, out float d) ? d : 1f;
                return score * decay;
            }
            return 0.5f;
        }

        public void ProcessTurn()
        {
            foreach (var key in ActionDecay.Keys.ToList())
            {
                ActionDecay[key] = Math.Max(0.5f, ActionDecay[key] - ActionScoreDecayPerTurn);
            }
        }

        public List<string> GetTopActions(int count = 3)
        {
            return ActionScores
                .OrderByDescending(kv => kv.Value * (ActionDecay.TryGetValue(kv.Key, out float d) ? d : 1f))
                .Take(count)
                .Select(kv => kv.Key)
                .ToList();
        }

        // COUNTER-PLAY
        public void RecordPlayerPolicy(string policyId)
        {
            if (string.IsNullOrEmpty(policyId)) return;
            if (!PlayerPolicyCounts.ContainsKey(policyId)) PlayerPolicyCounts[policyId] = 0;
            PlayerPolicyCounts[policyId]++;
        }

        public void RecordPlayerOperation(int opTypeIndex)
        {
            string key = "op_" + opTypeIndex;
            if (!PlayerOperationCounts.ContainsKey(key)) PlayerOperationCounts[key] = 0;
            PlayerOperationCounts[key]++;
        }

        public void ObservePlayerAlignment(float alignment)
        {
            LastObservedPlayerAlignment = alignment;
        }

        public void ObservePlayerLegitimacy(float legitimacy)
        {
            PlayerLegitimacyHistory.Add(legitimacy);
            if (PlayerLegitimacyHistory.Count > MaxLegitimacyHistory)
                PlayerLegitimacyHistory.RemoveAt(0);
        }

        public string GetMostUsedPlayerPolicy()
        {
            if (PlayerPolicyCounts.Count == 0) return null;
            return PlayerPolicyCounts.OrderByDescending(kv => kv.Value).First().Key;
        }

        public float GetPlayerLegitimacyTrend()
        {
            if (PlayerLegitimacyHistory.Count < 3) return 0f;
            float first = PlayerLegitimacyHistory.First();
            float last = PlayerLegitimacyHistory.Last();
            return last - first;
        }

        public bool IsPlayerErratic()
        {
            if (PlayerLegitimacyHistory.Count < 4) return false;
            float min = PlayerLegitimacyHistory.Min();
            float max = PlayerLegitimacyHistory.Max();
            return (max - min) > 30f;
        }

        public string GetCounterStrategy()
        {
            if (PlayerLegitimacyHistory.Count < 3) return null;

            float trend = GetPlayerLegitimacyTrend();
            string topPolicy = GetMostUsedPlayerPolicy();

            if (trend < -10f) return "AGGRESSIVE";
            if (trend > 10f) return "DEFENSIVE";
            if (IsPlayerErratic()) return "POPULIST";

            if (!string.IsNullOrEmpty(topPolicy))
                return "COUNTER_POLICY:" + topPolicy;

            return null;
        }

        public List<string> GetReport()
        {
            var lines = new List<string>();
            lines.Add("En başarılı AI aksiyonları:");
            foreach (var kv in ActionScores.OrderByDescending(kv => kv.Value).Take(3))
                lines.Add($"  {kv.Key}: %{kv.Value * 100:F0} ({ActionAttempts.GetValueOrDefault(kv.Key, 0)} deneme)");

            if (PlayerPolicyCounts.Count > 0)
            {
                lines.Add("Oyuncunun favori yasaları:");
                foreach (var kv in PlayerPolicyCounts.OrderByDescending(kv => kv.Value).Take(3))
                    lines.Add($"  {kv.Key}: {kv.Value} kez");
            }

            string counter = GetCounterStrategy();
            if (counter != null) lines.Add($"Karşı-strateji: {counter}");

            return lines;
        }
    }
}