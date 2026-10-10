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


internal sealed class CabinetPresenter : PagePresenter
{
    public CabinetPresenter(UIManager ui) : base(ui) { }

    public override void Build(RectTransform c, SimulationEngine e)
    {
        Debug.Log($"[Kabine] BuildCabinetPage çağrıldı. Rol: {e.CurrentRole}, Aktör sayısı: {e.Actors.Count}");

        bool isOpposition = e.CurrentRole == SimulationEngine.PlayerRole.Opposition;

        // MUHALEFET: İktidarın kabinesini göremez, gölge kabinesini görür
        if (isOpposition)
        {
            var oppCard = ui.Card(c, "MUHALEFET DURUMU");
            HudKit.Label(oppCard, "Muhalefettesiniz. İktidarın kabinesini göremezsiniz.", 22, HudTheme.Warn);
            HudKit.Label(oppCard, "Gölge Kabine sekmesinden kendi ekibinizi kurabilirsiniz.", 20, HudTheme.Dim);

            if (e.ShadowCab != null && e.ShadowCab.Members.Count > 0)
            {
                var shadowCard = ui.Card(c, "GÖLGE KABİNENİZ");
                foreach (var m in e.ShadowCab.Members)
                {
                    ui.StatRow(shadowCard, $"{m.Portfolio}: {UIManager.Clean(m.Name)}",
                        $"%{m.Competence:F0}", m.Competence / 100f, HudTheme.Info, "");
                }
            }
            return;
        }

        // İKTİDAR: Kabineyi göster
        if (e.Actors.Count == 0)
        {
            HudKit.Label(c, "Kabinede kimse yok. Aktör verisi yüklenmemiş olabilir.", 24, HudTheme.Bad);
            return;
        }

        HudKit.Label(c, "KABİNE ÜYELERİ", 28, HudTheme.Gold, TextAlignmentOptions.Left, FontStyles.Bold);
        HudKit.Label(c, $"Toplam: {e.Actors.Count} kişi", 20, HudTheme.Dim);

        foreach (var a in e.Actors)
        {
            var card = ui.Card(c, null);
            HudKit.Label(card, UIManager.Clean(a.Name), 27, HudTheme.Text, TextAlignmentOptions.Left, FontStyles.Bold);

            var traits = a.Traits.Select(UIManager.TraitTr).ToList();
            string meta = UIManager.RoleTr(a.Role) + "   |   " + UIManager.FactionTr(a.Faction) + "   |   " +
                          (a.Ideology < -20f ? "Sol" : (a.Ideology > 20f ? "Sağ" : "Merkez"));

            if (!string.IsNullOrEmpty(a.Portfolio))
                meta += "   |   Portföy: " + a.Portfolio;
            if (traits.Count > 0)
                meta += "   |   " + string.Join(", ", traits);

            HudKit.Label(card, meta, 19, HudTheme.Dim);

            ui.StatRow(card, "Sadakat", "%" + a.Loyalty.ToString("F0"), a.Loyalty / 100f,
                UIManager.GoodHigh(a.Loyalty, 60f, 40f), ui.DeltaStr("act:" + a.Id, true));
            ui.StatRow(card, "Memnuniyet", "%" + a.Satisfaction.ToString("F0"), a.Satisfaction / 100f,
                UIManager.GoodHigh(a.Satisfaction, 55f, 35f), "");

            // Bakan için görevden alma butonu
            if (a.Role == ActorRole.Minister)
            {
                var btn = HudKit.MakeButton(card, "Görevden Al (15 Sermaye)",
                    HudTheme.Bad, Color.white, 20, () =>
                {
                    string msg = e.Cabinet.DismissMinister(a.Id, e);
                    ui.WriteLog(msg);
                    ui.MarkDirty("kabine");
                    ui.RebuildCurrent();
                }, 50);
                btn.interactable = e.PoliticalCapital >= 15f;
            }
        }

        // BAKANLIK YÖNETİMİ
        if (e.Ministry != null)
        {
            var ministryCard = ui.Card(c, "BAKANLIK YÖNETİMİ");
            HudKit.Label(ministryCard,
                $"Açık Bakanlıklar: {e.Ministry.ActiveMinistries.Count}/{MinistryManager.MaxMinistries}",
                22, HudTheme.Text);

            foreach (var m in e.Ministry.ActiveMinistries)
            {
                HudKit.Label(ministryCard, "• " + m, 19, HudTheme.Good);
            }
        }
    }
}
