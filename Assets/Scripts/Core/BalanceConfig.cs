using UnityEngine;

namespace DemocracySim.Engine.Core
{
    /// <summary>
    /// Tüm denge sabitleri tek yerde. Kodda sihirli sayı bırakmayın.
    /// 
    /// KULLANIM:
    /// 1. Unity Editor'de: Assets > Create > Kapitalist > Balance Config
    /// 2. Oluşan dosyayı Assets/Resources/BalanceConfig.asset olarak kaydedin
    /// 3. Kodda: BalanceConfig.Instance.CustomPolicyCost
    /// 
    /// Eğer Resources'da dosya yoksa, kod varsayılan değerlerle çalışır.
    /// </summary>
    [CreateAssetMenu(fileName = "BalanceConfig", menuName = "Kapitalist/Balance Config")]
    public class BalanceConfig : ScriptableObject
    {
        private static BalanceConfig _instance;

        public static BalanceConfig Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<BalanceConfig>("BalanceConfig");
                    if (_instance == null)
                    {
                        // Fallback: varsayılan değerlerle geçici instance
                        _instance = CreateInstance<BalanceConfig>();
                        Debug.LogWarning("[BalanceConfig] Resources/BalanceConfig.asset bulunamadı. " +
                                         "Varsayılan değerler kullanılıyor. " +
                                         "Editor'de: Assets > Create > Kapitalist > Balance Config");
                    }
                }
                return _instance;
            }
        }

        // ============================================================
        // SİYASİ SERMAYE
        // ============================================================
        [Header("Siyasi Sermaye")]
        public float CapitalGainGoverning = 10f;
        public float CapitalGainOpposition = 5f;
        public float MaxPoliticalCapital = 200f;
        public float LobbyCapitalThreshold = 10f;

        // ============================================================
        // OPERASYON MALİYETLERİ
        // ============================================================
        [Header("Operasyon Maliyetleri")]
        public float CustomPolicyCost = 50f;
        public float RallyCostGoverning = 15f;
        public float RallyCostOpposition = 10f;
        public float PopulistPromiseCost = 15f;
        public float PersuadeMinisterCost = 10f;
        public float NeutralizeRivalCost = 20f;
        public float ScandalCost = 15f;
        public float PolicyChangeCost = 10f;
        public float AntiCampaignCost = 20f;

        // ============================================================
        // SKANDAL
        // ============================================================
        [Header("Skandal")]
        [Range(0f, 1f)] public float ScandalSuccessChance = 0.40f;
        public float ScandalDamageMin = 10f;
        public float ScandalDamageMax = 25f;

        // ============================================================
        // SEÇİM
        // ============================================================
        [Header("Seçim")]
        public int ElectionIntervalTurns = 12;
        public float IncumbencyFatiguePerTerm = 3f;
        public float LeaderImageWeight = 0.30f;

        // ============================================================
        // KAMPANYA
        // ============================================================
        [Header("Kampanya")]
        public int AntiCampaignDurationTurns = 2;

        // ============================================================
// EKONOMİ KRİZ MEKANİKLERİ (FAZ 9)
// ============================================================
[Header("Ekonomi Kriz Mekanikleri")]
[Range(0f, 1f)] public float CrisisWarningThreshold = 0.30f;    // Bu oranın altına düşünce uyarı
[Range(0f, 1f)] public float CrisisCriticalThreshold = 0.15f;   // Bu oranın altında kritik

// IMF Yardımı
public float ImfBailoutAmount = 800f;          // Anlık borç azaltma (milyar)
public float ImfBailoutConditionLegitCost = 20f;   // Meşruiyet cezası
public float ImfBailoutConditionArmyCost = 10f;    // Ordu memnuniyeti cezası
public int ImfBailoutCooldownTurns = 15;         // 15 turda bir kullanılabilir
public int ImfBailoutRequiredCrisisTurns = 5;      // En az 5 tur kritik seviyede olmalı

// Acil Vergi
public float EmergencyTaxCapital = 60f;            // Anlık sermaye kazancı
public float EmergencyTaxUnrest = 15f;             // Huzursuzluk artışı
public float EmergencyTaxLegitCost = 8f;           // Meşruiyet kaybı
public int EmergencyTaxCooldown = 8;               // 8 tur bekleme

// Yapısal Reform
public float StructuralReformCost = 100f;          // Maliyet (sermaye)
public float StructuralReformLegitGain = 15f;      // Uzun vadede meşruiyet
public float StructuralReformDebtReduction = 400f; // Anlık borç azaltma
public float StructuralReformUnrestAdd = 20f;      // Kısa vadeli huzursuzluk
public int StructuralReformDuration = 5;           // Etki süresi (tur)

        // ============================================================
        // BÜTÇE
        // ============================================================
        [Header("Bütçe")]
        public float RevenueMultiplier = 0.5f;
        public float SpendingMultiplier = 0.7f;
        public float DebtReference = 2000f;
        public float BaseInterestRate = 0.02f;
        public float MaxInterestRate = 0.10f;

        // ============================================================
        // YOLSUZLUK VE BÜROKRASİ
        // ============================================================
        [Header("Yolsuzluk ve Bürokrasi")]
        public float CorruptionDecayPerTurn = 0.5f;
        public int BureaucracyDelayDivisor = 20;

        // ============================================================
        // KRİZ
        // ============================================================
        [Header("Kriz")]
        public float CrisisTriggerChance = 0.08f;
        public int CrisisAutoResolveTurns = 4;
        public float AutoResolvePenaltyMultiplier = 1.5f;

        // ============================================================
        // ASKERİYE
        // ============================================================
        [Header("Askeriye")]
        public float CoupRiskThreshold = 30f;
        public float CoupBaseChance = 0.5f;

        // ============================================================
        // LEGITIMACY
        // ============================================================
        [Header("Legitimacy")]
        public float LegitimacyModifierDecay = 0.8f;
        public float LowLegitimacyThreshold = 35f;
    }
}