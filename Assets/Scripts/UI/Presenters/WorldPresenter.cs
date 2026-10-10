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


internal sealed class WorldPresenter : PagePresenter
{
    public WorldPresenter(UIManager ui) : base(ui) { }

    public override void Build(RectTransform c, SimulationEngine e)
    {
        var target = ui.Card(c, "HEDEF ÜLKE");
        ui.diplomacyTargets = HudKit.NewRect(target, "Targets");
        HudKit.VStack(ui.diplomacyTargets.gameObject, 8, 0);

        var info = ui.Card(c, "İLİŞKİ VE CASUS AĞI");
        ui.relationLabel = HudKit.Label(info, "İlişki: -", 30, HudTheme.Text, TextAlignmentOptions.Left, FontStyles.Bold);
        ui.relationBarHolder = HudKit.NewRect(info, "RelationBar");
        HudKit.VStack(ui.relationBarHolder.gameObject, 0, 0);
        ui.networkLabel = HudKit.Label(info, "Casus Ağı Gücü: -", 23, HudTheme.Dim);

        // FAZ 0: Seçili AI ülkenin iç durumu (boş kart, sonra doldurulur)
        var details = ui.Card(c, "ÜLKE İÇ DURUMU");
        ui.countryDetailHolder = HudKit.NewRect(details, "Details");
        HudKit.VStack(ui.countryDetailHolder.gameObject, 6, 0);
        HudKit.Label(ui.countryDetailHolder, "Yukarıdan bir ülke seçin.", 20, HudTheme.Dim);

        var ops = ui.Card(c, "GİZLİ OPERASYONLAR");
        HudKit.Label(ops, "Başarısız operasyon ilişkileri bozar ve meşruiyetinize zarar verir.", 19, HudTheme.Dim);
        ui.intelButton = HudKit.MakeButton(ops, "Casusluk: gizli verileri ele geçir", HudTheme.Action, Color.white, 23, null, 60);
        ui.sabotageButton = HudKit.MakeButton(ops, "Sabotaj: altyapıya zarar ver", HudTheme.Action, Color.white, 23, null, 60);
        ui.manipulateButton = HudKit.MakeButton(ops, "Manipülasyon: halkı etkile", HudTheme.Action, Color.white, 23, null, 60);
        ui.closeDiplomacyButton = HudKit.MakeButton(ops, "Genel ekrana dön", HudTheme.Line, HudTheme.Dim, 21, null, 52);
    }
}
