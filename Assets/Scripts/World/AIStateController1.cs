using System;
using System.Collections.Generic;
using System.Linq;
using DemocracySim.Engine.Core;

namespace DemocracySim.Engine.World
{
    public static class AIStateController
    {

        /// <summary>FAZ 5: Denge testi için — AI ülkelerde kaç kez çöküş/isyan sıfırlaması oldu.</summary>
        public static int CollapseCount { get; set; } = 0;
        private static readonly Dictionary<string, AIPersonality> CountryPersonalities 
            = new Dictionary<string, AIPersonality>();

               public static void ExecuteAITurn(Country country, WorldManager world)
        {
            var e = country.Engine;
            var u = e.Universe;

            if (!CountryPersonalities.ContainsKey(country.Id))
            {
                var types = (AIPersonalityType[])Enum.GetValues(typeof(AIPersonalityType));
                CountryPersonalities[country.Id] = new AIPersonality(types[SimRng.Next(types.Length)]);
            }
            var personality = CountryPersonalities[country.Id];

            // FAZ 0: Öğrenme hafızasını yükle (yoksa oluştur)
            var memory = GetOrCreateMemory(country.Id);

            // 1. Temel temizlik (kriz, seçim)
            HandleBasicNecessities(country, u);

            // 2. FAZ 0: Oyuncu taktiklerini gözlemle
            if (!country.IsPlayerControlled)
            {
                ObservePlayer(world, memory);
            }

            // 3. FAZ 0: Utility AI ile karar ver
                        if (SimRng.NextDouble() < 0.40)
            {
                PerformUtilityDecision(e, personality, memory, country);

                // FAZ 4: Trust bazlı diplomatik kararlar
            PerformDiplomaticDecisions(country, world);
            }
                        

            // 4. Hafızayı temizle (decay)
            memory.ProcessTurn();

            // 5. Global hizalanma
            country.GlobalAlignment += (float)(SimRng.NextDouble() * 2 - 1);
            country.GlobalAlignment = Math.Clamp(country.GlobalAlignment, -100f, 100f);
        }

        // ============================================================
        // FAZ 0: AI HAFIZA SİSTEMİ
        // ============================================================
                public static readonly Dictionary<string, AILearningMemory> CountryMemories 
            = new Dictionary<string, AILearningMemory>();

        public static AILearningMemory GetOrCreateMemory(string countryId)
        {
            if (!CountryMemories.ContainsKey(countryId))
                CountryMemories[countryId] = new AILearningMemory();
            return CountryMemories[countryId];
        }

        /// <summary>Oyuncunun son taktiklerini gözlemle ve hafızaya kaydet.</summary>
        private static void ObservePlayer(WorldManager world, AILearningMemory memory)
        {
            var player = world.Countries.FirstOrDefault(c => c.IsPlayerControlled);
            if (player == null) return;

            var pe = player.Engine;

            // Oyuncunun meşruiyet trendi
            memory.ObservePlayerLegitimacy(pe.Legitimacy.CurrentLegitimacy);

            // Oyuncunun ideolojik konumu
            memory.ObservePlayerAlignment(pe.PlayerGlobalAlignment);

            // Oyuncunun geçirdiği son yasalar
            foreach (var pol in pe.AllObjects.OfType<SimPolicy>().Where(p => p.IsActive).Take(3))
            {
                memory.RecordPlayerPolicy(pol.Id);
            }
        }
                // ============================================================
        // FAZ 4: TRUST BAZLI DİPLOMATİK KARARLAR
        // ============================================================
        private static void PerformDiplomaticDecisions(Country country, WorldManager world)
        {
            var player = world.Countries.FirstOrDefault(c => c.IsPlayerControlled);

            // 1) Oyuncuya karşı düşmanlık → casusluk denemesi
            if (player != null && country.Memory.WillSpyOn(player.Id) && SimRng.NextDouble() < 0.15)
            {
                // Casusluk: başarı şansı %50
                bool success = SimRng.NextDouble() < 0.5f;
                if (success)
                {
                    country.Memory.ChangeTrust(player.Id, -5f, "Casusluk yapıldı (yakalanmadı)");
                    // Oyuncunun istihbarat bilgisi sızar (basit etki)
                    SimLogger.Log($"[Diplomasi] {country.Name} oyuncuya casusluk yaptı (başarılı).");
                }
                else
                {
                    // Yakalandı → trust daha çok düşer
                    country.Memory.ChangeTrust(player.Id, -15f, "Casusluk yapıldı (yakalandı)");
                    SimLogger.Log($"[Diplomasi] {country.Name} casusluk yaparken yakalandı!", SimLogger.LogLevel.Warning);
                }
            }

            // 2) Oyuncuyla dostane → örgüt daveti
            if (player != null && country.Memory.WillInviteToOrg(player.Id) && SimRng.NextDouble() < 0.08)
            {
                // Oyuncunun katılabileceği örgüt var mı?
                var org = world.Organizations.FirstOrDefault(o =>
                    !o.MemberIds.Contains(country.Id) == false &&   // AI üyesi
                    !o.MemberIds.Contains(player.Id) &&              // Oyuncu üye değil
                    o.CanJoin(player));                              // Oyuncu katılabilir

                if (org != null)
                {
                    // Davet gönder (basit etki: ilişki artar)
                    country.Memory.ChangeTrust(player.Id, DiplomaticMemory.OrgInvite, $"Örgüt daveti ({org.Name})");
                    try { world.NotifyCountry(player, $"🤝 {country.Name} sizi {org.Name} örgütüne davet ediyor!", false); } catch { }
                }
            }

            // 3) Aynı örgütte olmak trust'ı artırır
            foreach (var org in world.Organizations)
            {
                if (org.MemberIds.Contains(country.Id) && player != null && org.MemberIds.Contains(player.Id))
                {
                    if (SimRng.NextDouble() < 0.3)
                        country.Memory.ChangeTrust(player.Id, DiplomaticMemory.SameOrg, $"Aynı örgütte ({org.Name})");
                }
            }
        }

        // ============================================================
        // FAZ 0: UTILITY AI KARAR MEKANİZMASI
        // ============================================================
        private static void PerformUtilityDecision(SimulationEngine e, AIPersonality personality, AILearningMemory memory, Country country)
        {
            // Ham sinyaller
            float legit = e.Legitimacy.CurrentLegitimacy;
            float unrest = e.Universe.Unrest;
            float gdp = e.AllObjects.FirstOrDefault(o => o.Id == "gdp")?.ActualValue ?? 50f;
            float corruption = e.CorruptionLevel;
            float army = e.Army.ArmySatisfaction;
            float sanctions = e.Universe.SanctionLevel;

            // Utility skoru: ne kadar acil müdahale gerekli?
            float urgency = UtilityAI.ScoreAction(legit, unrest, gdp, corruption, army, sanctions, personality.Type);

            // Karşı-strateji: oyuncunun taktiklerine göre ne yapmalı?
            string counterStrategy = memory.GetCounterStrategy();

            // Eylem havuzunu utility'ye göre puanla
            var candidates = new List<(SimPolicy policy, float score)>();

            foreach (var policy in e.AllObjects.OfType<SimPolicy>())
            {
                if (policy.IsActive) continue;   // Zaten aktif yasaları atla

                float score = 0f;

                // 1. Hafıza skoru: bu yasa geçmişte başarılı mıydı?
                score += memory.GetActionScore(policy.Id) * 0.3f;

                // 2. Kişilik uyumu: yasa AI'nin ideolojisine yakın mı?
                float ideoDiff = Math.Abs(policy.IdeologicalAlignment - (int)personality.Type * 40f);
                score += (1f - Math.Clamp(ideoDiff / 100f, 0f, 1f)) * 0.3f;

                // 3. Aciliyet: mevcut krize uygun mu?
                score += urgency * 0.2f;

                // 4. Karşı-strateji bonusu
                if (counterStrategy == "AGGRESSIVE" && policy.IdeologicalAlignment > 20f) score += 0.15f;
                if (counterStrategy == "DEFENSIVE" && policy.IdeologicalAlignment < -20f) score += 0.15f;
                if (counterStrategy == "POPULIST" && policy.GroupImpacts.Count > 0) score += 0.15f;
                if (counterStrategy != null && counterStrategy.StartsWith("COUNTER_POLICY:"))
                {
                    string targetPolicy = counterStrategy.Split(':')[1];
                    if (policy.Id != targetPolicy) score += 0.1f;   // Oyuncunun favorisine karşı başka yasa seç
                }

                // 5. Rastgelelik: monotonluğu kır
                score += (float)SimRng.NextDouble() * 0.1f;

                candidates.Add((policy, score));
            }

            if (candidates.Count == 0) return;

            // En yüksek skorlu yasayı seç
            var best = candidates.OrderByDescending(c => c.score).First();

            // Utility aciliyeti çok düşükse ve karşı-strateji yoksa pas geç
            if (urgency < 0.3f && counterStrategy == null && SimRng.NextDouble() < 0.5) return;

            // Yasayı meclise sun
            e.ProposePolicy(best.policy.Id);
            if (country != null)
            {
                string actionText = $"[{personality.Type}] {best.policy.Name} yasasını önerdi" +
                                    (counterStrategy != null ? $" ({counterStrategy})" : "");
                country.RecordAction(actionText);
            }
            memory.RecordAction(best.policy.Id, 0.5f);   // Başlangıçta nötr, sonuç sonra değerlendirilir

            // FAZ 3.5: AI kararını doğal dilde açıkla
string explanation = AIDecisionExplainer.ExplainPolicyChoice(
    personality.Type, best.policy, urgency, counterStrategy, e);
country.RecordAction(explanation);

            SimLogger.Log($"[AI ÖĞRENME] {personality.Type}: {best.policy.Name} sunuldu. " +
                          $"Aciliyet: %{urgency * 100:F0}, Hafıza: %{memory.GetActionScore(best.policy.Id) * 100:F0}" +
                          (counterStrategy != null ? $", Karşı-strateji: {counterStrategy}" : ""));
        }

        private static void HandleBasicNecessities(Country country, UniverseState u)
        {
            var e = country.Engine;
            if (u.PendingCrisis != null)
            {
                var opt = u.PendingCrisis.Options[SimRng.Next(u.PendingCrisis.Options.Count)];
                opt.Effect?.Invoke(e);
                u.PendingCrisis = null;
            }
            if (u.GameOverReason != null)
            {
                CollapseCount++;
                u.GameOverReason = null; u.RevoltTurns = 0; u.Unrest = 30f;
                e.CurrentRole = SimulationEngine.PlayerRole.Governing;
                e.Legitimacy.SetLegitimacy(50f); e.TurnUntilElection = 12;
                foreach (var g in e.Demographics) g.AdjustSatisfaction(50f - g.Satisfaction);
            }
                      if (e.TurnUntilElection <= 0)
            {
                bool won = e.Elections.RunElection(e, e.PartyManager, false);
                e.CurrentRole = SimulationEngine.PlayerRole.Governing;
                e.Campaign.Reset(); e.TurnUntilElection = 12;
                // FAZ 0: Seçim eylemini kaydet
                country.RecordAction(won ? "Seçimi kazandı, iktidarda kaldı" : "Seçim yapıldı");
            }
        }

        private static void PerformStrategicMove(SimulationEngine e, AIPersonality personality)
        {
            // ⚠️ KRİTİK: ÖNCE liste tanımlanmalı, SONRA içine ekleme yapılmalı
            var signals = new List<AISignal>();

            // --- Sinyal 1: Meşruiyet ---
            if (e.Legitimacy.CurrentLegitimacy < 40f)
                signals.Add(new AISignal {
                    Type = SignalType.LowLegitimacy,
                    Severity = (40f - e.Legitimacy.CurrentLegitimacy) / 40f
                });

            // --- Sinyal 2: Enflasyon ---
            if (e.Economy.Inflation > 15f)
                signals.Add(new AISignal {
                    Type = SignalType.HighInflation,
                    Severity = Math.Min(1f, (e.Economy.Inflation - 15f) / 50f)
                });

            // --- Sinyal 3: Huzursuzluk ---
            if (e.Universe.Unrest > 40f)
                signals.Add(new AISignal {
                    Type = SignalType.HighUnrest,
                    Severity = (e.Universe.Unrest - 40f) / 60f
                });

            // --- Sinyal 4: Mutsuz gruplar ---
            if (e.Demographics.Any(g => g.Satisfaction < 30f))
                signals.Add(new AISignal {
                    Type = SignalType.UnhappyGroups,
                    Severity = 0.7f
                });

            // --- Sinyal 5: Ordu memnuniyetsizliği (darbe riski) ---
            if (e.Army.ArmySatisfaction < 35f)
                signals.Add(new AISignal {
                    Type = SignalType.LowArmySatisfaction,
                    Severity = (35f - e.Army.ArmySatisfaction) / 35f
                });

            // --- Sinyal 6: Yolsuzluk ---
            if (e.CorruptionLevel > 50f)
                signals.Add(new AISignal {
                    Type = SignalType.HighCorruption,
                    Severity = (e.CorruptionLevel - 50f) / 50f
                });

            // --- Sinyal 7: Yaptırım baskısı ---
            if (e.Universe.SanctionLevel > 40f)
                signals.Add(new AISignal {
                    Type = SignalType.HighSanctions,
                    Severity = (e.Universe.SanctionLevel - 40f) / 60f
                });

            // --- Sinyal 8: Derin devlet ---
            if (e.DeepStateStability < 40f)
                signals.Add(new AISignal {
                    Type = SignalType.LowDeepState,
                    Severity = (40f - e.DeepStateStability) / 40f
                });

            if (signals.Count == 0) return;

            var topSignal = signals
                .OrderByDescending(s => s.Severity * GetWeightForSignal(s.Type, personality))
                .First();

            var bestPolicy = FindPolicyForSignal(topSignal, e);

            if (bestPolicy != null)
            {
                e.ProposePolicy(bestPolicy.Id);
                SimLogger.Log($"AI Hükümet ({personality.Type}) stratejik hamle: " +
                              $"{bestPolicy.Name} meclise sunuldu. Hedef: {topSignal.Type}");
            }
        }

        private static float GetWeightForSignal(SignalType type, AIPersonality p)
        {
            return type switch
            {
                SignalType.LowLegitimacy       => p.WeightLegitimacy,
                SignalType.HighInflation       => p.WeightEconomy,
                SignalType.HighUnrest          => p.WeightStability,
                SignalType.UnhappyGroups       => p.PopulismTendency,
                SignalType.LowArmySatisfaction => p.WeightStability,
                SignalType.HighCorruption      => p.WeightLegitimacy,
                SignalType.HighSanctions       => p.WeightEconomy,
                SignalType.LowDeepState        => p.WeightStability,
                _                              => 0.5f
            };
        }

        private static SimPolicy FindPolicyForSignal(AISignal signal, SimulationEngine e)
        {
            var policies = e.AllObjects.OfType<SimPolicy>().ToList();

            List<SimPolicy> candidates = signal.Type switch
            {
                SignalType.LowLegitimacy       => policies.Where(p => Math.Abs(p.IdeologicalAlignment) < 25f).ToList(),
                SignalType.HighInflation       => policies.Where(p => p.Id.Contains("tax") || p.IdeologicalAlignment > 15f).ToList(),
                SignalType.HighUnrest          => policies.Where(p => p.IdeologicalAlignment < -15f).ToList(),
                SignalType.UnhappyGroups       => policies.Where(p => p.GroupImpacts != null && p.GroupImpacts.Count > 0).ToList(),
                SignalType.LowArmySatisfaction => policies.Where(p => p.Id.Contains("military") || p.Id.Contains("defense")).ToList(),
                SignalType.HighCorruption      => policies.Where(p => p.Id.Contains("justice") || p.Id.Contains("anti_corruption")).ToList(),
                SignalType.HighSanctions       => policies.Where(p => p.Id.Contains("trade") || p.Id.Contains("foreign")).ToList(),
                SignalType.LowDeepState        => policies.Where(p => p.Id.Contains("reform") || Math.Abs(p.IdeologicalAlignment) < 20f).ToList(),
                _                              => new List<SimPolicy>()
            };

            if (candidates.Count == 0) return null;
            return candidates[SimRng.Next(candidates.Count)];
        }

        // ⚠️ Enum artık 8 değer içeriyor
        private enum SignalType
        {
            LowLegitimacy,
            HighInflation,
            HighUnrest,
            UnhappyGroups,
            LowArmySatisfaction,
            HighCorruption,
            HighSanctions,
            LowDeepState
        }

        private class AISignal
        {
            public SignalType Type;
            public float Severity;
        }
    }
}