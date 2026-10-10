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


internal sealed class InterestGroupsPresenter : PagePresenter
{
    public InterestGroupsPresenter(UIManager ui) : base(ui) { }

    public override void Build(RectTransform c, SimulationEngine e)
{
    if (e.InterestGroups == null || e.InterestGroups.Count == 0)
    {
        HudKit.Label(c, LocalizationManager.Get("ig_no_data"), 24, HudTheme.Dim);
        return;
    }

    HudKit.Label(c, LocalizationManager.Get("ig_title"), 28, HudTheme.Gold, 
        TextAlignmentOptions.Left, FontStyles.Bold);
    HudKit.Label(c, LocalizationManager.Get("ig_desc"), 
        20, HudTheme.Dim);

    foreach (var ig in e.InterestGroups)
    {
        var card = ui.Card(c, null);
        var top = HudKit.NewRect(card, "Top");
        HudKit.HStack(top.gameObject, 12, 0, TextAnchor.MiddleLeft, false, true);

        var info = HudKit.NewRect(top, "Info");
        HudKit.VStack(info.gameObject, 4, 0);
        HudKit.Size(info.gameObject, flexW: 1);

        string side = ig.Ideology < -20f ? LocalizationManager.Get("ideology_left") : (ig.Ideology > 20f ? LocalizationManager.Get("ideology_right") : LocalizationManager.Get("ideology_center"));
        HudKit.Label(info, UIManager.Clean(ig.Name) + $" ({side})", 26, HudTheme.Text, 
            TextAlignmentOptions.Left, FontStyles.Bold);
        HudKit.Label(info, LocalizationManager.Get("ig_info_fmt", ig.Power.ToString("F0"), ig.LobbyBudget.ToString("F0")), 
            19, HudTheme.Dim);

        var satLabel = HudKit.Label(top, $"%{ig.Satisfaction:F0}", 32,
            ig.Satisfaction < 30f ? HudTheme.Bad : (ig.Satisfaction < 50f ? HudTheme.Warn : HudTheme.Good),
            TextAlignmentOptions.Center, FontStyles.Bold);
        HudKit.Size(satLabel.gameObject, prefW: 100);

        HudKit.Bar(card, ig.Satisfaction / 100f,
            ig.Satisfaction < 30f ? HudTheme.Bad : (ig.Satisfaction < 50f ? HudTheme.Warn : HudTheme.Good), 10f);

        // Lobi baskısı uygula butonu (muhalefetteyken)
        if (e.CurrentRole == SimulationEngine.PlayerRole.Opposition && ig.LobbyBudget >= 20f)
        {
            var btn = HudKit.MakeButton(card, LocalizationManager.Get("ig_lobby_btn"), 
                HudTheme.Action, Color.white, 20, () =>
            {
                string msg = ig.ApplyLobbyPressure(e);
                ui.WriteLog(msg);
                ui.MarkDirty("cikar");
                ui.RebuildCurrent();
            }, 50);
        }
    }
}
}