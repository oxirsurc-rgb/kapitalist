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
            HudKit.Label(note, LocalizationManager.Get("pol_opp_note"), 22, HudTheme.Warn);
        }

        var policies = (ui.policyCache != null && ui.policyCache.Count > 0) ? ui.policyCache : e.AllObjects.OfType<SimPolicy>().ToList();
        if (policies.Count == 0) { HudKit.Label(c, LocalizationManager.Get("pol_none"), 24, HudTheme.Dim); return; }

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

            // FAZ Geliştirme: Politika Etki Öngörüsü (Impact Forecast)
            if (pol.OutgoingEffects.Count > 0)
            {
                var impacts = new List<string>();
                foreach (var eff in pol.OutgoingEffects.Take(3))
                {
                    string targetName = UIManager.Clean(eff.Target?.Name ?? eff.Target?.Id ?? "");
                    float sample = eff.Strength * 5f;
                    string sign = sample >= 0 ? "+" : "";
                    impacts.Add($"{targetName} {sign}{sample:F1}");
                }
                if (impacts.Count > 0)
                {
                    HudKit.Label(card, "[Tahmin (+5)]: " + string.Join(" | ", impacts), 18, HudTheme.Info, TextAlignmentOptions.Left);
                }
            }
        }
    }
}