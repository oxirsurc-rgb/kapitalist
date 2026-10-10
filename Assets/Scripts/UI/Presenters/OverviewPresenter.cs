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
        ui.StatCard(row, opp ? "HALK DESTEĞİ" : "MEŞRUİYET", legit.ToString("F1") + "%", legit / 100f, UIManager.GoodHigh(legit, 55f, 35f), ui.DeltaStr("legit", true));
        ui.StatCard(row, "SİYASİ SERMAYE", e.PoliticalCapital.ToString("F0"), e.PoliticalCapital / Mathf.Max(1f, e.MaxPoliticalCapital), HudTheme.Gold, ui.DeltaStr("capital", true));
        ui.StatCard(row, "SOKAK HUZURSUZLUĞU", e.Universe.Unrest.ToString("F0"), e.Universe.Unrest / 100f, UIManager.GoodLow(e.Universe.Unrest, 25f, 55f), ui.DeltaStr("unrest", false));
        ui.StatCard(row, "ORDU SADAKATİ", "%" + e.Army.ArmySatisfaction.ToString("F0"), e.Army.ArmySatisfaction / 100f, UIManager.GoodHigh(e.Army.ArmySatisfaction, 55f, 30f), ui.DeltaStr("army", true));

        if (e.CrisisChains != null && e.CrisisChains.ActiveCrises.Count > 0)
{
    var crisisCard = ui.Card(c, "AKTİF KRİZLER");
    foreach (var crisis in e.CrisisChains.ActiveCrises)
    {
        var crisisRow = HudKit.NewRect(crisisCard, "CrisisRow");
        HudKit.VStack(crisisRow.gameObject, 4, 0);    // ✅ crisisRow kullan
        var top = HudKit.NewRect(crisisRow, "Top");   // ✅ crisisRow kullan
        HudKit.HStack(top.gameObject, 8, 0, TextAnchor.MiddleLeft, false, true);

        string stageStr = $"Aşama {crisis.CurrentStage + 1}/{crisis.TotalStages}";
        HudKit.Label(top, UIManager.Clean(crisis.Title), 22, HudTheme.Bad,
            TextAlignmentOptions.Left, FontStyles.Bold);
        HudKit.Label(top, stageStr, 18, HudTheme.Warn, TextAlignmentOptions.Right);

        HudKit.Label(crisisRow, UIManager.Clean(crisis.StageDescription), 18, HudTheme.Dim);

        int turnsLeft = CrisisChainManager.CrisisAutoResolveTurns - crisis.TurnsSinceStart;
        float progress = 1f - ((float)turnsLeft / CrisisChainManager.CrisisAutoResolveTurns);
        HudKit.Label(crisisRow, $"Otomatik çözülme: {turnsLeft} tur", 17,
            turnsLeft <= 1 ? HudTheme.Bad : HudTheme.Warn);
        HudKit.Bar(crisisRow, progress, turnsLeft <= 1 ? HudTheme.Bad : HudTheme.Warn, 8f);
    }
}

        // FAZ 3: Parti Fonu göstergesi (muhalefetteyken önemli)
        if (e.CurrentRole == SimulationEngine.PlayerRole.Opposition && e.Party != null)
        {
            var partyCard = ui.Card(c, "PARTİ FONU (Muhalefet)");
            float fund = e.Party.Fund;
            float maxFund = e.Party.MaxFund;
            Color fundColor = fund < 30f ? HudTheme.Bad : (fund < 100f ? HudTheme.Warn : HudTheme.Good);

            ui.StatRow(partyCard, "Fon", 
                $"{fund:F0} / {maxFund:F0}", 
                fund / maxFund, 
                fundColor, "");

            float net = e.Party.NetIncome;
            ui.StatRow(partyCard, "Net Gelir", 
                $"{(net >= 0 ? "+" : "")}{net:F0}/tur", 
                Mathf.Clamp01(Mathf.Abs(net) / 10f), 
                net >= 0 ? HudTheme.Good : HudTheme.Bad, "");

            string advice = fund < 30f 
                ? "Fon kritik seviyede. Bağış kampanyası başlat!"
                : fund < 100f 
                    ? "Fon düşük. Devlet yardımı alabilirsin." 
                    : "Fon sağlıklı.";
            HudKit.Label(partyCard, advice, 19, fundColor);
        }
                // FAZ 3 Adım 5: Yargı durumu
        if (e.CurrentRole == SimulationEngine.PlayerRole.Opposition && e.Judiciary != null)
        {
            var judCard = ui.Card(c, "YARGI SISTEMI");
            float indep = e.Judiciary.JudicialIndependence;
            Color indepColor = UIManager.GoodHigh(indep, 60f, 40f);
            ui.StatRow(judCard, "Yargi Bagimsizligi", 
                "%" + indep.ToString("F0"), indep / 100f, indepColor, "");
            HudKit.Label(judCard, e.Judiciary.GetStatusText(), 20, HudTheme.Text);
        }
                // FAZ 3 Adım 4: Protesto durumu (muhalefetteyken)
        if (e.CurrentRole == SimulationEngine.PlayerRole.Opposition && e.Protest != null)
        {
            var protestCard = ui.Card(c, "PROTESTO DURUMU");
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
                    $"Tema: {themeStr}   |   Yoğunluk: %{intensity:F0}   |   Kalan: {e.Protest.Current.TurnsLeft} tur", 
                    20, HudTheme.Text);
                HudKit.Label(protestCard, 
                    "Her tur hükümet meşruiyeti düşüyor ve sokaklar hareketleniyor.", 
                    18, HudTheme.Dim);
            }
            else if (e.Protest.CooldownTurns > 0)
            {
                HudKit.Label(protestCard, $"Yeni protesto için {e.Protest.CooldownTurns} tur beklemelisin.", 19, HudTheme.Dim);
            }
            else
            {
                HudKit.Label(protestCard, 
                    "Protesto hazır. 'Protesto Düzenle' butonundan başlatabilirsin.", 
                    19, HudTheme.Dim);
            }
        }

    
        var row2 = ui.Row(c);
        var groupsCard = ui.Card(row2, "HALK GRUPLARI", -1, 3);
        foreach (var g in e.Demographics)
        {
            bool radical = UIManager.IsRadical(e, g.Id);
            string name = UIManager.Clean(g.Name) + (radical ? "  (RADİKAL)" : "");
            ui.StatRow(groupsCard, name, "%" + g.Satisfaction.ToString("F0"), g.Satisfaction / 100f,
                    radical ? HudTheme.Bad : UIManager.GoodHigh(g.Satisfaction, 55f, 35f), ui.DeltaStr("grp:" + g.Id, true));
        }

        var stateCard = ui.Card(row2, "DEVLET DURUMU", -1, 2);
        List<string> lines = null;
        try { lines = UniverseSystems.StatusReport(e); } catch (Exception ex) { Debug.LogWarning("[UI] StatusReport: " + ex.Message); }
        if (lines != null)
            foreach (var l in lines) HudKit.Label(stateCard, UIManager.Clean(l), 20, HudTheme.Dim);

        // Yasama süreci
        var legis = ui.Card(c, "YASAMA SÜRECİ");
        bool any = false;
        foreach (var p in e.ProposedPolicies)
        {
            any = true;
            HudKit.Label(legis, "Meclis gündeminde: " + UIManager.Clean(p.Name) + "  (sonraki turda oylanacak)", 21, HudTheme.Text);
        }
        foreach (var pend in e.Universe.Pending)
        {
            any = true;
            HudKit.Label(legis, "Bürokraside: " + UIManager.Clean(UIManager.PolicyName(e, pend.PolicyId)) + "  (" + pend.TurnsLeft + " tur kaldı)", 21, HudTheme.Warn);
        }
        if (!any) HudKit.Label(legis, "Bekleyen yasa yok.", 21, HudTheme.Dim);
    }
}
