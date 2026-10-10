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
            var oppCard = ui.Card(c, LocalizationManager.Get("card_opposition_status"));
            HudKit.Label(oppCard, LocalizationManager.Get("cab_opp_note1"), 22, HudTheme.Warn);
            HudKit.Label(oppCard, LocalizationManager.Get("cab_opp_note2"), 20, HudTheme.Dim);

            if (e.ShadowCab != null && e.ShadowCab.Members.Count > 0)
            {
                var shadowCard = ui.Card(c, LocalizationManager.Get("card_your_shadow"));
                foreach (var m in e.ShadowCab.Members)
                {
                    ui.StatRow(shadowCard, $"{m.Portfolio}: {UIManager.Clean(m.Name)}",
                        $"%{m.Competence:F0}", m.Competence / 100f, HudTheme.Info, "");
                }
            }
            return;
        }

        // 1. İKTİDAR: MEVCUT BAKANLAR VE REEL ETKİLERİ
        HudKit.Label(c, LocalizationManager.Get("cabinet_members"), 28, HudTheme.Gold, TextAlignmentOptions.Left, FontStyles.Bold);
        HudKit.Label(c, $"Aktif Bakan Sayısı: {e.Actors.Count(a => a.Role == ActorRole.Minister)} / 8  |  Siyasi Sermaye: {e.PoliticalCapital:F0}", 20, HudTheme.Dim);

        var activeMinisters = e.Actors.Where(a => a.Role == ActorRole.Minister).ToList();
        if (activeMinisters.Count == 0)
        {
            var emptyCard = ui.Card(c, null);
            HudKit.Label(emptyCard, "[UYARI] Kabinede atanmış bakan yok! Aşağıdaki aday havuzundan bakan atayarak departmanları yönetin.", 22, HudTheme.Bad);
        }

        foreach (var a in activeMinisters)
        {
            var card = ui.Card(c, null);
            HudKit.Label(card, UIManager.Clean(a.Name), 26, HudTheme.Text, TextAlignmentOptions.Left, FontStyles.Bold);

            var traits = a.Traits.Select(UIManager.TraitTr).ToList();
            string meta = (string.IsNullOrEmpty(a.Portfolio) ? "Genel Bakan" : $"{a.Portfolio} Bakanı") + "   |   " +
                          UIManager.FactionTr(a.Faction) + "   |   " +
                          (a.Ideology < -20f ? LocalizationManager.Get("ideology_left") : (a.Ideology > 20f ? LocalizationManager.Get("ideology_right") : LocalizationManager.Get("ideology_center")));

            if (traits.Count > 0)
                meta += "   |   Nitelik: " + string.Join(", ", traits);

            HudKit.Label(card, meta, 19, HudTheme.Dim);

            ui.StatRow(card, "Yetkinlik", "%" + a.Competence.ToString("F0"), a.Competence / 100f,
                UIManager.GoodHigh(a.Competence, 60f, 40f), "");
            ui.StatRow(card, LocalizationManager.Get("stat_loyalty"), "%" + a.Loyalty.ToString("F0"), a.Loyalty / 100f,
                UIManager.GoodHigh(a.Loyalty, 60f, 40f), ui.DeltaStr("act:" + a.Id, true));
            ui.StatRow(card, LocalizationManager.Get("stat_satisfaction"), "%" + a.Satisfaction.ToString("F0"), a.Satisfaction / 100f,
                UIManager.GoodHigh(a.Satisfaction, 55f, 35f), "");

            // Democracy 4 tarzı reel etki rozeti
            float capDelta = (a.Competence / 100f * 2.2f) + ((a.Loyalty - 50f) / 50f * 1.2f);
            string capStr = capDelta >= 0 ? $"+{capDelta:F1}" : $"{capDelta:F1}";
            string impactSummary = $"[Reel Etki] Siyasi Sermaye: {capStr}/tur  |  Departman Verimi: %{a.Competence:F0}";
            if (traits.Count > 0) impactSummary += $"  |  Taban: {traits[0]}";
            HudKit.Label(card, impactSummary, 18, HudTheme.Info, TextAlignmentOptions.Left, FontStyles.Bold);

            if (a.Loyalty < 40f)
            {
                HudKit.Label(card, "[KRİTİK UYARI] Düşük sadakat! İstifa ve basına sızıntı riski yüksek.", 19, HudTheme.Bad, TextAlignmentOptions.Left, FontStyles.Bold);
            }

            // Bakan görevden alma butonu
            var btn = HudKit.MakeButton(card, LocalizationManager.Get("cabinet_dismiss"),
                HudTheme.Bad, Color.white, 20, () =>
            {
                string msg = e.Cabinet.DismissMinister(a.Id, e);
                ui.WriteLog(msg);
                ui.Notify(msg);
                ui.MarkDirty("kabine");
                ui.RebuildCurrent();
            }, 48);
            btn.interactable = e.PoliticalCapital >= 15f;
        }

        // 2. BOŞ BAKANLIK KONTROLÜ
        if (e.Ministry != null && e.Ministry.ActiveMinistries.Count > 0)
        {
            var assignedPortfolios = activeMinisters.Select(m => m.Portfolio).Where(p => !string.IsNullOrEmpty(p)).ToHashSet();
            var vacant = e.Ministry.ActiveMinistries.Where(m => !assignedPortfolios.Contains(m)).ToList();
            if (vacant.Count > 0)
            {
                var vacantCard = ui.Card(c, "BOŞTA KALAN BAKANLIKLAR (ATAMA BEKLİYOR)");
                foreach (var v in vacant)
                {
                    HudKit.Label(vacantCard, $"• [BOŞ] {v} Bakanlığı — Bakan atanmadığı için politikalar verimsiz (-%25)!", 19, HudTheme.Warn);
                }
            }
        }

        // 3. DEMOCRACY 4 TARZI BAKAN ADAY HAVUZU VE ATAMA MENÜSÜ
        HudKit.Label(c, "DEMOCRACY 4 TARZI BAKAN ADAY HAVUZU (ATAMA MENÜSÜ)", 26, HudTheme.Gold, TextAlignmentOptions.Left, FontStyles.Bold);
        HudKit.Label(c, "Yeni bir bakan atayarak departman verimliliğini, siyasi sermaye üretimini ve seçmen tabanı desteğini artırabilirsiniz. (Atama: 30 Sermaye)", 19, HudTheme.Dim);

        // Aday havuzu boşsa veya ilk defa açılıyorsa doldur
        if (e.Universe.MinistryMarket == null || e.Universe.MinistryMarket.Count == 0)
        {
            MinistryMarket.RefreshMarket(e);
        }

        var marketList = e.Universe.MinistryMarket;
        if (marketList != null && marketList.Count > 0)
        {
            foreach (var cand in marketList.ToList())
            {
                var candCard = ui.Card(c, null);
                string candTitle = $"{cand.Name} — {cand.Portfolio} Bakanı Adayı";
                HudKit.Label(candCard, candTitle, 24, HudTheme.Text, TextAlignmentOptions.Left, FontStyles.Bold);

                string ideoLabel = cand.Ideology < -20f ? "Solcu" : (cand.Ideology > 20f ? "Sağcı" : "Merkezci");
                string candMeta = $"Nitelik: {cand.Trait}   |   İdeoloji: {ideoLabel}   |   Uzmanlık Alanı: {cand.Portfolio}";
                HudKit.Label(candCard, candMeta, 19, HudTheme.Dim);

                ui.StatRow(candCard, "Yetkinlik", "%" + cand.Competence.ToString("F0"), cand.Competence / 100f,
                    UIManager.GoodHigh(cand.Competence, 60f, 40f), "");
                ui.StatRow(candCard, "Sadakat", "%" + cand.Loyalty.ToString("F0"), cand.Loyalty / 100f,
                    UIManager.GoodHigh(cand.Loyalty, 60f, 40f), "");

                float estCap = (cand.Competence / 100f * 2.2f) + ((cand.Loyalty - 50f) / 50f * 1.2f);
                string estCapStr = estCap >= 0 ? $"+{estCap:F1}" : $"{estCap:F1}";
                HudKit.Label(candCard, $"[Öngörülen Etki] Sermaye: {estCapStr}/tur  |  Seçmen Eğilimi: {cand.Trait}", 18, HudTheme.Info);

                // Atama butonu
                string hireText = $"BAKAN OLARAK ATA (30 Sermaye)";
                var hireBtn = HudKit.MakeButton(candCard, hireText,
                    HudTheme.Action, Color.white, 20, () =>
                {
                    string res = MinistryMarket.Hire(e, cand.Id);
                    ui.WriteLog(res);
                    ui.Notify(res);
                    ui.MarkDirty("kabine");
                    ui.RebuildCurrent();
                }, 50);
                hireBtn.interactable = e.PoliticalCapital >= MinistryMarket.HireCost;
            }

            // Aday havuzunu yenile butonu
            var refreshBtn = HudKit.MakeButton(c, "ADAY HAVUZUNU YENİLE (Yeni Adaylar Çağır)",
                HudTheme.PanelHi, HudTheme.Text, 21, () =>
            {
                MinistryMarket.RefreshMarket(e);
                ui.Notify("[Bakanlık] Aday havuzu yenilendi.");
                ui.MarkDirty("kabine");
                ui.RebuildCurrent();
            }, 54);
        }

        // 4. BAKANLIK YÖNETİMİ
        if (e.Ministry != null)
        {
            var ministryCard = ui.Card(c, LocalizationManager.Get("card_ministry_mgmt"));
            HudKit.Label(ministryCard,
                LocalizationManager.Get("cabinet_open_ministries_fmt", e.Ministry.ActiveMinistries.Count, MinistryManager.MaxMinistries),
                22, HudTheme.Text);

            foreach (var m in e.Ministry.ActiveMinistries)
            {
                HudKit.Label(ministryCard, "• " + m, 19, HudTheme.Good);
            }
        }
    }
}