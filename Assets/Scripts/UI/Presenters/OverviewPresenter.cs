using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DemocracySim.Engine.Core;
using DemocracySim.Engine.World;
using DemocracySim.Engine.Legislative;

// R-REFACTOR: UIManager artık bir "kabuk": tuval, sekmeler, modal ve toast yönetimi.
// Sayfa içerikleri ilgili Presenter sınıflarında kurulur. Davranış değişmedi.


internal sealed class OverviewPresenter : PagePresenter
{
    public OverviewPresenter(UIManager ui) : base(ui) { }

    public override void Build(RectTransform c, SimulationEngine e)
    {
        bool opp = e.CurrentRole == SimulationEngine.PlayerRole.Opposition;
        float legit = e.Legitimacy.CurrentLegitimacy;

        var row = ui.Row(c);
        ui.StatCard(row, opp ? LocalizationManager.Get("hud_public_support") : LocalizationManager.Get("hud_legit_title"), legit.ToString("F1") + "%", legit / 100f, UIManager.GoodHigh(legit, 55f, 35f), ui.DeltaStr("legit", true));
        ui.StatCard(row, LocalizationManager.Get("stat_political_capital"), e.PoliticalCapital.ToString("F0"), e.PoliticalCapital / Mathf.Max(1f, e.MaxPoliticalCapital), HudTheme.Gold, ui.DeltaStr("capital", true));
        ui.StatCard(row, LocalizationManager.Get("stat_street_unrest"), e.Universe.Unrest.ToString("F0"), e.Universe.Unrest / 100f, UIManager.GoodLow(e.Universe.Unrest, 25f, 55f), ui.DeltaStr("unrest", false));
        ui.StatCard(row, LocalizationManager.Get("stat_army_loyalty"), "%" + e.Army.ArmySatisfaction.ToString("F0"), e.Army.ArmySatisfaction / 100f, UIManager.GoodHigh(e.Army.ArmySatisfaction, 55f, 30f), ui.DeltaStr("army", true));

        if (e.CrisisChains != null && e.CrisisChains.ActiveCrises.Count > 0)
{
    var crisisCard = ui.Card(c, LocalizationManager.Get("card_active_crises"));
    foreach (var crisis in e.CrisisChains.ActiveCrises)
    {
        var crisisRow = HudKit.NewRect(crisisCard, "CrisisRow");
        HudKit.VStack(crisisRow.gameObject, 4, 0);    // ✅ crisisRow kullan
        var top = HudKit.NewRect(crisisRow, "Top");   // ✅ crisisRow kullan
        HudKit.HStack(top.gameObject, 8, 0, TextAnchor.MiddleLeft, false, true);

        string stageStr = LocalizationManager.Get("crisis_stage_fmt", crisis.CurrentStage + 1, crisis.TotalStages);
        HudKit.Label(top, UIManager.Clean(crisis.Title), 22, HudTheme.Bad,
            TextAlignmentOptions.Left, FontStyles.Bold);
        HudKit.Label(top, stageStr, 18, HudTheme.Warn, TextAlignmentOptions.Right);

        HudKit.Label(crisisRow, UIManager.Clean(crisis.StageDescription), 18, HudTheme.Dim);

        int turnsLeft = CrisisChainManager.CrisisAutoResolveTurns - crisis.TurnsSinceStart;
        float progress = 1f - ((float)turnsLeft / CrisisChainManager.CrisisAutoResolveTurns);
        HudKit.Label(crisisRow, LocalizationManager.Get("crisis_auto_resolve_fmt", turnsLeft), 17,
            turnsLeft <= 1 ? HudTheme.Bad : HudTheme.Warn);
        HudKit.Bar(crisisRow, progress, turnsLeft <= 1 ? HudTheme.Bad : HudTheme.Warn, 8f);
    }
}

        // FAZ 3: Parti Fonu göstergesi (muhalefetteyken önemli)
        if (e.CurrentRole == SimulationEngine.PlayerRole.Opposition && e.Party != null)
        {
            var partyCard = ui.Card(c, LocalizationManager.Get("card_party_fund"));
            float fund = e.Party.Fund;
            float maxFund = e.Party.MaxFund;
            Color fundColor = fund < 30f ? HudTheme.Bad : (fund < 100f ? HudTheme.Warn : HudTheme.Good);

            ui.StatRow(partyCard, LocalizationManager.Get("party_fund_label"), 
                $"{fund:F0} / {maxFund:F0}", 
                fund / maxFund, 
                fundColor, "");

            float net = e.Party.NetIncome;
            ui.StatRow(partyCard, LocalizationManager.Get("party_net_income"), 
                LocalizationManager.Get("per_turn_fmt", (net >= 0 ? "+" : "") + net.ToString("F0")), 
                Mathf.Clamp01(Mathf.Abs(net) / 10f), 
                net >= 0 ? HudTheme.Good : HudTheme.Bad, "");

            string advice = fund < 30f 
                ? LocalizationManager.Get("party_fund_critical")
                : fund < 100f 
                    ? LocalizationManager.Get("party_fund_low") 
                    : LocalizationManager.Get("party_fund_ok");
            HudKit.Label(partyCard, advice, 19, fundColor);
        }
                // FAZ 3 Adım 5: Yargı durumu
        if (e.CurrentRole == SimulationEngine.PlayerRole.Opposition && e.Judiciary != null)
        {
            var judCard = ui.Card(c, LocalizationManager.Get("card_judiciary"));
            float indep = e.Judiciary.JudicialIndependence;
            Color indepColor = UIManager.GoodHigh(indep, 60f, 40f);
            ui.StatRow(judCard, LocalizationManager.Get("judiciary_independence"), 
                "%" + indep.ToString("F0"), indep / 100f, indepColor, "");
            HudKit.Label(judCard, e.Judiciary.GetStatusText(), 20, HudTheme.Text);
        }
                // FAZ 3 Adım 4: Protesto durumu (muhalefetteyken)
        if (e.CurrentRole == SimulationEngine.PlayerRole.Opposition && e.Protest != null)
        {
            var protestCard = ui.Card(c, LocalizationManager.Get("card_protest_status"));
            string status = e.Protest.GetStatusText();
            Color statusColor = e.Protest.Current != null ? HudTheme.Warn 
                              : e.Protest.CooldownTurns > 0 ? HudTheme.Dim 
                              : HudTheme.Good;
            HudKit.Label(protestCard, status, 22, statusColor, TextAlignmentOptions.Left, FontStyles.Bold);

            if (e.Protest.Current != null)
            {
                float intensity = e.Protest.Current.InitialIntensity;
                string themeStr = ProtestManager.ThemeTurkish(e.Protest.Current.Theme);
                HudKit.Label(protestCard, 
                    LocalizationManager.Get("protest_info_fmt", themeStr, intensity.ToString("F0"), e.Protest.Current.TurnsLeft), 
                    20, HudTheme.Text);
                HudKit.Label(protestCard, 
                    LocalizationManager.Get("protest_effect"), 
                    18, HudTheme.Dim);
            }
            else if (e.Protest.CooldownTurns > 0)
            {
                HudKit.Label(protestCard, LocalizationManager.Get("protest_cooldown_fmt", e.Protest.CooldownTurns), 19, HudTheme.Dim);
            }
            else
            {
                HudKit.Label(protestCard, 
                    LocalizationManager.Get("protest_ready"), 
                    19, HudTheme.Dim);
            }
        }

    
        var row2 = ui.Row(c);
        var groupsCard = ui.Card(row2, LocalizationManager.Get("card_demographics"), -1, 3);
        foreach (var g in e.Demographics)
        {
            bool radical = UIManager.IsRadical(e, g.Id);
            string name = UIManager.Clean(g.Name) + (radical ? "  (" + LocalizationManager.Get("radical_tag") + ")" : "");
            ui.StatRow(groupsCard, name, "%" + g.Satisfaction.ToString("F0"), g.Satisfaction / 100f,
                    radical ? HudTheme.Bad : UIManager.GoodHigh(g.Satisfaction, 55f, 35f), ui.DeltaStr("grp:" + g.Id, true));
        }

        var stateCard = ui.Card(row2, LocalizationManager.Get("card_state_status"), -1, 2);
        List<string> lines = null;
        try { lines = UniverseSystems.StatusReport(e); } catch (Exception ex) { Debug.LogWarning("[UI] StatusReport: " + ex.Message); }
        if (lines != null)
            foreach (var l in lines) HudKit.Label(stateCard, UIManager.Clean(l), 20, HudTheme.Dim);

        // Yasama süreci
        var legis = ui.Card(c, LocalizationManager.Get("card_legislative_process"));
        bool any = false;
        foreach (var p in e.ProposedPolicies)
        {
            any = true;
            HudKit.Label(legis, LocalizationManager.Get("legis_on_agenda_fmt", UIManager.Clean(p.Name)), 21, HudTheme.Text);
        }
        foreach (var pend in e.Universe.Pending)
        {
            any = true;
            HudKit.Label(legis, LocalizationManager.Get("legis_in_bureaucracy_fmt", UIManager.Clean(UIManager.PolicyName(e, pend.PolicyId)), pend.TurnsLeft), 21, HudTheme.Warn);
        }
        if (!any) HudKit.Label(legis, LocalizationManager.Get("legis_none"), 21, HudTheme.Dim);
    }
}