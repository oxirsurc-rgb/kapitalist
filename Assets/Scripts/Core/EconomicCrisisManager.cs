using System;
using System.Linq;

namespace DemocracySim.Engine.Core
{
    /// <summary>
    /// FAZ 9: Ekonomi kriz mekanikleri.
    /// Borç sarmalı, IMF yardımı, acil vergi, yapısal reform.
    /// </summary>
    public static class EconomicCrisisManager
    {
        /// <summary>
        /// Ekonominin "sağlık durumu" — 0.0 (batmış) ile 1.0 (mükemmel) arası.
        /// Kredi notu, borç oranı, enflasyonu birleştirir.
        /// </summary>
        public static float GetEconomicHealth(SimulationEngine e)
        {
            if (e?.Economy == null) return 1f;
            var cfg = BalanceConfig.Instance;

            // 1) Kredi notu (0-100 → 0-1)
            float creditScore = e.Economy.CreditRating / 100f;

            // 2) Borç oranı (2000 milyar referans)
            float debtRatio = 1f - Math.Min(1f, e.Economy.NationalDebt / 2500f);
            debtRatio = Math.Max(0f, debtRatio);

            // 3) Enflasyon (0-30% arası iyi, 30+ kötü)
            float inflationScore = 1f - Math.Min(1f, e.Economy.Inflation / 40f);
            inflationScore = Math.Max(0f, inflationScore);

            // Ağırlıklı ortalama
            return creditScore * 0.4f + debtRatio * 0.4f + inflationScore * 0.2f;
        }

        /// <summary>Ekonomi durumu etiketi.</summary>
        public static string GetHealthLabel(float health)
        {
            if (health >= 0.75f) return "MÜKEMMEL";
            if (health >= 0.55f) return "SAĞLIKLI";
            if (health >= 0.40f) return "ORTA";
            if (health >= 0.25f) return "ZAYIF";
            if (health >= 0.15f) return "KRİTİK";
            return "ÇÖKÜŞ EŞİĞİNDE";
        }

        /// <summary>IMF benzeri yardım yapılabilir mi?</summary>
        public static string CanGetImfBailout(SimulationEngine e)
        {
            if (e?.Economy == null) return "Ekonomi verisi yok.";
            var cfg = BalanceConfig.Instance;

            // Kritik seviye kontrolü
            float health = GetEconomicHealth(e);
            if (health > cfg.CrisisCriticalThreshold)
                return $"Ekonomi henüz IMF yardımı için yeterince kötü değil (Sağlık: {GetHealthLabel(health)}).";

            // Kredi notu kontrolü
            if (e.Economy.CreditRating > 40f)
                return $"Kredi notu hâlâ çok yüksek ({(int)e.Economy.CreditRating}/100). IMF başvurusu için <40 gerekli.";

            // Cooldown kontrolü
            if (e.Universe.ImfBailoutUsedTurn > 0 &&
                e.CurrentTurn - e.Universe.ImfBailoutUsedTurn < cfg.ImfBailoutCooldownTurns)
            {
                int remaining = cfg.ImfBailoutCooldownTurns - (e.CurrentTurn - e.Universe.ImfBailoutUsedTurn);
                return $"IMF ile son anlaşmanız taze — {remaining} tur beklemelisiniz.";
            }

            return null;   // Yapılabilir
        }

        /// <summary>IMF yardımı yap.</summary>
        public static string GetImfBailout(SimulationEngine e)
        {
            string blocker = CanGetImfBailout(e);
            if (blocker != null) return blocker;

            var cfg = BalanceConfig.Instance;

            // Uygula
            e.Economy.AdjustDebt(-cfg.ImfBailoutAmount);
            e.Legitimacy.AdjustLegitimacy(-cfg.ImfBailoutConditionLegitCost);
            e.Army.LoadState(
                Math.Max(0f, e.Army.ArmySatisfaction - cfg.ImfBailoutConditionArmyCost),
                e.Army.MilitaryStrength,
                e.Army.LoyaltyToLeader);

            // Kayıt
            e.Universe.ImfBailoutUsedTurn = e.CurrentTurn;

            // Haber
            e.Universe.Messages.Add(new UniverseMessage
            {
                Text = $"💰 IMF YARDIMI: {cfg.ImfBailoutAmount:F0} milyar borç silindi. " +
                       $"Ancak kemer sıkma koşulları kabul edildi: Meşruiyet -{cfg.ImfBailoutConditionLegitCost:F0}, " +
                       $"Ordu -{cfg.ImfBailoutConditionArmyCost:F0}. 15 tur boyunca yeni yardım yok.",
                IsWarning = false
            });

            return $"IMF anlaşması imzalandı! +{cfg.ImfBailoutAmount:F0} milyar borç affı, " +
                   $"ama meşruiyet ve ordu desteği zedelendi.";
        }

        /// <summary>Acil vergi paketi yapılabilir mi?</summary>
        public static string CanUseEmergencyTax(SimulationEngine e)
        {
            if (e?.Economy == null) return "Ekonomi verisi yok.";
            var cfg = BalanceConfig.Instance;

            if (e.Universe.EmergencyTaxUsedTurn > 0 &&
                e.CurrentTurn - e.Universe.EmergencyTaxUsedTurn < cfg.EmergencyTaxCooldown)
            {
                int remaining = cfg.EmergencyTaxCooldown - (e.CurrentTurn - e.Universe.EmergencyTaxUsedTurn);
                return $"Acil vergi son kullanımdan taze — {remaining} tur beklemelisiniz.";
            }

            return null;
        }

        /// <summary>Acil vergi paketini uygula.</summary>
        public static string ApplyEmergencyTax(SimulationEngine e)
        {
            string blocker = CanUseEmergencyTax(e);
            if (blocker != null) return blocker;

            var cfg = BalanceConfig.Instance;

            // Anlık nakit
            e.PoliticalCapital = Math.Min(e.MaxPoliticalCapital, e.PoliticalCapital + cfg.EmergencyTaxCapital);
            e.Economy.AdjustDebt(-80f);   // Küçük borç azaltma

            // Bedeller
            e.Legitimacy.AdjustLegitimacy(-cfg.EmergencyTaxLegitCost);
            e.Universe.Unrest = Math.Clamp(e.Universe.Unrest + cfg.EmergencyTaxUnrest, 0f, 100f);

            // Çalışan grupları özellikle etkiler
            foreach (var g in e.Demographics)
                g.AdjustSatisfaction(-3f);

            e.Universe.EmergencyTaxUsedTurn = e.CurrentTurn;

            e.Universe.Messages.Add(new UniverseMessage
            {
                Text = $"💸 ACİL VERGİ PAKETİ: Hazine {cfg.EmergencyTaxCapital:F0} sermaye kazandı, borç 80 azaldı. " +
                       $"Ancak halk öfkeli: Meşruiyet -{cfg.EmergencyTaxLegitCost:F0}, " +
                       $"Huzursuzluk +{cfg.EmergencyTaxUnrest:F0}.",
                IsWarning = true
            });

            return $"Acil vergi paketi kabul edildi. +{cfg.EmergencyTaxCapital:F0} sermaye, " +
                   $"ama halk sokağa dökülebilir.";
        }

        /// <summary>Yapısal reform paketi yapılabilir mi?</summary>
        public static string CanUseStructuralReform(SimulationEngine e)
        {
            if (e?.Economy == null) return "Ekonomi verisi yok.";
            var cfg = BalanceConfig.Instance;

            if (e.Universe.StructuralReformTurnsLeft > 0)
                return $"Yapısal reform zaten yürürlükte — {e.Universe.StructuralReformTurnsLeft} tur kaldı.";

            if (e.PoliticalCapital < cfg.StructuralReformCost)
                return $"Yeterli sermaye yok ({cfg.StructuralReformCost:F0} gerekir).";

            return null;
        }

        /// <summary>Yapısal reform paketini uygula.</summary>
        public static string ApplyStructuralReform(SimulationEngine e)
        {
            string blocker = CanUseStructuralReform(e);
            if (blocker != null) return blocker;

            var cfg = BalanceConfig.Instance;
            e.PoliticalCapital -= cfg.StructuralReformCost;

            // Anlık: borç azalt
            e.Economy.AdjustDebt(-cfg.StructuralReformDebtReduction);

            // Kısa vadeli acı: huzursuzluk
            e.Universe.Unrest = Math.Clamp(e.Universe.Unrest + cfg.StructuralReformUnrestAdd, 0f, 100f);

            // Uzun vadeli reform süreci
            e.Universe.StructuralReformTurnsLeft = cfg.StructuralReformDuration;

            e.Universe.Messages.Add(new UniverseMessage
            {
                Text = $"📋 YAPISAL REFORM PAKETİ: {cfg.StructuralReformDebtReduction:F0} milyar borç silindi. " +
                       $"{cfg.StructuralReformDuration} tur boyunca her tur +3 meşruiyet kazanılacak. " +
                       $"Ancak halk kısa vadede sıkıntı çekecek: Huzursuzluk +{cfg.StructuralReformUnrestAdd:F0}.",
                IsWarning = false
            });

            return $"Yapısal reform paketi kabul edildi. {cfg.StructuralReformDuration} tur boyunca meşruiyet toparlanacak.";
        }

        /// <summary>Her tur çağrılır — reform süreci ilerlet.</summary>
        public static void ProcessTurn(SimulationEngine e)
        {
            if (e?.Universe == null) return;

            // Yapısal reform süreci
            if (e.Universe.StructuralReformTurnsLeft > 0)
            {
                e.Universe.StructuralReformTurnsLeft--;
                var cfg = BalanceConfig.Instance;
                e.Legitimacy.AdjustLegitimacy(cfg.StructuralReformLegitGain / cfg.StructuralReformDuration);

                if (e.Universe.StructuralReformTurnsLeft == 0)
                {
                    e.Universe.Messages.Add(new UniverseMessage
                    {
                        Text = "✅ Yapısal reform tamamlandı! Ekonomi toparlanıyor, halk reformu sahiplendi.",
                        IsWarning = false
                    });
                }
            }

            // Kriz uyarı seviyesi
            float health = GetEconomicHealth(e);
            var cfg2 = BalanceConfig.Instance;
            if (health <= cfg2.CrisisCriticalThreshold)
            {
                e.Universe.EconomicWarningLevel = 2;   // Kritik
            }
            else if (health <= cfg2.CrisisWarningThreshold)
            {
                e.Universe.EconomicWarningLevel = 1;   // Uyarı
            }
            else
            {
                e.Universe.EconomicWarningLevel = 0;   // Normal
            }
        }
    }
}