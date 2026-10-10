using System;
using System.Collections.Generic;
using System.Linq;

namespace DemocracySim.Engine.Core
{
    public class LegitimacyManager
    {
        public float CurrentLegitimacy { get; private set; } = 100f;

        // Skandal, bastırma, darbe gibi olayların bıraktığı kalıcı iz.
        // RecalculateFromDemographics her tur meşruiyeti sıfırdan hesapladığı için, bu iz olmadan
        // tüm AdjustLegitimacy cezaları/ödülleri bir sonraki turda siliniyordu. Her tur %20 sönümlenir.
        public float Modifier { get; set; } = 0f;
        public float GetEffectMultiplier() => CurrentLegitimacy < 20f ? 0.1f : (CurrentLegitimacy < 50f ? 0.5f : 1.0f);

        // --- YENİ: Gruplara dayalı meşruiyet hesaplama ---
        public void RecalculateFromDemographics(List<DemographicGroup> groups)
        {
            if (groups == null || groups.Count == 0) return;

            float weightedSum = 0;
            float totalInfluence = 0;

            foreach (var group in groups)
            {
                weightedSum += group.Satisfaction * group.Influence;
                totalInfluence += group.Influence;
            }

            float baseValue = totalInfluence > 0 ? weightedSum / totalInfluence : 50f;
            Modifier *= 0.8f;
            CurrentLegitimacy = Math.Clamp(baseValue + Modifier, 0f, 100f);
        }

        public void AdjustLegitimacy(float delta)
        {
            CurrentLegitimacy = Math.Clamp(CurrentLegitimacy + delta, 0f, 100f);
            Modifier = Math.Clamp(Modifier + delta, -100f, 100f);
        }

        // Kayıt/Yükleme sistemi için: mutlak değeri doğrudan ayarlar (AdjustLegitimacy delta ile çalışır, bu ile karıştırma)
        public void SetLegitimacy(float value)
{
    CurrentLegitimacy = Math.Clamp(value, 0f, 100f);
    
    // ═══════════════════════════════════════════════════════════
    // EK-32: Senaryo meşruiyetinin 1. turda sıfırlanmasını engelle
    // ═══════════════════════════════════════════════════════════
    // RecalculateFromDemographics her tur baseValue + Modifier hesaplıyor.
    // Modifier = 0 ise senaryo değeri kaybolur.
    // Senaryo değerini base 50'ye göre fark olarak sakla.
    Modifier = value - 50f;
}
    }
}
