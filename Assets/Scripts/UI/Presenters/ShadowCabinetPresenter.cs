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


internal sealed class ShadowCabinetPresenter : PagePresenter
{
    public ShadowCabinetPresenter(UIManager ui) : base(ui) { }

    public override void Build(RectTransform c, SimulationEngine e)
    {
        bool isOpposition = e.CurrentRole == SimulationEngine.PlayerRole.Opposition;
        
        // Bilgi kartı
        var info = ui.Card(c, null);
        if (isOpposition)
        {
            HudKit.Label(info, LocalizationManager.Get("shadow_title"), 28, HudTheme.Gold, TextAlignmentOptions.Left, FontStyles.Bold);
            HudKit.Label(info, 
                LocalizationManager.Get("shadow_desc_opp"), 
                20, HudTheme.Text);
        }
        else
        {
            HudKit.Label(info, LocalizationManager.Get("shadow_title_gov"), 28, HudTheme.Gold, TextAlignmentOptions.Left, FontStyles.Bold);
            HudKit.Label(info, 
                LocalizationManager.Get("shadow_desc_gov"), 
                20, HudTheme.Dim);
        }

        // Mevcut gölge bakanlar
        var sc = e.ShadowCab;
        if (sc != null && sc.Members.Count > 0)
        {
            var membersCard = ui.Card(c, LocalizationManager.Get("shadow_current_fmt", sc.Members.Count));
            foreach (var m in sc.Members)
            {
                var row = HudKit.NewRect(membersCard, "ShadowRow");
                HudKit.VStack(row.gameObject, 4, 0);

                var top = HudKit.NewRect(row, "Top");
                HudKit.HStack(top.gameObject, 12, 0, TextAnchor.MiddleLeft, false, true);

                var infoBox = HudKit.NewRect(top, "Info");
                HudKit.VStack(infoBox.gameObject, 4, 0);
                HudKit.Size(infoBox.gameObject, flexW: 1);

                HudKit.Label(infoBox, $"{m.Portfolio}: {UIManager.Clean(m.Name)}", 24, HudTheme.Text, 
                    TextAlignmentOptions.Left, FontStyles.Bold);
                HudKit.Label(infoBox, 
                    LocalizationManager.Get("shadow_stats_fmt", m.Competence.ToString("F0"), m.MediaSkill.ToString("F0"), m.Loyalty.ToString("F0")), 
                    18, HudTheme.Dim);

                // Görevden al butonu
                if (isOpposition)
                {
                    var btnDismiss = HudKit.MakeButton(top, LocalizationManager.Get("shadow_dismiss"), HudTheme.Bad, Color.white, 18, () =>
                    {
                        string msg = e.ShadowCab.DismissShadowMinister(m.Id, e);
                        ui.WriteLog(msg);
                        ui.MarkDirty("golge");
                        ui.RebuildCurrent();
                    }, 44);
                    HudKit.Size(btnDismiss.gameObject, prefW: 160);
                    btnDismiss.interactable = e.PoliticalCapital >= 10f;
                }
            }

            // Ekstra bilgi
            HudKit.Label(membersCard, 
                LocalizationManager.Get("shadow_bonus_fmt", sc.Members.Count * 2), 
                19, HudTheme.Good);
        }

        // Atama butonları (sadece muhalefetteyken)
        if (isOpposition && sc != null && sc.Members.Count < 3)
        {
            var appointCard = ui.Card(c, LocalizationManager.Get("shadow_appoint_card"));
            HudKit.Label(appointCard, 
                LocalizationManager.Get("shadow_cost_fmt", ShadowCabinet.AppointmentCost.ToString("F0"), e.PoliticalCapital.ToString("F0")), 
                20, HudTheme.Dim);

            string[] portfolios = { "Ekonomi", "Adalet", "Sosyal" };
            Func<string,string> PortfolioLabel = pf => pf == "Ekonomi" ? LocalizationManager.Get("pf_economy") : (pf == "Adalet" ? LocalizationManager.Get("pf_justice") : (pf == "Sosyal" ? LocalizationManager.Get("pf_social") : pf));
            foreach (var p in portfolios)
            {
                // Bu portföy zaten dolu mu?
                bool occupied = sc.Members.Any(m => m.Portfolio == p);
                if (occupied) continue;

                var btn = HudKit.MakeButton(appointCard, LocalizationManager.Get("shadow_appoint_btn_fmt", PortfolioLabel(p)), 
                    HudTheme.Action, Color.white, 22, () =>
                {
                    string msg = e.ShadowCab.AppointShadowMinister(p, e);
                    ui.WriteLog(msg);
                    ui.MarkDirty("golge");
                    ui.MarkDirty("genel");
                    ui.RebuildCurrent();
                }, 52);

                btn.interactable = e.PoliticalCapital >= ShadowCabinet.AppointmentCost;
            }
        }
    }
}