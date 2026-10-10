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


internal sealed class PeoplePresenter : PagePresenter
{
    public PeoplePresenter(UIManager ui) : base(ui) { }

    public override void Build(RectTransform c, SimulationEngine e)
    {
        foreach (var g in e.Demographics)
        {
            bool radical = UIManager.IsRadical(e, g.Id);
            var card = ui.Card(c, UIManager.Clean(g.Name));
            ui.StatRow(card, "Memnuniyet", "%" + g.Satisfaction.ToString("F0"), g.Satisfaction / 100f,
                    radical ? HudTheme.Bad : UIManager.GoodHigh(g.Satisfaction, 55f, 35f), ui.DeltaStr("grp:" + g.Id, true));
            string meta = "Siyasi ağırlık: %" + (g.Influence * 100f).ToString("F0");
            int low;
            if (e.Universe.LowSatTurns.TryGetValue(g.Id, out low) && low > 0) meta += "   |   düşük memnuniyet: " + low + " tur";
            HudKit.Label(card, meta, 19, HudTheme.Dim);
            if (radical) HudKit.Label(card, "RADİKALLEŞTİ: her tur memnuniyet ve meşruiyet kaybettiriyor.", 20, HudTheme.Bad);
        }

        if (e.CurrentRole == SimulationEngine.PlayerRole.Governing && e.Universe.Partners.Count > 0)
        {
            var coal = ui.Card(c, "KOALİSYON ORTAKLARI");
            foreach (var p in e.Universe.Partners)
            {
                string status = p.InGovernment ? "hükümette" : "koalisyon dışı";
                string side = p.Ideology < -20f ? "sol" : (p.Ideology > 20f ? "sağ" : "merkez");
                ui.StatRow(coal, UIManager.Clean(p.Name) + "  (" + side + ", " + status + ")", "%" + p.Satisfaction.ToString("F0"), p.Satisfaction / 100f,
                        p.InGovernment ? UIManager.GoodHigh(p.Satisfaction, 50f, 25f) : HudTheme.Dim, "");
            }
        }

          // FAZ 3.5: Halk grupları heatmap — renk kodlu memnuniyet matrisi
var heatmapCard = ui.Card(c, "MEMNUNİYET ISIL HARİTASI");
var heatmapGrid = HudKit.NewRect(heatmapCard, "HeatmapGrid");
var grid = heatmapGrid.gameObject.AddComponent<GridLayoutGroup>();
grid.cellSize = new Vector2(140, 50);
grid.spacing = new Vector2(4, 4);
grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
grid.constraintCount = 3;
grid.childAlignment = TextAnchor.UpperLeft;

foreach (var g in e.Demographics)
{
    float sat = g.Satisfaction;
    Color cellColor = sat >= 70f ? HudTheme.Good
                   : sat >= 50f ? HudTheme.Warn
                   : sat >= 30f ? HudTheme.Hex("E8A33D")
                   : HudTheme.Bad;

    var cell = HudKit.Box(heatmapGrid, "Cell", cellColor);
    HudKit.VStack(cell.gameObject, 2, 6, TextAnchor.MiddleCenter);
    HudKit.Label(cell.transform, UIManager.Clean(g.Name), 16, Color.white, 
        TextAlignmentOptions.Center, FontStyles.Bold);
    HudKit.Label(cell.transform, $"%{sat:F0}", 22, Color.white, 
        TextAlignmentOptions.Center, FontStyles.Bold);
    
}

// FAZ 3: Göç durumu
if (e.Migration != null)
{
    var migCard = ui.Card(c, "GÖÇ VE DİASPORA");
    
    float migPop = e.Migration.TotalMigrantPopulation;
    float integ = e.Migration.IntegrationScore;
    Color integColor = integ >= 70f ? HudTheme.Good
                    : integ >= 50f ? HudTheme.Info
                    : integ >= 30f ? HudTheme.Warn
                    : HudTheme.Bad;
    
    ui.StatRow(migCard, "Göçmen Nüfusu", $"%{migPop:F1}", migPop / 100f, HudTheme.Info, "");
    ui.StatRow(migCard, $"Entegrasyon ({e.Migration.GetIntegrationLabel()})", 
        $"%{integ:F0}", integ / 100f, integColor, "");
    
    float netFlow = e.Universe.MigrationBalance;
    string flowText = netFlow >= 0 ? $"Net Göç Alan (+{netFlow:F1})" : $"Net Göç Veren ({netFlow:F1})";
    Color flowColor = netFlow >= 0 ? HudTheme.Good : HudTheme.Bad;
    HudKit.Label(migCard, flowText, 20, flowColor);
    
    if (e.Migration.PendingRemittance > 0f)
        HudKit.Label(migCard, $"Bekleyen Diaspora Havalesi: {e.Migration.PendingRemittance:F1}", 
            19, HudTheme.Gold);
    
    // Entegrasyon düşükse uyarı
    if (integ < 30f)
        HudKit.Label(migCard, "⚠️ DÜŞÜK ENTEGRASYON: Sosyal gerginlik artıyor!", 20, HudTheme.Bad);
}

        // FAZ 3.5: Radikalleşme kademeleri
var radCard = ui.Card(c, "RADİKALLEŞME DURUMU");
foreach (var g in e.Demographics)


{
    var tier = e.Universe.RadicalizationTiers.GetValueOrDefault(g.Id, 
        RadicalizationHelper.GetTier(g.Satisfaction));
    Color tierColor = tier switch
    {
        RadicalizationTier.Satisfied    => HudTheme.Good,
        RadicalizationTier.Dissatisfied => HudTheme.Dim,
        RadicalizationTier.Angry        => HudTheme.Warn,
        RadicalizationTier.Radical      => HudTheme.Bad,
        RadicalizationTier.Insurgent    => HudTheme.Hex("8B2A2A"),
        _ => HudTheme.Dim
    };
    ui.StatRow(radCard, g.Name, RadicalizationHelper.GetLabel(tier), 
        (int)tier / 4f, tierColor, "");
}

    }
}
