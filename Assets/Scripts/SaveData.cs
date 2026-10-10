using System.Collections.Generic;
using DemocracySim.Engine.Core;

namespace DemocracySim.Engine.Data
{
    // Tüm oyun dünyasının anlık fotoğrafı
    public class SaveData
    {
        public Dictionary<string, AILearningMemory> AIMemories { get; set; } = new Dictionary<string, AILearningMemory>();
        // EK-1: Deterministik RNG durumu
        public uint RngMasterSeed { get; set; }
        public Dictionary<string, GameRandomState> RngStreamStates { get; set; } = new Dictionary<string, GameRandomState>();
        public int SaveVersion { get; set; } = 2;
        public string SaveName { get; set; } = "Kayıt";
        public string SaveDate { get; set; } = "";
        public string PlayerCountryName { get; set; } = "";
        public int TotalTurns { get; set; } = 0;
        public int GlobalTurn { get; set; }
        public float GlobalTension { get; set; }
        public string PlayerCountryId { get; set; } = "";
        public List<CountrySaveData> Countries { get; set; } = new List<CountrySaveData>();
        public List<OrgSaveData> Organizations { get; set; } = new List<OrgSaveData>();
        public Dictionary<string, float> DiplomacyRelations { get; set; } = new Dictionary<string, float>();
    }

    public class CountrySaveData
    {

    
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public bool IsPlayerControlled { get; set; }
        public float GlobalAlignment { get; set; }
        public EngineSaveData Engine { get; set; } = new EngineSaveData();
        
        // FAZ 4: Diplomasi hafızası  ← YENİ
        public DiplomaticMemory Memory { get; set; } = new DiplomaticMemory();
    
    
    }

    public class EngineSaveData
    {
                // FAZ 3: Kriz zincirleri
        public List<ActiveCrisis> ActiveCrises { get; set; } = new List<ActiveCrisis>();
        public Dictionary<string, int> ResolvedCrises { get; set; } = new Dictionary<string, int>();
        public List<string> UnlockedAchievements { get; set; } = new List<string>();
        public int TotalCrisesTriggered { get; set; } = 0;
        public int TotalCrisesResolved { get; set; } = 0;
        public int CurrentTurn { get; set; }
        public int TurnUntilElection { get; set; }
        public float PoliticalCapital { get; set; }
        public float Legitimacy { get; set; }
        public float CorruptionLevel { get; set; }
        public float DeepStateStability { get; set; }
        // FAZ 3.5: Seçmen geçişi — oy blokları
public Dictionary<string, Dictionary<string, float>> VotingBlocs { get; set; } 
    = new Dictionary<string, Dictionary<string, float>>();

// FAZ 3.5: Fraksiyon pazarlığı — aktif talepler
public Dictionary<string, FactionDemandSaveData> FactionDemands { get; set; }
    = new Dictionary<string, FactionDemandSaveData>();

// FAZ 3.5: Kademeli radikalleşme
public Dictionary<string, int> RadicalizationTiers { get; set; }
    = new Dictionary<string, int>();

// FAZ 3: Çıkar grupları
public List<InterestGroupSaveData> InterestGroups { get; set; }
    = new List<InterestGroupSaveData>();

// FAZ 3.5: Karar günlüğü
public List<DecisionLogSaveData> DecisionLog { get; set; }
    = new List<DecisionLogSaveData>();

        // Ekonomi, Ordu, İstihbarat
        public float Inflation { get; set; }
        public float NationalDebt { get; set; }
        public float CreditRating { get; set; }
        public float ArmySatisfaction { get; set; }
        public float MilitaryStrength { get; set; }
        public float IntelNetworkStrength { get; set; }

        // Kaydedilmeyen kritik alanlar
        public float PlayerGlobalAlignment { get; set; } = 0f;
        public string CurrentRole { get; set; } = "Governing";
        public float LegitimacyModifier { get; set; } = 0f;
        public float ArmyLoyaltyToLeader { get; set; } = 50f;
        public float ArmyCoupRiskPercent { get; set; } = 0f;
        public float IntelTechLevel { get; set; } = 10f;
        public float IntelAgencyBudget { get; set; } = 50f;

        // Listeler
        public List<ObjectSaveData> Objects { get; set; } = new List<ObjectSaveData>();
        public List<FactionSaveData> Factions { get; set; } = new List<FactionSaveData>();
        public List<ActorSaveData> Actors { get; set; } = new List<ActorSaveData>();
        public List<DemographicSaveData> Demographics { get; set; } = new List<DemographicSaveData>();
        public List<ProposedPolicySaveData> ProposedPolicies { get; set; } = new List<ProposedPolicySaveData>();
        public List<CoalitionPartnerSaveData> CoalitionPartners { get; set; } = new List<CoalitionPartnerSaveData>();
        public List<SectorSaveData> Sectors { get; set; } = new List<SectorSaveData>();
        public List<ShadowMinisterSaveData> ShadowCabinet { get; set; } = new List<ShadowMinisterSaveData>();

        // AI
        public string AIPersonalityType { get; set; } = "";
        public string RulingFactionId { get; set; } = "reformist";

        // FAZ 2: Seçim modeli
        public float LeaderImage { get; set; } = 50f;
        public int ConsecutiveTerms { get; set; } = 0;
        public float ScandalScar { get; set; } = 0f;

        // FAZ 3: Parti fonu
        public float PartyFund { get; set; } = 100f;

        // FAZ 3: Anti-kampanya
        public Dictionary<string, int> ActiveAntiCampaigns { get; set; } = new Dictionary<string, int>();

        // FAZ 3 Adım 4: Protesto  ← ARTIK DOĞRU SINIFTA
        public string ActiveProtestTheme { get; set; } = "";
        public int ActiveProtestTurnsLeft { get; set; } = 0;
        public float ActiveProtestIntensity { get; set; } = 0f;
        public int ProtestCooldown { get; set; } = 0;
        // FAZ 3: Göç yönetimi
public float MigrantIntegration { get; set; } = 50f;
public float MigrantPopulation { get; set; } = 0f;
public float MigrantPendingRemittance { get; set; } = 0f;
public List<float> MigrantHistory { get; set; } = new List<float>();

        // FAZ 3 Adım 5: Yargı  ← ARTIK DOĞRU SINIFTA
        public float JudicialIndependence { get; set; } = 60f;
        public string ActiveCaseType { get; set; } = "";
        public string ActiveCaseTargetId { get; set; } = "";
        public int ActiveCaseTurnsLeft { get; set; } = 0;
        public float ActiveCaseChance { get; set; } = 0f;
        public int CaseCooldown { get; set; } = 0;
        public float TechLevel { get; set; } = 30f;
        public int PatentCount { get; set; } = 5;
        public float RndBudget { get; set; } = 30f;
        public float BrainGain { get; set; } = 0f;
        public float PatentProgress { get; set; } = 0f;
    }

    // -------------------- Alt sınıflar --------------------

    public class ShadowMinisterSaveData
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Portfolio { get; set; } = "";
        public float Competence { get; set; }
        public float MediaSkill { get; set; }
        public float Loyalty { get; set; }
        public int AppointedTurn { get; set; }
    }
    // FAZ 3.5: Fraksiyon talebi
public class FactionDemandSaveData
{
    public string PolicyId { get; set; } = "";
    public float TargetValue { get; set; }
    public float SupportReward { get; set; }
    public float RejectionPenalty { get; set; }
    public bool IsFulfilled { get; set; }
}

// FAZ 3: Çıkar grubu
public class InterestGroupSaveData
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public float Ideology { get; set; }
    public float Power { get; set; }
    public float Satisfaction { get; set; }
    public float LobbyBudget { get; set; }
}

// FAZ 3.5: Karar günlüğü
public class DecisionLogSaveData
{
    public int Turn { get; set; }
    public string Category { get; set; } = "";
    public string Description { get; set; } = "";
    public float Impact { get; set; }
    public string Timestamp { get; set; } = "";  // DateTime → string
}

    public class FactionSaveData
    {
        public string Id { get; set; } = "";
        public float Support { get; set; } = 50f;
        public bool IsChallenging { get; set; } = false;
        public int ChallengeTurnsLeft { get; set; } = 0;
    }

    public class CoalitionPartnerSaveData
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public float Ideology { get; set; }
        public float Satisfaction { get; set; } = 70f;
        public bool InGovernment { get; set; } = true;
        public int LeftTurn { get; set; } = 0;
    }

    public class SectorSaveData
    {
        public string Id { get; set; } = "";
        public float GdpShare { get; set; }
        public float Productivity { get; set; }
        public float TaxBurden { get; set; }
        public float Employment { get; set; }
    }

    public class ObjectSaveData
    {
        public string Id { get; set; } = "";
        public float ActualValue { get; set; }
        public bool IsActive { get; set; }
        public float Intensity { get; set; }
    }

    public class ProposedPolicySaveData
    {
        public string PolicyId { get; set; } = "";
    }

    public class ActorSaveData
    {
        public string Id { get; set; } = "";
        public float Loyalty { get; set; }
        public float Satisfaction { get; set; }
    }

    public class DemographicSaveData
    {
        public string Id { get; set; } = "";
        public float Satisfaction { get; set; }
    }

    public class OrgSaveData
    {
        public string Id { get; set; } = "";
        public List<string> MemberIds { get; set; } = new List<string>();
        public string LeaderCountryId { get; set; } = "";
    }
}