using System;
using System.Collections.Generic;
using System.Linq;

namespace DemocracySim.Engine.Core
{
    /// <summary>
    /// FAZ 3: Kriz Zinciri Yöneticisi.
    /// Çok aşamalı krizleri yönetir: her aşama farklı seçenekler ve etkiler sunar.
    /// Araştırma referansı: CK3 Situations (3 fazlı), Suzerain (gecikmeli kriz),
    /// Global Leaders (oyuncunun zayıf göstergelerine ağırlıklandırma).
    /// </summary>
    [Serializable]
    public class CrisisChainManager
    {
        public List<ActiveCrisis> ActiveCrises { get; set; } = new List<ActiveCrisis>();
        public Dictionary<string, int> ResolvedCrises { get; set; } = new Dictionary<string, int>();
        public int TotalCrisesTriggered { get; set; } = 0;
        public int TotalCrisesResolved { get; set; } = 0;


        // ============================================================
        // SABİTLER (Araştırma: Crisis Cabinet 3-4 tur sonra zorla çözer)
        // ============================================================
        public const int MaxActiveCrises = 3;
        public const int CrisisAutoResolveTurns = 4;   // Bu süre sonunda otomatik çözülür
        public const float AutoResolvePenaltyMultiplier = 1.5f;   // %150 ceza

        // ============================================================
        // ANA TUR DÖNGÜSÜ
        // ============================================================

            public void ProcessTurn(SimulationEngine e, float globalTension)
{
    // EK-21: Oyuncu muhalefetteyken hükümet krizleri kendi çözer.
    // Oyuncu muhalefetteyken kriz popup'ı almaz — sadece iktidardayken müdahale eder.
    bool isOppositionPlayer = e.IsPlayerCountry
                              && e.CurrentRole == SimulationEngine.PlayerRole.Opposition;

    // Aktif krizleri ilerlet
    for (int i = ActiveCrises.Count - 1; i >= 0; i--)
    {
        var crisis = ActiveCrises[i];

        // Muhalefetteyken ceza yarıya iner (oyuncu sorumlu değil)
        float penaltyMultiplier = isOppositionPlayer ? 0.4f : 1.0f;
        ApplyTurnPenalty(crisis, e, penaltyMultiplier);

        crisis.TurnsSinceStart++;

        if (crisis.CurrentStage < crisis.TotalStages - 1
            && crisis.TurnsSinceStart >= crisis.StageDurationTurns)
        {
            AdvanceStage(crisis, e);
        }

        // Muhalefetteyken kriz otomatik çözülür (hükümet çözdü kabul et)
        // İktidarken oyuncu müdahale etmezse 4 turda otomatik çözülür
        if (isOppositionPlayer || crisis.TurnsSinceStart >= CrisisAutoResolveTurns)
        {
            AutoResolveCrisis(crisis, e);
            ActiveCrises.RemoveAt(i);

            // Muhalefetteyken oyuncuya haber ver
            if (isOppositionPlayer)
            {
                e.Universe.Messages.Add(new UniverseMessage
                {
                    Text = $"📰 Hükümet '{crisis.Title}' krizini çözdü. Muhalefet olarak nasıl yönettiğini izliyorsunuz.",
                    IsWarning = false
                });
            }
        }
    }

    // Yeni kriz — sadece iktidardayken veya AI ülkeleri için
    if (!isOppositionPlayer
        && ActiveCrises.Count < MaxActiveCrises
        && SimRng.NextDouble() < BalanceConfig.Instance.CrisisTriggerChance)
    {
        TryTriggerNewCrisis(e, globalTension);
    }
}

        // ============================================================
        // AŞAMA YÖNETİMİ
        // ============================================================

        private void AdvanceStage(ActiveCrisis crisis, SimulationEngine e)
        {
            crisis.CurrentStage++;
            crisis.TurnsSinceStart = 0;
            crisis.NextStageTurn = crisis.StageDurationTurns;

            SimLogger.Log($"[Kriz] {crisis.Title} - Aşama {crisis.CurrentStage + 1}/{crisis.TotalStages}: {crisis.StageDescription}");
        }

        private void ApplyTurnPenalty(ActiveCrisis crisis, SimulationEngine e, float multiplier = 1.0f)
{
    float penaltyMult = (crisis.Severity / 50f) * multiplier;
    e.Legitimacy.AdjustLegitimacy(-1f * penaltyMult);
    e.Universe.Unrest = Math.Clamp(e.Universe.Unrest + 2f * penaltyMult, 0f, 100f);

    if (crisis.Type == CrisisType.Economic)
    {
        var gdp = e.Registry.Get(ObjectRegistry.Ids.Gdp);
        if (gdp != null) gdp.ActualValue = Math.Max(0f, gdp.ActualValue - 0.3f * penaltyMult);
    }
}

        private void AutoResolveCrisis(ActiveCrisis crisis, SimulationEngine e)
        {
            // Araştırma: %150 ceza ile otomatik çözülür
            float basePenalty = 5f * (crisis.Severity / 50f);
            float autoPenalty = basePenalty * AutoResolvePenaltyMultiplier;

            e.Legitimacy.AdjustLegitimacy(-autoPenalty);
            e.Universe.Unrest = Math.Clamp(e.Universe.Unrest + autoPenalty * 0.5f, 0f, 100f);
            e.PoliticalCapital = Math.Max(0f, e.PoliticalCapital - autoPenalty);

            SimLogger.Log($"[Kriz] {crisis.Title} otomatik çözüldü (ceza: -{autoPenalty:F1}).", SimLogger.LogLevel.Warning);
            TotalCrisesResolved++;
        }

        // ============================================================
        // KRİZ TETİKLEME
        // ============================================================

        private void TryTriggerNewCrisis(SimulationEngine e, float globalTension)
        {
            // Araştırma: Global Leaders krizleri oyuncunun zayıf göstergelerine ağırlıklandırır
            var candidates = new List<(CrisisType type, float weight)>();

            // Ekonomik kriz adayları
            float inflation = (float)e.Economy.Inflation;
            if (inflation > 15f) candidates.Add((CrisisType.Economic, inflation / 20f));
            if (e.Economy.NationalDebt > 2000f) candidates.Add((CrisisType.Debt, e.Economy.NationalDebt / 3000f));

            // Sosyal kriz adayları
            if (e.Universe.Unrest > 40f) candidates.Add((CrisisType.Social, e.Universe.Unrest / 60f));

            // Dış politika
            if (globalTension > 40f) candidates.Add((CrisisType.Diplomatic, globalTension / 60f));

            // Doğal afet (her zaman düşük olasılık)
            candidates.Add((CrisisType.NaturalDisaster, 0.3f));

            // Askeri
            if (e.Army.ArmySatisfaction < 40f) candidates.Add((CrisisType.Military, (40f - e.Army.ArmySatisfaction) / 40f));

            if (candidates.Count == 0) return;

            // Ağırlıklı rastgele seçim
            float totalWeight = candidates.Sum(c => c.weight);
            float roll = (float)(SimRng.NextDouble() * totalWeight);
            float cumulative = 0f;
            CrisisType selected = CrisisType.Economic;

            foreach (var c in candidates)
            {
                cumulative += c.weight;
                if (roll <= cumulative) { selected = c.type; break; }
            }

            TriggerCrisis(selected, e);
        }

       public void TriggerCrisis(CrisisType type, SimulationEngine e)
{
    // Zaten aktif aynı tipte kriz varsa atla
    if (ActiveCrises.Any(c => c.Type == type)) return;

    var crisis = CreateCrisis(type, e);
    if (crisis == null) return;

    ActiveCrises.Add(crisis);
    TotalCrisesTriggered++;
    SimLogger.Log($"[Kriz] YENİ KRİZ: {crisis.Title} (Şiddet: {crisis.Severity:F0})", SimLogger.LogLevel.Warning);

    // EK-6: Motor UI'ı tanımaz. EventBus üzerinden yayınla; UI katmanı dinler.
    EventBus.Publish(new CrisisTriggeredEvent
    {
        CrisisId = crisis.Id,
        Title = crisis.Title,
        Severity = crisis.Severity
    });
}

        private ActiveCrisis CreateCrisis(CrisisType type, SimulationEngine e)
        {
            var crisis = new ActiveCrisis
            {
                Id = Guid.NewGuid().ToString().Substring(0, 8),
                Type = type,
                CurrentStage = 0,
                TotalStages = 3,
                TurnsSinceStart = 0,
                NextStageTurn = 2,
                StageDurationTurns = 2,
                Severity = 30f + (float)(SimRng.NextDouble() * 40f)
            };

            switch (type)
            {
                case CrisisType.Economic:
                    crisis.Title = "EKONOMİK BUHRAN";
                    crisis.StageDescription = "Piyasalar çöküyor, yatırımcılar panikte.";
                    break;
                case CrisisType.Debt:
                    crisis.Title = "BORÇ KRİZİ";
                    crisis.StageDescription = "Devlet borçları ödenemiyor, kredi notu düşüyor.";
                    break;
                case CrisisType.Social:
                    crisis.Title = "SOSYAL HUZURSUZLUK";
                    crisis.StageDescription = "Sokaklar karışıyor, halk öfkeli.";
                    break;
                case CrisisType.Diplomatic:
                    crisis.Title = "DİPLOMATİK GERGİNLİK";
                    crisis.StageDescription = "Uluslararası ilişkiler kopma noktasında.";
                    break;
                case CrisisType.NaturalDisaster:
                    crisis.Title = "DOĞAL AFET";
                    crisis.StageDescription = "Büyük bir doğal afet ülkeyi vurdu.";
                    break;
                case CrisisType.Military:
                    crisis.Title = "ASKERİ HUZURSUZLUK";
                    crisis.StageDescription = "Ordu içinde hoşnutsuzluk artıyor, darbe söylentileri var.";
                    break;
            }

            return crisis;
        }

        // ============================================================
        // SEÇİM UYGULAMA
        // ============================================================

                // ============================================================
        // FAZ 3: KRİZ SEÇİM SİSTEMİ (aşama bazlı)
        // ============================================================

        /// <summary>Bir krizin mevcut aşamasındaki seçenekleri döndürür.</summary>
        public List<CrisisChoice> GetCurrentChoices(ActiveCrisis crisis)
        {
            if (crisis == null) return new List<CrisisChoice>();

            var choices = new List<CrisisChoice>();
            int stage = Math.Min(crisis.CurrentStage, 2);   // 0, 1, 2

            switch (crisis.Type)
            {
                case CrisisType.Economic:
                    if (stage == 0)
                    {
                        choices.Add(new CrisisChoice("Faiz oranlarını artır",
                            "Enflasyonu düşürür ama ekonomiyi yavaşlatır.",
                            e => { e.Economy.AdjustInflation(-8f); e.Legitimacy.AdjustLegitimacy(-4f);
                                   e.Universe.Unrest = Math.Clamp(e.Universe.Unrest + 6f, 0f, 100f); }));
                        choices.Add(new CrisisChoice("Para bas, halka dağıt",
                            "Kısa vadede rahatlatır ama enflasyonu tetikler.",
                            e => { e.Economy.AdjustInflation(+12f); e.Legitimacy.AdjustLegitimacy(+5f);
                                   e.Universe.Unrest = Math.Clamp(e.Universe.Unrest - 8f, 0f, 100f); }));
                    }
                    else if (stage == 1)
                    {
                        choices.Add(new CrisisChoice("Kemer sıkma politikası",
                            "Uzun vadede iyileşme, kısa vadede acı.",
                            e => { e.Economy.AdjustDebt(-300f); e.Legitimacy.AdjustLegitimacy(-6f);
                                   e.Universe.Unrest = Math.Clamp(e.Universe.Unrest + 10f, 0f, 100f); }));
                        choices.Add(new CrisisChoice("Uluslararası kredi al",
                            "IMF benzeri kredi — faiz yüksek.",
                            e => { e.Economy.AdjustDebt(+400f); e.PoliticalCapital += 30f;
                                   e.Legitimacy.AdjustLegitimacy(-3f); }));
                    }
                    else
                    {
                        choices.Add(new CrisisChoice("Reform paketi açıkla",
                            "Uzun vadeli yapısal reform.",
                            e => { e.Economy.AdjustDebt(-150f); e.Economy.AdjustInflation(-5f);
                                   e.Legitimacy.AdjustLegitimacy(+8f); }));
                        choices.Add(new CrisisChoice("Durumu kabul et",
                            "Kriz kendi halinde geçsin.",
                            e => { e.Legitimacy.AdjustLegitimacy(-5f); }));
                    }
                    break;

                case CrisisType.Debt:
                    if (stage == 0)
                    {
                        choices.Add(new CrisisChoice("Borç yapılandırma",
                            "Vadeleri uzat, faiz yükünü azalt.",
                            e => { e.Economy.AdjustDebt(-200f); e.Legitimacy.AdjustLegitimacy(-4f); }));
                        choices.Add(new CrisisChoice("Yeni tahvil ihraç et",
                            "Kısa vadede para bul, ama borç büyür.",
                            e => { e.Economy.AdjustDebt(+200f); e.PoliticalCapital += 20f; }));
                    }
                    else
                    {
                        choices.Add(new CrisisChoice("Kamu harcamalarını kıs",
                            "Acı reçete ama gerekli.",
                            e => { e.Economy.AdjustDebt(-400f); e.Universe.Unrest = Math.Clamp(e.Universe.Unrest + 12f, 0f, 100f); }));
                        choices.Add(new CrisisChoice("Vergileri artır",
                            "Halkı sık, hazineyi doldur.",
                            e => { e.Economy.AdjustDebt(-250f); e.Legitimacy.AdjustLegitimacy(-8f); }));
                    }
                    break;

                case CrisisType.Social:
                    if (stage == 0)
                    {
                        choices.Add(new CrisisChoice("Taviz ver, talepleri dinle",
                            "Halk sakinleşir ama bütçe sarsılır.",
                            e => { e.PoliticalCapital = Math.Max(0f, e.PoliticalCapital - 20f);
                                   e.Universe.Unrest = Math.Clamp(e.Universe.Unrest - 20f, 0f, 100f); }));
                        choices.Add(new CrisisChoice("Polisi gönder",
                            "Sert müdahale, kısa vadede etkili.",
                            e => { e.Legitimacy.AdjustLegitimacy(-10f);
                                   e.Universe.Unrest = Math.Clamp(e.Universe.Unrest - 30f, 0f, 100f);
                                   e.Army.LoadState(e.Army.ArmySatisfaction, e.Army.MilitaryStrength, e.Army.LoyaltyToLeader); }));
                    }
                    else
                    {
                        choices.Add(new CrisisChoice("Reform vaat et",
                            "Halk bekleyecek, sabır tükenebilir.",
                            e => { e.Legitimacy.AdjustLegitimacy(+5f);
                                   e.Universe.Unrest = Math.Clamp(e.Universe.Unrest - 10f, 0f, 100f); }));
                        choices.Add(new CrisisChoice("Görmezden gel",
                            "Öfke birikir, patlama riski.",
                            e => { e.Universe.Unrest = Math.Clamp(e.Universe.Unrest + 15f, 0f, 100f);
                                   e.Legitimacy.AdjustLegitimacy(-6f); }));
                    }
                    break;

                case CrisisType.Diplomatic:
                    choices.Add(new CrisisChoice("Diplomatik kanalları kullan",
                        "Yumuşak güç, uzun vadede etkili.",
                        e => { e.Legitimacy.AdjustLegitimacy(+5f); }));
                    choices.Add(new CrisisChoice("Askeri güç göster",
                        "Caydırıcılık sağlar ama tırmandırır.",
                        e => { e.Army.LoadState(Math.Min(100f, e.Army.ArmySatisfaction + 8f),
                                                e.Army.MilitaryStrength, e.Army.LoyaltyToLeader);
                               e.Legitimacy.AdjustLegitimacy(-3f); }));
                    break;

                case CrisisType.NaturalDisaster:
                    choices.Add(new CrisisChoice("Acil yardım paketi",
                        "Halk devletin yanında olduğunu görür.",
                        e => { e.Legitimacy.AdjustLegitimacy(+12f); e.PoliticalCapital = Math.Max(0f, e.PoliticalCapital - 25f); }));
                    choices.Add(new CrisisChoice("Sınırlı müdahale",
                        "Bütçe korunur ama halk öfkelenir.",
                        e => { e.Legitimacy.AdjustLegitimacy(-8f);
                               e.Universe.Unrest = Math.Clamp(e.Universe.Unrest + 10f, 0f, 100f); }));
                    break;

                case CrisisType.Military:
                    choices.Add(new CrisisChoice("Ordu bütçesini artır",
                        "Generaller memnun olur, darbe riski düşer.",
                        e => { e.Army.LoadState(Math.Min(100f, e.Army.ArmySatisfaction + 15f),
                                                e.Army.MilitaryStrength, e.Army.LoyaltyToLeader);
                               e.Economy.AdjustDebt(+200f); }));
                    choices.Add(new CrisisChoice("Orduya mesaj ver",
                        "Sivil otoriteyi vurgula.",
                        e => { e.Legitimacy.AdjustLegitimacy(+3f);
                               e.Army.LoadState(Math.Max(0f, e.Army.ArmySatisfaction - 10f),
                                                e.Army.MilitaryStrength, e.Army.LoyaltyToLeader); }));
                    break;
            }

            return choices;
        }

        /// <summary>Oyuncu seçimini uygula ve aşamayı ilerlet.</summary>
        public void ApplyChoice(string crisisId, CrisisChoice choice, SimulationEngine e)
        {
            var crisis = ActiveCrises.FirstOrDefault(c => c.Id == crisisId);
            if (crisis == null || choice == null) return;

            // Etkiyi uygula
            choice.Effect?.Invoke(e);
            SimLogger.Log($"[Kriz] {crisis.Title} - Seçim: {choice.Label}");

            // Aşamayı ilerlet
            crisis.CurrentStage++;
            crisis.TurnsSinceStart = 0;

            if (crisis.CurrentStage >= crisis.TotalStages)
            {
                // Kriz bitti
                ActiveCrises.Remove(crisis);
                TotalCrisesResolved++;
                SimLogger.Log($"[Kriz] {crisis.Title} tamamlandı (oyuncu kararıyla).");
            }
        }

        // ============================================================
        // RAPORLAMA
        // ============================================================

        public List<string> GetReport()
        {
            var lines = new List<string>();
            if (ActiveCrises.Count == 0)
            {
                lines.Add("Aktif kriz yok. Durum kontrol altında.");
                return lines;
            }

            lines.Add($"AKTİF KRİZLER ({ActiveCrises.Count}/{MaxActiveCrises}):");
            foreach (var c in ActiveCrises)
            {
                lines.Add($"• {c.Title} - Aşama {c.CurrentStage + 1}/{c.TotalStages} " +
                          $"(Şiddet: %{c.Severity:F0}, {CrisisAutoResolveTurns - c.TurnsSinceStart} tur kaldı)");
            }
            return lines;
        }
    }

    // ============================================================
    // YARDIMCI SINIFLAR
    // ============================================================

    public enum CrisisType
    {
        Economic,
        Debt,
        Social,
        Diplomatic,
        NaturalDisaster,
        Military
    }

    [Serializable]
    public class ActiveCrisis
    {
        public string Id { get; set; }
        public CrisisType Type { get; set; }
        public string Title { get; set; }
        public string StageDescription { get; set; }
        public int CurrentStage { get; set; }
        public int TotalStages { get; set; } = 3;
        public int TurnsSinceStart { get; set; }
        public int NextStageTurn { get; set; } = 2;
        public int StageDurationTurns { get; set; } = 2;
        public float Severity { get; set; } = 50f;
    }
        /// <summary>Kriz seçeneği — oyuncuya sunulan kararlar.</summary>
    [Serializable]
    public class CrisisChoice
    {
        public string Label { get; set; }
        public string Description { get; set; }
        public Action<SimulationEngine> Effect { get; set; }

        public CrisisChoice() { }

        public CrisisChoice(string label, string description, Action<SimulationEngine> effect)
        {
            Label = label;
            Description = description;
            Effect = effect;
        }
    }
}