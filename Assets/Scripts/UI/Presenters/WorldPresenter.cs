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
using DemocracySim.Engine.UI;

// R-REFACTOR: UIManager artık bir "kabuk": tuval, sekmeler, modal ve toast yönetimi.
// Sayfa içerikleri ilgili Presenter sınıflarında kurulur. Davranış değişmedi.


internal sealed class WorldPresenter : PagePresenter
{
    public WorldPresenter(UIManager ui) : base(ui) { }

    public override void Build(RectTransform c, SimulationEngine e)
    {
        var target = ui.Card(c, LocalizationManager.Get("world_target"));
        ui.diplomacyTargets = HudKit.NewRect(target, "Targets");
        HudKit.VStack(ui.diplomacyTargets.gameObject, 8, 0);

        var info = ui.Card(c, LocalizationManager.Get("world_relations_card"));
        ui.relationLabel = HudKit.Label(info, LocalizationManager.Get("diplo_relation_none"), 30, HudTheme.Text, TextAlignmentOptions.Left, FontStyles.Bold);
        ui.relationBarHolder = HudKit.NewRect(info, "RelationBar");
        HudKit.VStack(ui.relationBarHolder.gameObject, 0, 0);
        ui.networkLabel = HudKit.Label(info, LocalizationManager.Get("diplo_network_none"), 23, HudTheme.Dim);

        // FAZ 0: Seçili AI ülkenin iç durumu (boş kart, sonra doldurulur)
        var details = ui.Card(c, LocalizationManager.Get("world_internal"));
        ui.countryDetailHolder = HudKit.NewRect(details, "Details");
        HudKit.VStack(ui.countryDetailHolder.gameObject, 6, 0);
        HudKit.Label(ui.countryDetailHolder, LocalizationManager.Get("world_pick_above"), 20, HudTheme.Dim);

        var ops = ui.Card(c, LocalizationManager.Get("world_covert"));
        HudKit.Label(ops, LocalizationManager.Get("world_covert_warn"), 19, HudTheme.Dim);
        ui.intelButton = HudKit.MakeButton(ops, LocalizationManager.Get("world_btn_intel"), HudTheme.Action, Color.white, 23, null, 60);
        ui.sabotageButton = HudKit.MakeButton(ops, LocalizationManager.Get("world_btn_sabotage"), HudTheme.Action, Color.white, 23, null, 60);
        ui.manipulateButton = HudKit.MakeButton(ops, LocalizationManager.Get("world_btn_manipulate"), HudTheme.Action, Color.white, 23, null, 60);
        ui.closeDiplomacyButton = HudKit.MakeButton(ops, LocalizationManager.Get("world_btn_back"), HudTheme.Line, HudTheme.Dim, 21, null, 52);
        // ═══════════════════════════════════════════════════════════════
        // FAZ 13: Ülke karşılaştırma — Radar chart (oyuncu vs seçili)
        // ═══════════════════════════════════════════════════════════════
        var compareCard = ui.Card(c, "ÜLKE KARŞILAŞTIRMA");
        ui.radarChartHolder = HudKit.NewRect(compareCard, "RadarHolder");
        HudKit.VStack(ui.radarChartHolder.gameObject, 4, 0);

        // KÜRESEL TİCARET VE JEOPOLİTİK AĞ
        var geoCard = ui.Card(c, "KÜRESEL TİCARET VE JEOPOLİTİK AĞ");
        HudKit.VStack(geoCard.gameObject, 6, 8);
        if (ui.lastCountry != null && ui.lastCountry.Engine != null)
        {
            var eng = ui.lastCountry.Engine;
            var partners = eng.Universe.TradePartners;
            string partnerText = partners.Count > 0 
                ? string.Join(", ", partners.Select(p => p.ToUpper()))
                : "Aktif ikili ticaret anlaşması yok";
            
            HudKit.Label(geoCard, $"<b>Ticaret Ortakları ({partners.Count}):</b> {partnerText}", 18, HudTheme.Good);
            
            float sanctions = eng.Universe.SanctionLevel;
            Color sancCol = sanctions > 30f ? HudTheme.Bad : (sanctions > 10f ? HudTheme.Warn : HudTheme.Dim);
            HudKit.Label(geoCard, $"<b>Uluslararası Yaptırım Baskısı:</b> %{sanctions:F0}", 18, sancCol);
            
            float alignment = ui.lastCountry.GlobalAlignment;
            string bloc = alignment < -25f ? "Doğu / Çok Kutuplu Blok" : (alignment > 25f ? "Batı / Atlantik Bloku" : "Bağlantısızlar / Bağımsız");
            HudKit.Label(geoCard, $"<b>Küresel İdeolojik Konumlanma:</b> {bloc} ({alignment:+0.0;-0.0})", 18, HudTheme.Gold);
        }

        // Radar chart dolduracak public metot
        PopulateRadarChart();
    }

    private void PopulateRadarChart()
{
    if (ui.radarChartHolder == null) return;
    if (ui.lastCountry == null || ui.selectedCountryDetail == null) return;

    var player = ui.lastCountry.Engine;
    var other = ui.selectedCountryDetail.Engine;

    var axesPlayer = new List<ChartKit.RadarAxis>
    {
        new ChartKit.RadarAxis("Meşruiyet", player.Legitimacy.CurrentLegitimacy / 100f),
        new ChartKit.RadarAxis("GSYİH",      player.Registry.GetValue(ObjectRegistry.Ids.Gdp, 50f) / 100f),
        new ChartKit.RadarAxis("Ordu",       player.Army.ArmySatisfaction / 100f),
        new ChartKit.RadarAxis("Eğitim",     player.Registry.GetValue(ObjectRegistry.Ids.EducationLevel, 50f) / 100f),
        new ChartKit.RadarAxis("Teknoloji",  player.Registry.GetValue(ObjectRegistry.Ids.TechLevel, 50f) / 100f),
        new ChartKit.RadarAxis("Refah",      (100f - player.Registry.GetValue(ObjectRegistry.Ids.PovertyRate, 50f)) / 100f)
    };

    var axesOther = new List<ChartKit.RadarAxis>
    {
        new ChartKit.RadarAxis("Meşruiyet", other.Legitimacy.CurrentLegitimacy / 100f),
        new ChartKit.RadarAxis("GSYİH",      other.Registry.GetValue(ObjectRegistry.Ids.Gdp, 50f) / 100f),
        new ChartKit.RadarAxis("Ordu",       other.Army.ArmySatisfaction / 100f),
        new ChartKit.RadarAxis("Eğitim",     other.Registry.GetValue(ObjectRegistry.Ids.EducationLevel, 50f) / 100f),
        new ChartKit.RadarAxis("Teknoloji",  other.Registry.GetValue(ObjectRegistry.Ids.TechLevel, 50f) / 100f),
        new ChartKit.RadarAxis("Refah",      (100f - other.Registry.GetValue(ObjectRegistry.Ids.PovertyRate, 50f)) / 100f)
    };

    HudKit.ClearChildren(ui.radarChartHolder);
    ChartKit.DrawRadarChart(ui.radarChartHolder, axesPlayer, axesOther, 90f,
        ui.lastCountry.Name, ui.selectedCountryDetail.Name);
}
}