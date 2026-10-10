using System;
using System.Collections.Generic;
using System.Linq;

namespace DemocracySim.Engine.Core
{
    public enum CaseType
    {
        Constitutional,   // Anayasa ihlali — yasayı iptal ettir
        Corruption,       // Yolsuzluk — bakanı yargılat
        Press             // Basın — gazeteciyi koru/sustur
    }

    public enum JudgeIdeology
    {
        Progressive,   // Sol eğilimli
        Conservative,  // Sağ eğilimli
        Neutral        // Merkez
    }

    [Serializable]
    public class SupremeCourtJudge
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public JudgeIdeology Ideology { get; set; }
        public float Independence { get; set; } = 70f;   // 0-100
        public int AppointedTurn { get; set; }

        public SupremeCourtJudge(string id, string name, JudgeIdeology ideology)
        {
            Id = id; Name = name; Ideology = ideology;
        }
    }

    /// <summary>FAZ 3 Adım 5: Yargı sistemi.</summary>
    public class JudicialSystem
    {
        // Yüksek mahkeme yargıçları
        public List<SupremeCourtJudge> Judges { get; set; } = new List<SupremeCourtJudge>();

        // Yargı bağımsızlığı (0-100) — genel gösterge
        public float JudicialIndependence { get; set; } = 60f;

        // Aktif dava
        public class ActiveCase
        {
            public CaseType Type { get; set; }
            public string TargetId { get; set; }        // Yasa veya aktör ID
            public int TurnsLeft { get; set; }          // Kaç tur sürer
            public float SuccessChance { get; set; }    // 0-1
        }

        public ActiveCase CurrentCase { get; private set; }
        public int CaseCooldown { get; set; } = 0;


        // Sabitler
        public const float CaseFilingCost = 30f;      // Dava açma maliyeti (sermaye)
        public const int CaseDuration = 3;            // Dava süresi (tur)
        public const int CaseCooldownTurns = 5;

        public JudicialSystem()
        {
            // Varsayılan 5 yargıç
            Judges.Add(new SupremeCourtJudge("judge_1", "Basri Bey", JudgeIdeology.Progressive));
            Judges.Add(new SupremeCourtJudge("judge_2", "Hilmi Hanım", JudgeIdeology.Conservative));
            Judges.Add(new SupremeCourtJudge("judge_3", "Prof. Cem Bey", JudgeIdeology.Neutral));
            Judges.Add(new SupremeCourtJudge("judge_4", "Dr. Selin Hanım", JudgeIdeology.Progressive));
            Judges.Add(new SupremeCourtJudge("judge_5", "Yargıç Mert Bey", JudgeIdeology.Conservative));
        }

        public void ProcessTurn(SimulationEngine e)
        {
            if (CaseCooldown > 0) CaseCooldown--;

            // Yargı bağımsızlığı derin devlet ve yolsuzluk ile değişir
            float targetIndependence = 60f 
                - e.CorruptionLevel * 0.5f 
                + e.DeepStateStability * 0.1f;
            targetIndependence = Math.Clamp(targetIndependence, 0f, 100f);
            JudicialIndependence += (targetIndependence - JudicialIndependence) * 0.05f;

            // Aktif dava ilerlet
            if (CurrentCase != null)
            {
                CurrentCase.TurnsLeft--;
                if (CurrentCase.TurnsLeft <= 0)
                {
                    ResolveCase(e);
                }
            }
        }

        public bool CanFileCase(SimulationEngine e)
        {
            if (e.CurrentRole != SimulationEngine.PlayerRole.Opposition) return false;
            if (CurrentCase != null) return false;
            if (CaseCooldown > 0) return false;
            if (e.PoliticalCapital < CaseFilingCost) return false;
            return true;
        }

        public string GetBlockReason(SimulationEngine e)
        {
            if (e.CurrentRole != SimulationEngine.PlayerRole.Opposition)
                return "Dava sadece muhalefetteyken acilabilir.";
            if (CurrentCase != null)
                return $"Zaten devam eden bir dava var ({CurrentCase.TurnsLeft} tur kaldi).";
            if (CaseCooldown > 0)
                return $"Yeni dava icin {CaseCooldown} tur beklemelisin.";
            if (e.PoliticalCapital < CaseFilingCost)
                return $"Yeterli sermaye yok ({CaseFilingCost:F0} gerekir).";
            return "";
        }

        public string FileCase(CaseType type, string targetId, SimulationEngine e)
        {
            if (!CanFileCase(e)) return GetBlockReason(e);

            e.PoliticalCapital -= CaseFilingCost;

            // Başarı şansı hesapla
            float chance = CalculateSuccessChance(type, e);

            CurrentCase = new ActiveCase
            {
                Type = type,
                TargetId = targetId,
                TurnsLeft = CaseDuration,
                SuccessChance = chance
            };

            string typeStr = CaseTypeTurkish(type);
            return $"{typeStr} davasi acildi. Basari sansi: %{chance * 100:F0}. {CaseDuration} tur surecek.";
        }

       private float CalculateSuccessChance(CaseType type, SimulationEngine e)
{
    float baseChance = type switch
    {
        CaseType.Constitutional => 0.45f,
        CaseType.Corruption      => 0.35f,
        CaseType.Press           => 0.50f,
        _ => 0.40f
    };

    // Genel yargı bağımsızlığı bonusu
    float independenceBonus = (JudicialIndependence - 50f) * 0.005f;

    // EK-16: Bireysel yargıç bağımsızlığı — ortalama bağımsızlığı %70 üstü ise bonus
    float avgJudgeIndependence = 70f;
    if (Judges != null && Judges.Count > 0)
        avgJudgeIndependence = 0f;
    if (Judges != null && Judges.Count > 0)
    {
        float sum = 0f;
        for (int i = 0; i < Judges.Count; i++) sum += Judges[i].Independence;
        avgJudgeIndependence = sum / Judges.Count;
    }
    float judgeBonus = (avgJudgeIndependence - 70f) * 0.003f;   // -0.21 .. +0.09

    float ideologyBonus = CalculateJudgeAlignment(type, e);

    float total = baseChance + independenceBonus + judgeBonus + ideologyBonus;
    return Math.Clamp(total, 0.05f, 0.95f);
}

        private float CalculateJudgeAlignment(CaseType type, SimulationEngine e)
        {
            // Oyuncunun ideolojisi ile yargıçların ideolojisi ne kadar uyumlu?
            float playerAlign = e.PlayerGlobalAlignment;
            float matchCount = 0f;

            foreach (var j in Judges)
            {
                bool matches = false;
                if (playerAlign < -20f && j.Ideology == JudgeIdeology.Progressive) matches = true;
                else if (playerAlign > 20f && j.Ideology == JudgeIdeology.Conservative) matches = true;
                else if (j.Ideology == JudgeIdeology.Neutral) matches = true;

                if (matches) matchCount++;
            }

            // 5 yargıçtan kaçı eşleşiyor → bonus
            return (matchCount / Judges.Count) * 0.2f - 0.1f;   // -0.1 .. +0.1
        }

        private void ResolveCase(SimulationEngine e)
        {
            if (CurrentCase == null) return;

            bool success = SimRng.NextDouble() < CurrentCase.SuccessChance;
            var type = CurrentCase.Type;
            var targetId = CurrentCase.TargetId;
            CurrentCase = null;
            CaseCooldown = CaseCooldownTurns;

            if (success)
            {
                ApplySuccessEffect(e, type, targetId);
            }
            else
            {
                ApplyFailureEffect(e, type);
            }
        }

        private void ApplySuccessEffect(SimulationEngine e, CaseType type, string targetId)
        {
            switch (type)
            {
                case CaseType.Constitutional:
                    // Yasayı iptal et
                    var policy = e.AllObjects.OfType<SimPolicy>().FirstOrDefault(p => p.Id == targetId);
                    if (policy != null)
                    {
                        policy.IsActive = false;
                        policy.Intensity = 0f;
                        e.Legitimacy.AdjustLegitimacy(8f);
                        SimLogger.Log($"[Yargi] ANAYASA MAHKEMESI: {policy.Name} yasasi iptal edildi! +8 mesruiyet.", SimLogger.LogLevel.Warning);
                    }
                    break;

                case CaseType.Corruption:
                    // Bakanı görevden al (Simülasyon: Influences azalt)
                    var actor = e.Actors.FirstOrDefault(a => a.Id == targetId);
                    if (actor != null)
                    {
                        actor.Influence = Math.Max(0f, actor.Influence - 30f);
                        actor.Loyalty = Math.Max(0f, actor.Loyalty - 30f);
                        e.Legitimacy.AdjustLegitimacy(10f);
                        e.CorruptionLevel = Math.Max(0f, e.CorruptionLevel - 5f);
                        SimLogger.Log($"[Yargi] YOLSUZLUK DAVASI: {actor.Name} suçlu bulundu! +10 mesruiyet, -5 yolsuzluk.", SimLogger.LogLevel.Warning);
                    }
                    break;

                case CaseType.Press:
                    // Basın özgürlüğü korundu
                    e.Legitimacy.AdjustLegitimacy(5f);
                    e.DeepStateStability = Math.Max(0f, e.DeepStateStability - 5f);
                    SimLogger.Log("[Yargi] BASIN DAVASI: Gazeteci beraat etti! +5 mesruiyet, derin devlet rahatsiz.", SimLogger.LogLevel.Info);
                    break;
            }
        }

        private void ApplyFailureEffect(SimulationEngine e, CaseType type)
        {
            // Dava kaybedildi — meşruiyet düşer, oyuncu sermaye kaybeder
            e.Legitimacy.AdjustLegitimacy(-5f);
            e.PoliticalCapital = Math.Max(0f, e.PoliticalCapital - 10f);
            string typeStr = CaseTypeTurkish(type);
            SimLogger.Log($"[Yargi] {typeStr} davasi kaybedildi! -5 mesruiyet, -10 sermaye.", SimLogger.LogLevel.Warning);
        }

        public static string CaseTypeTurkish(CaseType t)
        {
            switch (t)
            {
                case CaseType.Constitutional: return "Anayasa";
                case CaseType.Corruption:     return "Yolsuzluk";
                case CaseType.Press:          return "Basin";
                default: return t.ToString();
            }
        }

        public string GetStatusText()
        {
            if (CurrentCase != null)
                return $"Aktif dava: {CaseTypeTurkish(CurrentCase.Type)} — {CurrentCase.TurnsLeft} tur kaldi";
            if (CaseCooldown > 0)
                return $"Yargi cooldown: {CaseCooldown} tur";
            return "Yargi hazir";
        }

        public void RestoreCase(CaseType type, string targetId, int turnsLeft, float chance)
        {
            CurrentCase = new ActiveCase
            {
                Type = type,
                TargetId = targetId,
                TurnsLeft = turnsLeft,
                SuccessChance = chance
            };
        }
    }
}