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


internal sealed class FactionsPresenter : PagePresenter
{
    public FactionsPresenter(UIManager ui) : base(ui) { }

    public override void Build(RectTransform c, SimulationEngine e)
{
    var fm = e.Factions;
    if (fm == null) { HudKit.Label(c, LocalizationManager.Get("fac_no_data"), 24, HudTheme.Dim); return; }

    HudKit.Label(c, LocalizationManager.Get("fac_title"), 28, HudTheme.Gold, TextAlignmentOptions.Left, FontStyles.Bold);
    HudKit.Label(c, LocalizationManager.Get("fac_desc"),
        20, HudTheme.Dim);

    foreach (var f in fm.Factions)
    {
        var card = ui.Card(c, null);
        var top = HudKit.NewRect(card, "Top");
        HudKit.HStack(top.gameObject, 12, 0, TextAnchor.MiddleLeft, false, true);

        var info = HudKit.NewRect(top, "Info");
        HudKit.VStack(info.gameObject, 4, 0);
        HudKit.Size(info.gameObject, flexW: 1);

        string side = f.Ideology < -20f ? LocalizationManager.Get("ideology_left") : (f.Ideology > 20f ? LocalizationManager.Get("ideology_right") : LocalizationManager.Get("ideology_center"));
        HudKit.Label(info, UIManager.Clean(f.Name) + $" ({side})", 26, HudTheme.Text, TextAlignmentOptions.Left, FontStyles.Bold);

        if (f.IsChallenging)
            HudKit.Label(info, LocalizationManager.Get("fac_challenge_fmt", f.ChallengeTurnsLeft), 20, HudTheme.Bad);

        var val = HudKit.Label(top, "%" + f.Support.ToString("F0"), 32, 
            f.Support < 20f ? HudTheme.Bad : (f.Support < 40f ? HudTheme.Warn : HudTheme.Good),
            TextAlignmentOptions.Center, FontStyles.Bold);
        HudKit.Size(val.gameObject, prefW: 100);

        float range = 100f;
        HudKit.Bar(card, f.Support / range, 
            f.Support < 20f ? HudTheme.Bad : (f.Support < 40f ? HudTheme.Warn : HudTheme.Good), 10f);

        // Aksiyon butonları
        var actions = HudKit.NewRect(card, "Actions");
        HudKit.HStack(actions.gameObject, 8, 0, TextAnchor.MiddleLeft, true, true);

        var btnConcede = HudKit.MakeButton(actions, LocalizationManager.Get("fac_concede"), HudTheme.Action, Color.white, 20, () =>
        {
            string msg = fm.ConcedeToFaction(f.Id, e, 15f);
            ui.WriteLog(msg);
            ui.MarkDirty("fraksiyonlar");
            ui.RebuildCurrent();
        }, 50);
        btnConcede.interactable = e.PoliticalCapital >= 15f;

        var btnAppoint = HudKit.MakeButton(actions, LocalizationManager.Get("fac_appoint"), HudTheme.Action, Color.white, 20, () =>
        {
            string msg = fm.AppointFactionMinister(f.Id, e);
            ui.WriteLog(msg);
            ui.MarkDirty("fraksiyonlar");
            ui.RebuildCurrent();
        }, 50);
        btnAppoint.interactable = e.PoliticalCapital >= 10f;
    }

// FAZ 3.5: Fraksiyon talepleri
if (e.FactionBargain != null && e.FactionBargain.ActiveDemands.Count > 0)
{
    var demandCard = ui.Card(c, LocalizationManager.Get("fac_demands"));
    foreach (var kv in e.FactionBargain.ActiveDemands)
    {
        var demand = kv.Value;
        var faction = fm.Factions.FirstOrDefault(f => f.Id == kv.Key);
        var policy = e.AllObjects.OfType<SimPolicy>().FirstOrDefault(p => p.Id == demand.PolicyId);
        if (faction == null || policy == null) continue;

        HudKit.Label(demandCard, 
            AIDecisionExplainer.ExplainFactionDemand(faction, demand, policy), 
            20, HudTheme.Text);

        var btnRow = HudKit.NewRect(demandCard, "BtnRow");
        HudKit.HStack(btnRow.gameObject, 8, 0, TextAnchor.MiddleLeft, true, true);

        string fid = kv.Key;
        HudKit.MakeButton(btnRow, LocalizationManager.Get("accept"), HudTheme.Good, Color.white, 20, () =>
        {
            string msg = e.FactionBargain.AcceptDemand(fid, e);
            ui.WriteLog(msg);
            ui.MarkDirty("fraksiyonlar");
            ui.RebuildCurrent();
        }, 48);

        HudKit.MakeButton(btnRow, LocalizationManager.Get("reject"), HudTheme.Bad, Color.white, 20, () =>
        {
            string msg = e.FactionBargain.RejectDemand(fid, e);
            ui.WriteLog(msg);
            ui.MarkDirty("fraksiyonlar");
            ui.RebuildCurrent();
        }, 48);
    }
}

}
}