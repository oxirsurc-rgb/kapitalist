using System;
using System.Collections.Generic;
using System.Linq;

namespace DemocracySim.Engine.Core
{
    /// <summary>
    /// FAZ 3.5: AI kararlarını doğal dilde açıklar.
    /// Şu an şablon tabanlı; ileride LLM entegrasyonu için genişletilebilir.
    /// </summary>
    public static class AIDecisionExplainer
    {
        /// <summary>Bir AI kararını açıklayan metin üretir.</summary>
        public static string ExplainPolicyChoice(
            AIPersonalityType personality,
            SimPolicy chosenPolicy,
            float urgency,
            string counterStrategy,
            SimulationEngine e)
        {
            var sb = new System.Text.StringBuilder();

            // 1. Kişilik temelli giriş
            string personalityIntro = personality switch
            {
                AIPersonalityType.Populist   => "Halkın nabzını tutarak",
                AIPersonalityType.Ideologue  => "İdeolojik ilkelerimize bağlı kalarak",
                AIPersonalityType.Technocrat => "Verilere ve verimliliğe bakarak",
                AIPersonalityType.Autocrat   => "Düzeni ve istikrarı önceliklendirerek",
                _ => "Durumu değerlendirerek"
            };

            sb.Append($"{personalityIntro}, ");

            // 2. Aciliyet temelli gerekçe
            string urgencyText = urgency switch
            {
                > 0.7f => "acil bir müdahale gerekiyor",
                > 0.4f => "durum kontrol altına alınmalı",
                _      => "mevcut durumu iyileştirmek için"
            };
            sb.Append($"{urgencyText}. ");

            // 3. Politika seçimi
            sb.Append($"Bu nedenle {chosenPolicy.Name} yasasını öneriyoruz. ");

            // 4. Karşı-strateji varsa açıkla
            if (!string.IsNullOrEmpty(counterStrategy))
            {
                string counterText = counterStrategy switch
                {
                    "AGGRESSIVE" => "Oyuncunun meşruiyeti düşüyor, agresif politikalarla baskıyı artıracağız.",
                    "DEFENSIVE"  => "Oyuncu güçleniyor, savunmacı politikalarla dengeyi koruyacağız.",
                    "POPULIST"   => "Oyuncu tutarsız, popülist politikalarla halkı kazanacağız.",
                    _ when counterStrategy.StartsWith("COUNTER_POLICY:") 
                        => $"Oyuncunun favori yasası {counterStrategy.Split(':')[1]}, biz karşıt politikayla yanıt vereceğiz.",
                    _ => "Oyuncunun stratejisine karşı hamle yapıyoruz."
                };
                sb.Append(counterText);
            }

            return sb.ToString();
        }

        /// <summary>Bir AI fraksiyon talebini açıklar.</summary>
        public static string ExplainFactionDemand(Faction f, FactionDemand demand, SimPolicy policy)
        {
            string side = f.Ideology < -20f ? "sol" : (f.Ideology > 20f ? "sağ" : "merkez");
            return $"{f.Name} ({side} kanat), {policy.Name} yasasının {demand.TargetValue:F0} " +
                   $"seviyesine çekilmesini talep ediyor. " +
                   $"Kabul edilirse destek +{demand.SupportReward:F0}, reddedilirse -{demand.RejectionPenalty:F0}.";
        }

        /// <summary>Bir bakanın istifa nedenini açıklar.</summary>
        public static string ExplainResignation(PoliticalActor minister, SimulationEngine e)
        {
            var reasons = new List<string>();

            if (minister.Loyalty < 40f)
                reasons.Add($"sadakati düşük (%{minister.Loyalty:F0})");

            float ideoDiff = Math.Abs(minister.Ideology - e.PlayerGlobalAlignment);
            if (ideoDiff > 60f)
                reasons.Add($"ideolojik olarak uzaklaştı (mesafe {ideoDiff:F0})");

            if (e.Legitimacy.CurrentLegitimacy < 35f)
                reasons.Add($"hükümetin meşruiyeti zayıf (%{e.Legitimacy.CurrentLegitimacy:F0})");

            if (reasons.Count == 0) return $"{minister.Name} istifa etti.";

            return $"{minister.Name} istifa etti çünkü {string.Join(", ", reasons)}.";
        }

        
        /// <summary>
/// FAZ 3.5: LLM entegrasyonu için hazırlık.
/// Şu an şablon tabanlı; LLM aktifse gerçek açıklama üretir.
/// </summary>
        /// <summary>
        /// FAZ 3.5: LLM entegrasyonu. Yerel LLM aktifse açıklama üretir; değilse şablon kullanır.
        /// </summary>
        public static void RequestLLMExplanation(string prompt, Action<string> onComplete)
        {
            if (LLMClient.IsReady)
            {
                LLMClient.EmbellishText(prompt, onComplete);
                return;
            }

            // Fallback: şablon tabanlı
            onComplete?.Invoke(prompt);
        }

        /// <summary>
        /// Yasa Meclis'te oylandığında dinamik basın ve muhalefet tepkisi üretir (LLM veya kural tabanlı).
        /// </summary>
        public static void GeneratePolicyPublicReaction(SimPolicy policy, bool isPassed, SimulationEngine e, Action<string> onComplete)
        {
            float legit = e.Legitimacy.CurrentLegitimacy;
            float unrest = e.Universe.Unrest;
            string outcome = isPassed ? "kabul edildi" : "reddedildi";

            string fallback;
            if (isPassed)
            {
                if (unrest > 50f)
                    fallback = $"[Muhalefet]: \"{policy.Name} kararı toplumsal gerilimi tırmandırır!\"";
                else if (legit > 60f)
                    fallback = $"[Basın]: \"Hükümet Meclis'te güven tazeledi; {policy.Name} yürürlüğe giriyor.\"";
                else
                    fallback = $"[Kulis]: \"{policy.Name} yasası zorlu bir oylamayla geçti.\"";
            }
            else
            {
                fallback = $"[Muhalefet]: \"{policy.Name} tasarısının reddedilmesi iktidara açık bir uyarıdır!\"";
            }

            if (LLMClient.IsReady)
            {
                string prompt = $"Bir siyasi simülasyon oyununda '{policy.Name}' yasası Meclis'te {outcome}. " +
                               $"Hükümet meşruiyeti %{legit:F0}, halk huzursuzluğu %{unrest:F0}. " +
                               $"Muhalefet lideri veya bir gazete manşeti ağzından tek cümlelik çarpıcı bir Türkçe demeç yaz:\nManşet:";
                LLMClient.ExplainDecision(prompt, result =>
                {
                    if (string.IsNullOrEmpty(result) || result.Contains("Error") || result.Contains("Exception"))
                        onComplete?.Invoke(fallback);
                    else
                        onComplete?.Invoke($"[Basın]: {result.Trim()}");
                });
            }
            else
            {
                onComplete?.Invoke(fallback);
            }
        }
    }
}