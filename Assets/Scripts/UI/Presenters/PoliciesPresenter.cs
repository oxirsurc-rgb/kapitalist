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


internal sealed class PoliciesPresenter : PagePresenter
{
    public PoliciesPresenter(UIManager ui) : base(ui) { }

    public override void Build(RectTransform c, SimulationEngine e)
    {
        bool opp = e.CurrentRole == SimulationEngine.PlayerRole.Opposition;
        if (opp)
        {
            var note = ui.Card(c, null);
            HudKit.Label(note, "Muhalefettesiniz: yasa öneremezsiniz. Miting, skandal ve kampanya ile iktidarı hedefleyin.", 22, HudTheme.Warn);
        }

        var policies = (ui.policyCache != null && ui.policyCache.Count > 0) ? ui.policyCache : e.AllObjects.OfType<SimPolicy>().ToList();
        if (policies.Count == 0) { HudKit.Label(c, "Henüz yasa yok.", 24, HudTheme.Dim); return; }

        foreach (var p in policies)
        {
            var pol = p;
            var card = ui.Card(c, null);
            var top = HudKit.NewRect(card, "Top");
            HudKit.HStack(top.gameObject, 12, 0, TextAnchor.MiddleLeft, false, true);

            var info = HudKit.NewRect(top, "Info");
            HudKit.VStack(info.gameObject, 4, 0);
            HudKit.Size(info.gameObject, flexW: 1);
            HudKit.Label(info, UIManager.Clean(pol.Name), 27, HudTheme.Text, TextAlignmentOptions.Left, FontStyles.Bold);
            HudKit.Label(info, ui.PolicyTags(e, pol), 19, HudTheme.Dim);

            var val = HudKit.Label(top, pol.ActualValue.ToString("F1"), 34, HudTheme.Gold, TextAlignmentOptions.Center, FontStyles.Bold);
            HudKit.Size(val.gameObject, prefW: 110);

            var minus = ui.MiniButton(top, "-5", () => { if (ui.onPolicyChange != null) ui.onPolicyChange(pol.Id, -5f); });
            var plus = ui.MiniButton(top, "+5", () => { if (ui.onPolicyChange != null) ui.onPolicyChange(pol.Id, 5f); });
            minus.interactable = !opp;
            plus.interactable = !opp;
            ui.MiniButton(top, "?", () => { if (ui.onPolicyAdvice != null) ui.onPolicyAdvice(pol.Id); });

            float range = Mathf.Max(0.01f, pol.MaxValue - pol.MinValue);
            HudKit.Bar(card, (pol.ActualValue - pol.MinValue) / range, pol.IsActive ? HudTheme.Gold : HudTheme.Dim, 8f);
        }
    }
}
