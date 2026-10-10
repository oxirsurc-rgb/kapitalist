using System;
using System.Collections.Generic;

namespace DemocracySim.Engine.Core
{
    /// <summary>
    /// FAZ 0: Utility AI — Dave Mark yaklaşımı.
    /// </summary>
    public static class UtilityAI
    {
        public enum CurveType { Linear, Quadratic, Exponential, Sigmoid, SmoothStep }

        public static float Normalize(float value, float min, float max)
        {
            if (max <= min) return 0f;
            return Math.Clamp((value - min) / (max - min), 0f, 1f);
        }

        public static float ApplyCurve(float input01, CurveType curve)
        {
            input01 = Math.Clamp(input01, 0f, 1f);
            switch (curve)
            {
                case CurveType.Linear:      return input01;
                case CurveType.Quadratic:   return input01 * input01;
                case CurveType.Exponential: return (float)((Math.Exp(input01) - 1) / (Math.E - 1));
                case CurveType.Sigmoid:     return 1f / (1f + (float)Math.Exp(-10f * (input01 - 0.5f)));
                case CurveType.SmoothStep:  return input01 * input01 * (3f - 2f * input01);
                default:                    return input01;
            }
        }

        public static float CompensationFactor(int count, float sum)
        {
            if (count == 0) return 0f;
            float modifiedSum = (sum + 1f) / count;
            return Math.Clamp(modifiedSum * modifiedSum, 0f, 1f);
        }

        public static float ScoreAction(float legitimacy, float unrest, float gdp, float corruption,
                                float army, float sanctions, AIPersonalityType personality)
{
    float wLegit, wUnrest, wGdp, wCorruption, wArmy, wSanctions;
    switch (personality)
    {
        case AIPersonalityType.Populist:
            wLegit = 0.30f; wUnrest = 0.25f; wGdp = 0.10f; wCorruption = 0.05f; wArmy = 0.10f; wSanctions = 0.05f;
            break;
        case AIPersonalityType.Ideologue:
            wLegit = 0.15f; wUnrest = 0.15f; wGdp = 0.10f; wCorruption = 0.25f; wArmy = 0.15f; wSanctions = 0.10f;
            break;
        case AIPersonalityType.Technocrat:
            wLegit = 0.10f; wUnrest = 0.10f; wGdp = 0.40f; wCorruption = 0.15f; wArmy = 0.05f; wSanctions = 0.10f;
            break;
        case AIPersonalityType.Autocrat:
            wLegit = 0.05f; wUnrest = 0.30f; wGdp = 0.10f; wCorruption = 0.05f; wArmy = 0.35f; wSanctions = 0.05f;
            break;
        default:
            wLegit = 0.20f; wUnrest = 0.20f; wGdp = 0.20f; wCorruption = 0.10f; wArmy = 0.15f; wSanctions = 0.05f;
            break;
    }

    // FAZ 3.5: DİNAMİK AĞIRLIK — mevcut duruma göre aciliyet artır
    // Meşruiyet kritikse ağırlığı iki katına çıkar
    if (legitimacy < 30f) wLegit *= 2.0f;
    else if (legitimacy < 45f) wLegit *= 1.4f;

    // Huzursuzluk kritikse ağırlığı artır
    if (unrest > 60f) wUnrest *= 2.0f;
    else if (unrest > 40f) wUnrest *= 1.4f;

    // GSYİH düşükse ağırlığı artır (teknokrat için ekstra)
    if (gdp < 30f)
    {
        wGdp *= 1.8f;
        if (personality == AIPersonalityType.Technocrat) wGdp *= 1.3f;
    }

    // Yaptırım yüksekse ağırlığı artır
    if (sanctions > 50f) wSanctions *= 2.0f;

    // Askeri memnuniyet kritikse ağırlığı artır (otokrat için ekstra)
    if (army < 30f)
    {
        wArmy *= 2.0f;
        if (personality == AIPersonalityType.Autocrat) wArmy *= 1.5f;
    }

    float total = 0f;
    float weightSum = 0f;

    total += ApplyCurve(Normalize(100f - legitimacy, 0f, 100f), CurveType.Sigmoid) * wLegit;      weightSum += wLegit;
    total += ApplyCurve(Normalize(unrest, 0f, 100f), CurveType.Quadratic) * wUnrest;               weightSum += wUnrest;
    total += ApplyCurve(Normalize(100f - gdp, 0f, 100f), CurveType.Linear) * wGdp;                 weightSum += wGdp;
    total += ApplyCurve(Normalize(corruption, 0f, 100f), CurveType.Linear) * wCorruption;          weightSum += wCorruption;
    total += ApplyCurve(Normalize(100f - army, 0f, 100f), CurveType.Sigmoid) * wArmy;              weightSum += wArmy;
    total += ApplyCurve(Normalize(sanctions, 0f, 100f), CurveType.Exponential) * wSanctions;       weightSum += wSanctions;

    return weightSum > 0f ? total / weightSum : 0f;
}
    }
}