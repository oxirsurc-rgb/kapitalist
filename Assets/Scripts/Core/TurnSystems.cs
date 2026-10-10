using System.Linq;
using System.Diagnostics;

namespace DemocracySim.Engine.Core
{
    // ============================================================
    // FAZ R: TUR SİSTEMLERİ — öncelik sırasına göre
    // ============================================================

    /// <summary>Öncelik 10: Seçmen geçişi (en önce).</summary>
    public class VoterTransitionSystem : ITurnSystem
    {
        public string SystemName => "VoterTransition";
        public int Priority => 10;
        public void ProcessTurn(SimulationEngine e) => e.VoterTransition.ProcessTurn(e);
    }

    /// <summary>Öncelik 20: Fraksiyonlar + pazarlık.</summary>
    public class FactionSystem : ITurnSystem
    {
        public string SystemName => "Factions";
        public int Priority => 20;
        public void ProcessTurn(SimulationEngine e)
        {
            e.Factions.ProcessTurn(e, e.PlayerGlobalAlignment);
            e.FactionBargain.ProcessTurn(e);
        }
    }

    /// <summary>Öncelik 25: Gölge kabine.</summary>
    public class ShadowCabinetSystem : ITurnSystem
    {
        public string SystemName => "ShadowCabinet";
        public int Priority => 25;
        public void ProcessTurn(SimulationEngine e) => e.ShadowCab.ProcessTurn(e);
    }

    /// <summary>Öncelik 30: Parti fonu.</summary>
    public class PartyBudgetSystem : ITurnSystem
    {
        public string SystemName => "PartyBudget";
        public int Priority => 30;
        public void ProcessTurn(SimulationEngine e) => e.Party.ProcessTurn(e);
    }

    /// <summary>Öncelik 35: Protesto.</summary>
    public class ProtestSystem : ITurnSystem
    {
        public string SystemName => "Protest";
        public int Priority => 35;
        public void ProcessTurn(SimulationEngine e) => e.Protest.ProcessTurn(e);
    }

    /// <summary>Öncelik 40: Yargı.</summary>
    public class JudiciarySystem : ITurnSystem
    {
        public string SystemName => "Judiciary";
        public int Priority => 40;
        public void ProcessTurn(SimulationEngine e) => e.Judiciary.ProcessTurn(e);
    }

    /// <summary>Öncelik 45: Anti-kampanyalar.</summary>
    public class AntiCampaignSystem : ITurnSystem
    {
        public string SystemName => "AntiCampaigns";
        public int Priority => 45;
        public void ProcessTurn(SimulationEngine e)
        {
            // ProcessAntiCampaigns private; reflection yerine engine'e public metot ekleyeceğiz
            e.ProcessAntiCampaignsPublic();
        }
    }

    /// <summary>Öncelik 50: Sektörler.</summary>
    public class SectorSystem : ITurnSystem
    {
        public string SystemName => "Sectors";
        public int Priority => 50;
        public void ProcessTurn(SimulationEngine e) => e.Sectors.ProcessTurn(e);
    }

    /// <summary>Öncelik 55: Medya kuruluşları.</summary>
    public class MediaOutletSystem : ITurnSystem
    {
        public string SystemName => "MediaOutlets";
        public int Priority => 55;
        public void ProcessTurn(SimulationEngine e) => e.MediaOutlets.ProcessTurn(e, e.PlayerGlobalAlignment);
    }

    /// <summary>Öncelik 60: Teknoloji.</summary>
    public class TechnologySystem : ITurnSystem
    {
        public string SystemName => "Technology";
        public int Priority => 60;
        public void ProcessTurn(SimulationEngine e) => e.Technology.ProcessTurn(e);
    }

    /// <summary>Öncelik 65: Kriz zincirleri.</summary>
    public class CrisisChainSystem : ITurnSystem
    {
        public string SystemName => "CrisisChains";
        public int Priority => 65;
        public void ProcessTurn(SimulationEngine e) => e.CrisisChains.ProcessTurn(e, e.Universe.SanctionLevel);
    }

    /// <summary>Öncelik 70: Ekonomi (faiz + enflasyon + iflas).</summary>
    public class EconomySystem : ITurnSystem
    {
        public string SystemName => "Economy";
        public int Priority => 70;
        public void ProcessTurn(SimulationEngine e) => e.Economy.UpdateEconomy(e);
    }

    /// <summary>Öncelik 75: Ordu + darbe kontrolü.</summary>
    public class ArmySystem : ITurnSystem
    {
        public string SystemName => "Army";
        public int Priority => 75;
        public void ProcessTurn(SimulationEngine e)
        {
            var milPolicy = e.Registry.GetPolicy(ObjectRegistry.Ids.MilitaryBudget);
            float currentMilBudget = (milPolicy != null && milPolicy.IsActive) ? milPolicy.GetEffectiveValue() : 50f;
            e.Army.UpdateArmy(e, currentMilBudget);

            if (e.CurrentRole == SimulationEngine.PlayerRole.Governing && e.Army.CheckForCoup(e))
            {
                e.CurrentRole = SimulationEngine.PlayerRole.Opposition;
                e.Legitimacy.AdjustLegitimacy(-50f);
                e.TurnUntilElection = 12;
                e.Universe.Messages.Add(new UniverseMessage
                {
                    Text = "ASKERİ DARBE! Ordu yönetime el koydu; artık muhalefettesiniz.",
                    IsWarning = true
                });
            }
        }
    }

    /// <summary>Öncelik 80: İstihbarat.</summary>
    public class IntelSystem : ITurnSystem
    {
        public string SystemName => "Intel";
        public int Priority => 80;
        public void ProcessTurn(SimulationEngine e) => e.Intel.UpdateAgency(40f);
    }

    /// <summary>Öncelik 85: Bakanların pasif etkileri (Democracy 4 & Reel Hayat Dinamikleri).</summary>
    public class MinisterPassiveSystem : ITurnSystem
    {
        public string SystemName => "MinisterPassive";
        public int Priority => 85;
        public void ProcessTurn(SimulationEngine e)
        {
            var ministers = e.Actors.Where(a => a.Role == ActorRole.Minister).ToList();
            if (ministers.Count == 0 && e.CurrentRole == SimulationEngine.PlayerRole.Governing)
            {
                // Boş kabine cezası: bakan atanmamışsa yönetim zaafiyeti
                e.PoliticalCapital = System.Math.Max(0f, e.PoliticalCapital - 2f);
                return;
            }

            foreach (var minister in ministers)
            {
                if (string.IsNullOrEmpty(minister.Portfolio)) continue;
                float comp = minister.Competence / 100f;
                float loyaltyFactor = (minister.Loyalty - 50f) / 50f; // -1 .. +1

                // 1) Siyasi Sermaye Üretimi (Democracy 4 dinamiği)
                // Yetkin ve sadık bakanlar sermaye kazandırır; yetersiz/sadakatsiz olanlar eritir.
                float capitalDelta = (comp * 2.2f) + (loyaltyFactor * 1.2f);
                e.PoliticalCapital = System.Math.Clamp(
                    e.PoliticalCapital + capitalDelta, 0f, e.MaxPoliticalCapital);

                // 2) Departman Spesifik Reel Etkiler
                switch (minister.Portfolio.ToLower())
                {
                    case "ekonomi":
                        var gdp = e.Registry.Get(ObjectRegistry.Ids.Gdp);
                        if (gdp != null) gdp.ActualValue += comp * 0.2f;
                        if (e.Economy != null && comp > 0.5f)
                            e.Economy.AdjustInflation(-(comp - 0.5f) * 0.2f);
                        break;
                    case "sağlık":
                        var hq = e.Registry.Get(ObjectRegistry.Ids.HealthcareQuality);
                        if (hq != null) hq.ActualValue += comp * 0.2f;
                        var life = e.Registry.Get(ObjectRegistry.Ids.LifeExpectancy);
                        if (life != null) life.ActualValue += comp * 0.1f;
                        break;
                    case "savunma":
                        var mil = e.Registry.Get(ObjectRegistry.Ids.MilitaryStrength);
                        if (mil != null) mil.ActualValue += comp * 0.2f;
                        if (e.Army != null)
                            e.Army.AdjustLoyalty(comp * 0.4f);
                        break;
                    case "eğitim":
                        var edu = e.Registry.Get(ObjectRegistry.Ids.EducationLevel);
                        if (edu != null) edu.ActualValue += comp * 0.2f;
                        break;
                    case "adalet":
                        var crime = e.Registry.Get(ObjectRegistry.Ids.CrimeRate);
                        if (crime != null) crime.ActualValue -= comp * 0.2f;
                        e.CorruptionLevel = System.Math.Max(0f, e.CorruptionLevel - comp * 0.2f);
                        break;
                    case "içişleri":
                        e.Universe.Unrest = System.Math.Max(0f, e.Universe.Unrest - comp * 0.3f);
                        break;
                    case "dışişleri":
                        if (e.Universe.SanctionLevel > 0f)
                            e.Universe.SanctionLevel = System.Math.Max(0f, e.Universe.SanctionLevel - comp * 0.3f);
                        break;
                    case "çevre":
                        var env = e.Registry.Get(ObjectRegistry.Ids.EnvironmentQuality);
                        if (env != null) env.ActualValue += comp * 0.25f;
                        break;
                    case "teknoloji":
                        var tech = e.Registry.Get(ObjectRegistry.Ids.TechLevel);
                        if (tech != null) tech.ActualValue += comp * 0.25f;
                        break;
                    case "enerji":
                        var energy = e.Registry.Get(ObjectRegistry.Ids.EnergySecurity);
                        if (energy != null) energy.ActualValue += comp * 0.2f;
                        break;
                }

                // 3) Nitelik (Trait) ve Seçmen Tabanı Sempatisi (Democracy 4)
                if (minister.Traits != null)
                {
                    foreach (var trait in minister.Traits)
                    {
                        switch (trait)
                        {
                            case ActorTrait.BusinessPerson:
                                AdjustDemographic(e, "capitalists", 0.4f);
                                AdjustDemographic(e, "workers", -0.2f);
                                break;
                            case ActorTrait.Activist:
                                AdjustDemographic(e, "environmentalists", 0.5f);
                                AdjustDemographic(e, "workers", 0.3f);
                                AdjustDemographic(e, "capitalists", -0.3f);
                                break;
                            case ActorTrait.Populist:
                                AdjustDemographic(e, "workers", 0.5f);
                                AdjustDemographic(e, "intellectuals", -0.3f);
                                e.Legitimacy.AdjustLegitimacy(0.2f);
                                break;
                            case ActorTrait.Technocrat:
                                AdjustDemographic(e, "intellectuals", 0.5f);
                                e.CorruptionLevel = System.Math.Max(0f, e.CorruptionLevel - 0.1f);
                                break;
                            case ActorTrait.Academic:
                                AdjustDemographic(e, "intellectuals", 0.4f);
                                break;
                            case ActorTrait.Bureaucrat:
                                e.Legitimacy.AdjustLegitimacy(0.15f);
                                break;
                        }
                    }
                }
            }
        }

        private static void AdjustDemographic(SimulationEngine e, string groupId, float delta)
        {
            var grp = e.Demographics.FirstOrDefault(g => g.Id == groupId);
            if (grp != null) grp.AdjustSatisfaction(delta);
        }
    }

    /// <summary>Öncelik 90: Simülasyon iterasyonu (denge noktası).</summary>
    public class SimulationIterationSystem : ITurnSystem
    {
        public string SystemName => "SimulationIteration";
        public int Priority => 90;
        public void ProcessTurn(SimulationEngine e) => e.RunSimulationIterations();
    }

    /// <summary>Öncelik 95: Geçmiş kaydı + bilgi filtresi.</summary>
    public class HistoryAndInfoSystem : ITurnSystem
    {
        public string SystemName => "HistoryAndInfo";
        public int Priority => 95;
        public void ProcessTurn(SimulationEngine e) => e.RecordHistoryAndFilterInfo();
    }

    /// <summary>Öncelik 100: Demografik etki + meşruiyet.</summary>
    public class DemographicImpactSystem : ITurnSystem
    {
        public string SystemName => "DemographicImpact";
        public int Priority => 100;
        public void ProcessTurn(SimulationEngine e) => e.ApplyDemographicImpact();
    }

    /// <summary>Öncelik 105: Aktör değerlendirmesi.</summary>
    public class ActorEvaluationSystem : ITurnSystem
    {
        public string SystemName => "ActorEvaluation";
        public int Priority => 105;
        public void ProcessTurn(SimulationEngine e)
        {
            foreach (var actor in e.Actors) actor.EvaluateWorld(e.AllObjects, e.PlayerGlobalAlignment);
        }
    }

    /// <summary>Öncelik 110: Kabine entrikaları + istifalar.</summary>
    public class CabinetIntrigueSystem : ITurnSystem
    {
        public string SystemName => "CabinetIntrigue";
        public int Priority => 110;
        public void ProcessTurn(SimulationEngine e)
        {
            if (e.Cabinet != null)
                e.Cabinet.ProcessCabinetIntrigues(
                    e.Actors.Where(a => a.Role == ActorRole.Minister).ToList(), e);
            e.Cabinet.ProcessResignations(e);
            e.Cabinet.ProcessResignationRisks(e);
        }
    }

    /// <summary>Öncelik 115: Koalisyon + güvensizlik önergesi.</summary>
    public class CoalitionSystem : ITurnSystem
    {
        public string SystemName => "Coalition";
        public int Priority => 115;
        public void ProcessTurn(SimulationEngine e)
        {
            e.PartyManager.ProcessCoalitionTurn(e);
            if (e.PartyManager.IsVoteOfNoConfidenceActive)
            {
                string result = e.PartyManager.ProcessNoConfidenceTurn(e);
                if (!string.IsNullOrEmpty(result))
                    e.Universe.Messages.Add(new UniverseMessage { Text = result, IsWarning = true });
            }
        }
    }

    /// <summary>Öncelik 120: Medya haberleri.</summary>
    public class MediaNewsSystem : ITurnSystem
    {
        public string SystemName => "MediaNews";
        public int Priority => 120;
        public void ProcessTurn(SimulationEngine e)
        {
            e.CurrentNews = e.Media.GenerateTurnNews(e);
            foreach (var art in e.CurrentNews) e.Legitimacy.AdjustLegitimacy(art.ImpactOnLegitimacy);
        }
    }

    /// <summary>Öncelik 125: Evren sistemleri (halk, koalisyon, krizler).</summary>
    public class UniverseSystem : ITurnSystem
    {
        public string SystemName => "Universe";
        public int Priority => 125;
        public void ProcessTurn(SimulationEngine e) => UniverseSystems.ProcessTurn(e);
    }
    /// <summary>Öncelik 138: Başarım kontrolü (tur sonu).</summary>
public class AchievementSystem : ITurnSystem
{
    public string SystemName => "Achievements";
    public int Priority => 138;

    public void ProcessTurn(SimulationEngine e)
    {
        // EK-19: Başarımlar sadece oyuncu ülkesi için — AI ülkelerin başarımları yok
        if (!e.IsPlayerCountry) return;

        var newlyUnlocked = e.Achievements.ProcessTurn(e);
        foreach (var ach in newlyUnlocked)
        {
            DemocracySim.Engine.Data.GlobalAchievementTracker.Unlock(ach.Id);
            e.Universe.Messages.Add(new UniverseMessage
            {
                Text = $"🏆 BAŞARIM: {ach.Name} — {ach.Description}",
                IsWarning = false
            });
        }
    }
}

    public class MigrationSystem : ITurnSystem
{
    public string SystemName => "Migration";
    public int Priority => 128;
    public void ProcessTurn(SimulationEngine e) => e.Migration.ProcessTurn(e);
}

    /// <summary>Öncelik 130: Teknoloji GSYİH bonusu + yaptırım direnci.</summary>
    public class TechBonusSystem : ITurnSystem
    {
        /// <summary>Öncelik 128: Göç yönetimi.</summary>

        public string SystemName => "TechBonus";
        public int Priority => 130;
        public void ProcessTurn(SimulationEngine e) => e.ApplyTechBonuses();
    }

    /// <summary>Öncelik 135: Tur sayaçları (election, capital, corruption).</summary>
    public class TurnCounterSystem : ITurnSystem
    {
        public string SystemName => "TurnCounters";
        public int Priority => 135;
        public void ProcessTurn(SimulationEngine e) => e.AdvanceTurnCounters();
    }

    /// <summary>Öncelik 118: Muhalefet momentumu (her tur güncellenir).</summary>
public class OppositionMomentumSystem : ITurnSystem
{
    public string SystemName => "OppositionMomentum";
    public int Priority => 118;
    public void ProcessTurn(SimulationEngine e)
    {
        e.Elections.ProcessOppositionMomentum(e);
    }
}

/// <summary>Öncelik 140: Adaylık krizi uyarısı.</summary>
public class CandidateCrisisSystem : ITurnSystem
{
    public string SystemName => "CandidateCrisis";
    public int Priority => 140;

    public void ProcessTurn(SimulationEngine e)
    {
        if (!e.IsPlayerCountry) return;

        string msg = CandidateManager.GetStatusMessage(e);
        if (msg != null)
        {
            e.Universe.Messages.Add(new UniverseMessage
            {
                Text = msg,
                IsWarning = !CandidateManager.CanRun(e)
            });
        }
    }
}
/// <summary>Öncelik 141: Bakan piyasası yenileme.</summary>
public class MinistryMarketSystem : ITurnSystem
{
    public string SystemName => "MinistryMarket";
    public int Priority => 141;

    public void ProcessTurn(SimulationEngine e)
    {
        if (!e.IsPlayerCountry) return;

        // İlk turda piyasayı doldur
        if (e.Universe.MinistryMarket == null || e.Universe.MinistryMarket.Count == 0)
        {
            MinistryMarket.RefreshMarket(e);
            return;
        }

        // Her 3 turda bir yenile
        MinistryMarket.ProcessTurn(e);
    }
}

/// <summary>Öncelik 142: Ekonomi kriz mekanikleri.</summary>
public class EconomicCrisisSystem : ITurnSystem
{
    public string SystemName => "EconomicCrisis";
    public int Priority => 142;

    public void ProcessTurn(SimulationEngine e)
    {
        if (!e.IsPlayerCountry) return;

        EconomicCrisisManager.ProcessTurn(e);

        // Kritik uyarı mesajı (her 3 turda bir)
        if (e.Universe.EconomicWarningLevel == 2 && e.CurrentTurn % 3 == 0)
        {
            float health = EconomicCrisisManager.GetEconomicHealth(e);
            e.Universe.Messages.Add(new UniverseMessage
            {
                Text = $"🚨 EKONOMİK KRİZ UYARISI: Durum {EconomicCrisisManager.GetHealthLabel(health)}. " +
                       "Acil önlem almalısınız! (Devlet Menüsü > Ekonomi)",
                IsWarning = true
            });
        }
        else if (e.Universe.EconomicWarningLevel == 1 && e.CurrentTurn % 5 == 0)
        {
            float health = EconomicCrisisManager.GetEconomicHealth(e);
            e.Universe.Messages.Add(new UniverseMessage
            {
                Text = $"⚠️ EKONOMİK UYARI: Durum {EconomicCrisisManager.GetHealthLabel(health)}. " +
                       "Devlet Menüsü > Ekonomi bölümünden önlem alabilirsiniz.",
                IsWarning = true
            });
        }
    }
}

    /// <summary>Öncelik 145: Olay kontrolü (eski EventSys pipeline dışıydı).</summary>
public class EventCheckSystem : ITurnSystem
{
    public string SystemName => "EventCheck";
    public int Priority => 145;   // En son çalışsın
    
    // Son turda tetiklenen olay (ProcessTurn dönüş değeri için)
    public GameEvent LastTriggeredEvent { get; private set; }
    
    public void ProcessTurn(SimulationEngine e)
    {
        LastTriggeredEvent = e.EventSys.CheckForEvent(e);
    }
}

    // ============================================================
    // Yardımcı extension: null-safe Let
    // ============================================================
    internal static class ObjectExtensions
    {
        public static void Let<T>(this T obj, System.Action<T> action) where T : class
        {
            if (obj != null) action(obj);
        }
    }

    /// <summary>FAZ 22: Piyasa dinamikleri (arz-talep-fiyat).</summary>
public class MarketDynamicsSystem : ITurnSystem
{
    public string SystemName => "MarketDynamics";
    public int Priority => 68;   // Ekonomi'den hemen önce
    public void ProcessTurn(SimulationEngine e) => e.Market.ProcessTurn(e);
}
}