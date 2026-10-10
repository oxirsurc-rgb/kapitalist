using System;

namespace DemocracySim.Engine.Core
{
    // Etkinin kaynaktaki değişime göre hedefi hangi yönde etkilediğini belirler
    public enum EffectType
    {
        Positive,   // Kaynak artınca hedef de artar
        Negative    // Kaynak artınca hedef azalır
    }

    // Etkinin şiddetinin değere göre nasıl ölçekleneceğini belirler
    public enum CurveType
    {
        Linear,
        Exponential,
        Logarithmic,
        Sigmoid
    }

    public class SimEffect
    {
        public SimObject Source { get; set; }
        public SimObject Target { get; set; }
        // K1: Yön yalnızca Type'tan gelir; Strength her zaman >= 0 tutulur (çift negatif imkânsız).
        private float _strength;
        public float Strength { get => _strength; set => _strength = Math.Abs(value); }
        public EffectType Type { get; set; }
        public CurveType Curve { get; set; } = CurveType.Linear;

        public SimEffect(SimObject source, SimObject target, float strength, EffectType type)
        {
            Source = source;
            Target = target;
            Strength = strength;
            Type = type;
        }
                /// <summary>
        /// FAZ 1: Bu etkinin o anki hedefe katkısını döndürür (impact trace için).
        /// CalculateImpact ile aynı formülü kullanır.
        /// </summary>
        public float GetCurrentContribution()
        {
            return CalculateImpact();
        }

        /// <summary>
        /// FAZ 1: Etkiyi açıklayan okunabilir metin.
        /// </summary>
        public string GetDescription()
        {
            string direction = Type == EffectType.Positive ? "artırıyor" : "azaltıyor";
            string srcName = Source?.Name ?? Source?.Id ?? "?";
            return $"{srcName} → {direction}";
        }

        // K3 + K2: Politikada yalnızca yürürlükteki etkin değer (pasifse 0, bürokrasi hızı dahil) sayılır.
        // İstatistikte ise denge noktasından sapma sayılır; böylece dengedeki sistem sabit kalır.
        public float SourceSignal()
        {
            if (Source is SimPolicy p) return p.GetEffectiveValue();
            return Source.ActualValue - Source.EquilibriumValue;
        }

        public float CalculateImpact()
        {
            float baseValue = SourceSignal() * (Strength / 100f);
            float directionMultiplier = Type == EffectType.Negative ? -1f : 1f;

            float curved = Curve switch
            {
                CurveType.Linear => baseValue,
                CurveType.Exponential => Math.Sign(baseValue) * (float)Math.Pow(Math.Abs(baseValue), 1.5),
                CurveType.Logarithmic => Math.Sign(baseValue) * (float)Math.Log(Math.Abs(baseValue) + 1),
                CurveType.Sigmoid => (float)((2.0 / (1.0 + Math.Exp(-baseValue / 10.0))) - 1.0) * 50f,
                _ => baseValue
            };

            return curved * directionMultiplier;
        }
    }
}