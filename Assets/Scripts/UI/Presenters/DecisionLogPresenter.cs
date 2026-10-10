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


internal sealed class DecisionLogPresenter : PagePresenter
{
    public DecisionLogPresenter(UIManager ui) : base(ui) { }

    public override void Build(RectTransform c, SimulationEngine e)
{
    HudKit.Label(c, "KARAR GÜNLÜĞÜ", 28, HudTheme.Gold, 
        TextAlignmentOptions.Left, FontStyles.Bold);
    HudKit.Label(c, "Son 20 kararınız ve etkileri.", 20, HudTheme.Dim);

    if (e.DecisionLog == null || e.DecisionLog.Count == 0)
    {
        HudKit.Label(c, "Henüz kayıtlı karar yok.", 22, HudTheme.Dim);
        return;
    }

    foreach (var entry in e.DecisionLog)
    {
        var card = ui.Card(c, null);
        var top = HudKit.NewRect(card, "Top");
        HudKit.HStack(top.gameObject, 8, 0, TextAnchor.MiddleLeft, false, true);

        var turnLabel = HudKit.Label(top, $"Tur {entry.Turn}", 20, HudTheme.Gold, 
            TextAlignmentOptions.Left, FontStyles.Bold);
        HudKit.Size(turnLabel.gameObject, prefW: 80);

        var catLabel = HudKit.Label(top, $"[{entry.Category}]", 19, HudTheme.Info);
        HudKit.Size(catLabel.gameObject, prefW: 120);

        HudKit.Label(card, entry.Description, 21, HudTheme.Text);

        if (Math.Abs(entry.Impact) > 0.01f)
        {
            Color impactColor = entry.Impact >= 0 ? HudTheme.Good : HudTheme.Bad;
            HudKit.Label(card, $"Etki: {entry.Impact:+0.0;-0.0}", 19, impactColor);
        }
    }
}
}
