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


internal sealed class ParliamentPresenter : PagePresenter
{
    public ParliamentPresenter(UIManager ui) : base(ui) { }

    public override void Build(RectTransform c, SimulationEngine e)
    {
        if (e.PartyManager == null) { HudKit.Label(c, "Meclis verisi yok.", 24, HudTheme.Dim); return; }

        var pm = e.PartyManager;
        int totalSeats = pm.Parties.Sum(p => p.Seats);

        // Başlık
        HudKit.Label(c, "MECLİS", 32, HudTheme.Gold, TextAlignmentOptions.Left, FontStyles.Bold);
        HudKit.Label(c, $"Toplam {totalSeats} sandalye   |   Baraj: %{PartyManager.ElectionThreshold:F0}",
            22, HudTheme.Dim);

        // Oyuncu sandalyesi
        var playerParty = pm.Parties.FirstOrDefault(p => p.Id == "player");
                // FAZ 2: KOALİSYON YÖNETİMİ — sadece iktidardayken
        if (playerParty != null && e.CurrentRole == SimulationEngine.PlayerRole.Governing)
        {
            bool hasMajority = playerParty.Seats > totalSeats / 2;
            bool hasCoalition = pm.HasCoalitionMajority();

            // Koalisyon durumu
            var coalitionCard = ui.Card(c, "KOALİSYON DURUMU");
            // ... (mevcut kod aynı kalır)
        }
               else if (playerParty != null && e.CurrentRole == SimulationEngine.PlayerRole.Opposition)
        {
            // Muhalefetteyken bilgi mesajı
            var oppCard = ui.Card(c, "MUHALEFET DURUMU");
            HudKit.Label(oppCard, 
                $"Partiniz {playerParty.Seats} sandalyeye sahip. Muhalefettesiniz.", 
                22, HudTheme.Warn);
            HudKit.Label(oppCard, 
                "Koalisyon hükümeti kurmak için seçim kazanmalısınız.", 
                20, HudTheme.Dim);

            // FAZ 2 AŞAMA D: Güvensizlik önergesi durumu
            if (e.PartyManager.IsVoteOfNoConfidenceActive)
            {
                var ncbCard = ui.Card(c, "⚠️ GÜVENSİZLİK ÖNERGESİ AKTİF");
                HudKit.Label(ncbCard, 
                    $"Oylamaya {e.PartyManager.NoConfidenceTurnsLeft} tur kaldı.", 
                    24, HudTheme.Warn, TextAlignmentOptions.Left, FontStyles.Bold);
                HudKit.Label(ncbCard, 
                    $"Muhalefet desteği: %{e.PartyManager.CalculateOppositionSupport(e):F0}", 
                    20, HudTheme.Text);
            }
            else
            {
                float support = e.PartyManager.CalculateOppositionSupport(e);
                var ncbCard = ui.Card(c, "GÜVENSİZLİK ÖNERGESİ");
                ui.StatRow(ncbCard, "Muhalefet Desteği", 
                    $"%{support:F0}", support / 100f, 
                    UIManager.GoodHigh(support, 50f, 30f), "");
                HudKit.Label(ncbCard, 
                    "Devlet Menüsü'nden önerge verebilirsiniz.", 
                    19, HudTheme.Dim);
            }
        }

        // Meclis kompozisyonu
        var compCard = ui.Card(c, "MECLİS KOMPOZİSYONU");
        var sortedParties = pm.Parties.Where(p => p.Seats > 0).OrderByDescending(p => p.Seats).ToList();

        foreach (var party in sortedParties)
        {
            var row = HudKit.NewRect(compCard, "PartyRow");
            HudKit.VStack(row.gameObject, 4, 0);

            var top = HudKit.NewRect(row, "Top");
            HudKit.HStack(top.gameObject, 8, 0, TextAnchor.MiddleLeft, false, true);

            // Parti rengi noktası
            Color partyColor = ui.HexToColor(party.ColorHex());
            var dot = HudKit.Box(top, "Dot", partyColor, true, true);
            HudKit.Size(dot.gameObject, prefW: 16, prefH: 16, minH: 16);

            var nameLabel = HudKit.Label(top, $"{UIManager.Clean(party.Name)} ({party.SideLabel()})",
                22, HudTheme.Text, TextAlignmentOptions.Left, FontStyles.Bold);
            HudKit.Size(nameLabel.gameObject, flexW: 1);

            string govStr = party.IsInGovernment ? " [HÜKÜMET]" : "";
            var seatLabel = HudKit.Label(top, $"{party.Seats} sandalye{govStr}",
                21, partyColor, TextAlignmentOptions.Right, FontStyles.Bold);
            HudKit.Size(seatLabel.gameObject, prefW: 180);

            // Yüzdelik bar
            float pct = party.Seats / (float)totalSeats;
            HudKit.Bar(row, pct, partyColor, 8f);
        }

                // FAZ 2: KOALİSYON YÖNETİMİ
        if (playerParty != null)
        {
            bool hasMajority = playerParty.Seats > totalSeats / 2;
            bool hasCoalition = pm.HasCoalitionMajority();

            // Koalisyon durumu
            var coalitionCard = ui.Card(c, "KOALİSYON DURUMU");

            if (hasMajority)
            {
                HudKit.Label(coalitionCard, 
                    "Çoğunluğunuz var — tek başına iktidardasınız. Koalisyona gerek yok.", 
                    22, HudTheme.Good);
            }
            else if (hasCoalition)
            {
                int coalitionSeats = playerParty.Seats;
                foreach (var id in pm.CoalitionPartnerIds)
                {
                    var p = pm.Parties.FirstOrDefault(x => x.Id == id);
                    if (p != null) coalitionSeats += p.Seats;
                }
                HudKit.Label(coalitionCard, 
                    $"Koalisyon çoğunluğu var: {coalitionSeats}/{totalSeats} sandalye.", 
                    22, HudTheme.Good);
            }
            else
            {
                int needed = totalSeats / 2 + 1 - playerParty.Seats;
                HudKit.Label(coalitionCard, 
                    $"Çoğunluk yok! {needed} sandalye daha gerekli.", 
                    22, HudTheme.Bad);
            }

            // Mevcut ortaklar listesi
            if (pm.CoalitionPartnerIds.Count > 0)
            {
                HudKit.Label(coalitionCard, "MEVCUT ORTAKLAR:", 19, HudTheme.Gold, 
                    TextAlignmentOptions.Left, FontStyles.Bold);

                foreach (var id in pm.CoalitionPartnerIds.ToList())
                {
                    var partner = pm.Parties.FirstOrDefault(x => x.Id == id);
                    if (partner == null) continue;

                    var row = HudKit.NewRect(coalitionCard, "PartnerRow");
                    HudKit.HStack(row.gameObject, 8, 0, TextAnchor.MiddleLeft, true, true);

                    string nameStr = $"{partner.Name} ({partner.Seats} sandalye)";
                    var nameLabel = HudKit.Label(row, nameStr, 21, HudTheme.Text);
                    HudKit.Size(nameLabel.gameObject, flexW: 1);

                    string satStr = $"%{partner.Satisfaction:F0}";
                    Color satColor = partner.Satisfaction < 30f ? HudTheme.Bad 
                                   : partner.Satisfaction < 50f ? HudTheme.Warn 
                                   : HudTheme.Good;
                    var satLabel = HudKit.Label(row, satStr, 21, satColor, 
                        TextAlignmentOptions.Right, FontStyles.Bold);
                    HudKit.Size(satLabel.gameObject, prefW: 80);

                    // Çıkar butonu
                    var partnerId = id;
                    var btn = HudKit.MakeButton(row, "Çıkar", HudTheme.Bad, Color.white, 18, () =>
                    {
                        string msg = pm.RemoveCoalitionPartner(partnerId);
                        ui.WriteLog(msg);
                        ui.MarkDirty("meclis");
                        ui.RebuildCurrent();
                    }, 40);
                    HudKit.Size(btn.gameObject, prefW: 80);
                }
            }

            // Yeni ortak ekleme (çoğunluk yoksa)
            if (!hasMajority)
            {
                var addCard = ui.Card(c, "YENİ ORTAK EKLE");

                var candidates = pm.GetCoalitionCandidates("player", totalSeats / 2);
                foreach (var cand in candidates)
                {
                    if (pm.CoalitionPartnerIds.Contains(cand.Id)) continue;

                    var candId = cand.Id;
                    string label = $"{cand.Name} ({cand.Seats} sandalye, {cand.SideLabel()}, ideoloji {cand.Ideology:+0;-0})";
                    var btn = HudKit.MakeButton(addCard, label, HudTheme.Action, Color.white, 20, () =>
                    {
                        string msg = pm.AddCoalitionPartner(candId, e);
                        ui.WriteLog(msg);
                        ui.MarkDirty("meclis");
                        ui.RebuildCurrent();
                    }, 48);

                    btn.interactable = e.PoliticalCapital >= 30f;
                }
            }
        }
        
    }
}
