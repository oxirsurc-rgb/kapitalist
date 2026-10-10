using System;
using System.Collections.Generic;
using System.Linq;
using DemocracySim.Engine.Core;

namespace DemocracySim.Engine.Core
{
    // --- YENİ: Kampanya aşamaları ---
    public enum CampaignStage
    {
        None,           // Kampanya yok
        Nomination,     // Adaylık
        Primary,        // Ön seçim
        General,        // Genel kampanya
        ElectionNight   // Seçim gecesi
    }

    public class CampaignManager
    {
        // ================================================================
        // MEVCUT MEKANİKLER (KORUNDU)
        // ================================================================

        /// <summary>Hangi gruba ne kadar kampanya yatırımı yapıldı?</summary>
        public Dictionary<string, float> CampaignInvestments { get; set; }
            = new Dictionary<string, float>();

        public bool IsCampaignActive { get; set; } = false;

        public void StartCampaign(List<DemographicGroup> groups)
        {
            IsCampaignActive = true;
            CampaignInvestments.Clear();
            if (groups != null)
            {
                foreach (var group in groups)
                {
                    CampaignInvestments[group.Id] = 0f;
                }
            }
        }

       /// <summary>
/// Belirli bir gruba kampanya yatırımı yap.
/// NOT: Maliyet GameManager tarafından düşülür — bu metod sadece yatırımı kaydeder.
/// </summary>
public void InvestInGroup(string groupId, float amount)
{
    IsCampaignActive = true;
    CampaignInvestments.TryGetValue(groupId, out float current);
    CampaignInvestments[groupId] = current + amount;
}

        /// <summary>Kampanya sonucunda ek oy oranı hesapla (azalan verim).</summary>
        public float CalculateCampaignBonus(DemographicGroup group)
        {
            if (group == null || !CampaignInvestments.ContainsKey(group.Id)) return 0f;

            float investment = CampaignInvestments[group.Id];
            // 15 sermaye ≈ +3, 100 sermaye ≈ +8, 400+ sermaye ≈ +20
            return Math.Min(20f, (float)Math.Sqrt(Math.Max(0f, investment)) * 0.8f);
        }

        public void EndCampaign()
        {
            IsCampaignActive = false;
        }

        /// <summary>Seçim bittiğinde tüm yatırımları sıfırlar.</summary>
        public void Reset()
        {
            IsCampaignActive = false;
            CampaignInvestments.Clear();
            CurrentStage = CampaignStage.None;
            StageProgress = 0;
            StageHistory.Clear();
        }

        // ================================================================
        // YENİ: KAMPANYA AŞAMA SİSTEMİ
        // ================================================================

        public CampaignStage CurrentStage { get; private set; } = CampaignStage.None;

        /// <summary>Her aşamada 3 tur ilerleme gerekir (toplam 12 tur).</summary>
        public int StageProgress { get; private set; } = 0;

        public const int TurnsPerStage = 3;

        /// <summary>Aşama geçmişi (raporlama için).</summary>
        public List<string> StageHistory { get; private set; } = new List<string>();

        /// <summary>Kampanyayı aşamalı olarak başlatır.</summary>
        public void StartStagedCampaign(List<DemographicGroup> groups)
        {
            StartCampaign(groups);
            CurrentStage = CampaignStage.Nomination;
            StageProgress = 0;
            StageHistory.Clear();
            StageHistory.Add($"[T{DateTime.Now:HH:mm}] Adaylık süreci başladı.");
        }

        /// <summary>
        /// Her turda çağrılır. Aşama ilerlemesini artırır; 3 tur sonra
        /// bir sonraki aşamaya geçer.
        /// </summary>
        public void AdvanceTurn()
        {
            if (CurrentStage == CampaignStage.None) return;
            if (CurrentStage == CampaignStage.ElectionNight) return;

            StageProgress++;
            if (StageProgress >= TurnsPerStage)
            {
                StageProgress = 0;
                CurrentStage = (CampaignStage)((int)CurrentStage + 1);
                StageHistory.Add($"[T{DateTime.Now:HH:mm}] Aşama geçildi: {GetStageName(CurrentStage)}");
            }
        }

        /// <summary>Aşama adını Türkçe döndürür.</summary>
        public string GetStageName(CampaignStage stage)
        {
            return stage switch
            {
                CampaignStage.Nomination    => "Adaylık",
                CampaignStage.Primary       => "Ön Seçim",
                CampaignStage.General       => "Genel Kampanya",
                CampaignStage.ElectionNight => "Seçim Gecesi",
                _                           => "Kampanya Yok"
            };
        }

        /// <summary>Aşama açıklamasını döndürür (UI için).</summary>
        public string GetStageDescription()
        {
            return CurrentStage switch
            {
                CampaignStage.Nomination    => "Adaylık: Parti içi destek topluyorsunuz. Her miting parti içi fraksiyonları etkiler.",
                CampaignStage.Primary       => "Ön Seçim: Parti üyeleri sizi değerlendiriyor. Fraksiyon desteği kritik.",
                CampaignStage.General       => "Genel Kampanya: Halkın karşısına çıkıyorsunuz. Yatırımlar oy oranına yansıyor.",
                CampaignStage.ElectionNight => "Seçim Gecesi: Sonuçlar açıklanıyor. Artık kampanya yatırımı yapılamaz.",
                _                           => "Aktif kampanya yok."
            };
        }

        /// <summary>Bu aşamada yatırım yapılabilir mi?</summary>
        public bool CanInvest()
        {
            return CurrentStage != CampaignStage.None
                && CurrentStage != CampaignStage.ElectionNight;
        }

        /// <summary>Aşama çarpanı: Aşama ilerledikçe yatırım daha etkili olur.</summary>
        public float GetStageMultiplier()
        {
            return CurrentStage switch
            {
                CampaignStage.Nomination    => 0.7f,
                CampaignStage.Primary       => 1.0f,
                CampaignStage.General       => 1.4f,
                CampaignStage.ElectionNight => 1.0f,
                _                           => 1.0f
            };
        }

        /// <summary>Kampanya raporu (UI için).</summary>
        public List<string> GetReport()
        {
            var lines = new List<string>
            {
                $"Aşama: {GetStageName(CurrentStage)} ({StageProgress}/{TurnsPerStage})",
                GetStageDescription()
            };
            if (CampaignInvestments.Count > 0)
            {
                lines.Add("Yatırımlar:");
                foreach (var kv in CampaignInvestments.OrderByDescending(x => x.Value))
                    lines.Add($"  {kv.Key}: {kv.Value:F0}");
            }
            return lines;
        }
    }
}