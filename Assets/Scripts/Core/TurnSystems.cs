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

    /// <summary>Öncelik 85: Bakanların pasif etkileri.</summary>
    public class MinisterPassiveSystem : ITurnSystem
    {
        public string SystemName => "MinisterPassive";
        public int Priority => 85;
        public void ProcessTurn(SimulationEngine e)
        {
            foreach (var minister in e.Actors.Where(a => a.Role == ActorRole.Minister))
            {
                if (string.IsNullOrEmpty(minister.Portfolio)) continue;
                float competenceFactor = minister.Competence / 100f;
                switch (minister.Portfolio.ToLower())
                {
                    case "ekonomi":
    // EK-2: Registry + null-safe
    var gdp = e.Registry.Get(ObjectRegistry.Ids.Gdp);
    if (gdp != null) gdp.ActualValue += competenceFactor * 0.1f;
    break;
                    case "sağlık":
    {
        var obj = e.Registry.Get(ObjectRegistry.Ids.HealthcareQuality);
        if (obj != null) obj.ActualValue += competenceFactor * 0.1f;
    }
    break;
case "savunma":
    {
        var obj = e.Registry.Get(ObjectRegistry.Ids.MilitaryStrength);
        if (obj != null) obj.ActualValue += competenceFactor * 0.1f;
    }
    break;
case "eğitim":
    {
        var obj = e.Registry.Get(ObjectRegistry.Ids.EducationLevel);
        if (obj != null) obj.ActualValue += competenceFactor * 0.1f;
    }
    break;
case "adalet":
    {
        var obj = e.Registry.Get(ObjectRegistry.Ids.CrimeRate);
        if (obj != null) obj.ActualValue -= competenceFactor * 0.1f;
    }
    break;
                }
                e.PoliticalCapital = System.Math.Clamp(
                    e.PoliticalCapital + competenceFactor * 5f, 0f, e.MaxPoliticalCapital);
            }
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
}