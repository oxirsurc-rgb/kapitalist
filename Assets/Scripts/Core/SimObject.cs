using System;
using System.Collections.Generic;

namespace DemocracySim.Engine.Core
{
    public abstract class SimObject
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public float ActualValue { get; set; }
        public float PerceivedValue { get; set; }
        public float TargetValue { get; set; }
        public float MinValue { get; set; } = 0f;
        public float MaxValue { get; set; } = 100f;
        public float IdeologicalAlignment { get; set; } = 0f;

        // K2: Denge noktası. Etkiler olmadan bu değere geri döner.
        public float EquilibriumValue { get; set; }

        // K2: Denge noktasına dönüş oranı (0 = geri çekme yok).
        public float ReversionRate { get; set; } = 0f;

        // FAZ 1: Etki ataleti — değerin hedefe yaklaşma hızı (0 = hiç, 1 = anında).
        public float Inertia { get; set; } = 0.2f;

        // FAZ 1: Son 20 turun değerleri (mini grafik için)
        public List<float> History { get; set; } = new List<float>();
        private const int MaxHistoryLength = 20;

        public List<SimEffect> IncomingEffects { get; set; } = new List<SimEffect>();
        public List<SimEffect> OutgoingEffects { get; set; } = new List<SimEffect>();
        public SimEffect TopImpactor { get; set; }

        protected SimObject(string id, string name, float initialValue)
        {
            Id = id; Name = name;
            ActualValue = initialValue;
            EquilibriumValue = initialValue;
            PerceivedValue = initialValue;
            TargetValue = initialValue;
        }

        public void Clamp()
        {
            ActualValue = Math.Clamp(ActualValue, MinValue, MaxValue);
            PerceivedValue = Math.Clamp(PerceivedValue, MinValue, MaxValue);
        }

        // FAZ 1: Her tur sonunda çağrılır — geçmişe değer ekler.
        public void RecordHistory()
        {
            History.Add(ActualValue);
            while (History.Count > MaxHistoryLength)
                History.RemoveAt(0);
        }

        // FAZ 1: Son N turun trend yönü: +1 artıyor, -1 azalıyor, 0 sabit.
        public int GetTrendDirection(int lookback = 5)
        {
            if (History.Count < lookback) return 0;
            float oldVal = History[History.Count - lookback];
            float newVal = History[History.Count - 1];
            float diff = newVal - oldVal;
            if (Math.Abs(diff) < 0.3f) return 0;
            return diff > 0 ? 1 : -1;
        }

        // FAZ 1: Son N turun ortalama değişim hızı.
        public float GetTrendMagnitude(int lookback = 5)
        {
            if (History.Count < lookback) return 0f;
            float sum = 0f;
            for (int i = History.Count - lookback; i < History.Count - 1; i++)
                sum += History[i + 1] - History[i];
            return sum / (lookback - 1);
        }

        // FAZ 1: Bu objeye gelen tüm etkileri katkıya göre sıralı döndürür.
        public List<(SimEffect effect, float contribution)> GetEffectBreakdown()
        {
            var list = new List<(SimEffect, float)>();
            foreach (var eff in IncomingEffects)
            {
                float contrib = eff.GetCurrentContribution();
                if (Math.Abs(contrib) < 0.05f) continue;
                list.Add((eff, contrib));
            }
            list.Sort((a, b) => Math.Abs(b.Item2).CompareTo(Math.Abs(a.Item2)));
            return list;
        }

        // FAZ 1: Son N turda toplam değişim.
        public float GetChangeSince(int turnsAgo)
        {
            if (History.Count < turnsAgo + 1) return 0f;
            float oldVal = History[History.Count - 1 - turnsAgo];
            return ActualValue - oldVal;
        }
    }

    public class SimStatistic : SimObject
    {
        public const float DefaultReversionRate = 0.05f;
        public const float DefaultInertia = 0.15f;   // İstatistikler yavaş değişir

        public SimStatistic(string id, string name, float val) : base(id, name, val)
        {
            ReversionRate = DefaultReversionRate;
            Inertia = DefaultInertia;
        }
    }

    public class SimPolicy : SimObject
    {
        public bool IsActive { get; set; } = false;
        public float Intensity { get; set; } = 0f;

        public Dictionary<string, float> Frames { get; set; } = new Dictionary<string, float>();
        public Dictionary<string, float> GroupImpacts { get; set; } = new Dictionary<string, float>();

        // Bürokrasi/derin devlet bu değeri düşürerek yasayı sabote edebilir.
        public float ImplementationSpeed { get; set; } = 1.0f;

        // FAZ 1 (K5): Bütçe kategorisi — SADECE BİR KEZ
        public PolicyBudgetType BudgetType { get; set; } = PolicyBudgetType.Regulation;

        public SimPolicy(string id, string name, float val) : base(id, name, val)
        {
            Inertia = 1.0f;   // Politikalar anında değişir
        }

        public float GetEffectiveValue() => IsActive ? (ActualValue * Intensity * ImplementationSpeed) : 0f;
    }

    // FAZ 1 (K5): Politikaların bütçe kategorisi — CLASS'IN DIŞINDA, NAMESPACE İÇİNDE
    public enum PolicyBudgetType
    {
        Revenue,      // Vergi — gelir
        Expense,      // Harcama — gider
        Regulation    // Düzenleme — nötr
    }
}