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


internal sealed class ObjectDetailPresenter : PresenterBase
{
    public ObjectDetailPresenter(UIManager ui) : base(ui) { }

    public void Show(SimObject obj)
    {
        ui.OpenModal(760, card =>
        {
            // Başlık
            HudKit.Label(card, UIManager.Clean(obj.Name).ToUpper(), 38, HudTheme.Gold, TextAlignmentOptions.Center, FontStyles.Bold);
            
            // Anlık değer
            HudKit.Label(card, $"%{obj.ActualValue:F1}", 56, HudTheme.Text, TextAlignmentOptions.Center, FontStyles.Bold);

            // 3 tur önceki değer ve değişim
            float change3 = obj.GetChangeSince(3);
            string changeStr = (change3 >= 0 ? "+" : "") + change3.ToString("F1") + " puan (3 tur)";
            Color changeColor = change3 >= 0 ? HudTheme.Good : HudTheme.Bad;
            HudKit.Label(card, changeStr, 24, changeColor, TextAlignmentOptions.Center);

            // Denge noktası
            HudKit.Label(card, 
                $"Denge noktası: %{obj.EquilibriumValue:F1}  |  Atalet: %{obj.Inertia * 100:F0}", 
                20, HudTheme.Dim, TextAlignmentOptions.Center);

            // Etki breakdown
            var breakdown = obj.GetEffectBreakdown();
            var sourcesCard = ui.Card(card, "ETKİ KAYNAKLARI");
            
            if (breakdown.Count == 0)
            {
                HudKit.Label(sourcesCard, "Şu an aktif bir etki yok.", 20, HudTheme.Dim);
            }
            else
            {
                foreach (var (effect, contrib) in breakdown.Take(8))
                {
                    var row = HudKit.NewRect(sourcesCard, "Row");
                    HudKit.HStack(row.gameObject, 8, 0, TextAnchor.MiddleLeft, true, true);

                    // Etki işareti ve rengi
                    string sign = contrib >= 0 ? "▲" : "▼";
                    Color contribColor = contrib >= 0 ? HudTheme.Good : HudTheme.Bad;
                    
                    var icon = HudKit.Label(row, sign, 22, contribColor, TextAlignmentOptions.Left, FontStyles.Bold);
                    HudKit.Size(icon.gameObject, prefW: 24);

                    // Kaynak adı
                    string srcName = UIManager.Clean(effect.Source?.Name ?? effect.Source?.Id ?? "?");
                    var nameLabel = HudKit.Label(row, srcName, 22, HudTheme.Text);
                    HudKit.Size(nameLabel.gameObject, flexW: 1);

                    // Etki değeri
                    var valLabel = HudKit.Label(row, 
                        (contrib >= 0 ? "+" : "") + contrib.ToString("F2") + " puan", 
                        20, contribColor, TextAlignmentOptions.Right, FontStyles.Bold);
                    HudKit.Size(valLabel.gameObject, prefW: 130);
                }
            }

            // Son 20 tur mini grafiği (büyük)
            if (obj.History.Count >= 5)
            {
                var histCard = ui.Card(card, "SON 20 TUR");
                ui.DrawSparkline(histCard, obj.History, HudTheme.Info);
                
                // Min/max bilgisi
                float min = obj.History.Min();
                float max = obj.History.Max();
                HudKit.Label(histCard, $"Min: %{min:F1}   |   Max: %{max:F1}   |   Şimdi: %{obj.ActualValue:F1}", 
                    18, HudTheme.Dim, TextAlignmentOptions.Center);
            }

            // FAZ 3.5: Etki zinciri görselleştirmesi (mini node graph)
if (obj.IncomingEffects.Count > 0 || obj.OutgoingEffects.Count > 0)
{
    var graphCard = ui.Card(card, "ETKİ ZİNCİRİ");

    // Gelen etkiler (sol taraf)
    if (obj.IncomingEffects.Count > 0)
    {
        HudKit.Label(graphCard, "◀ GELEN ETKİLER", 18, HudTheme.Info, 
            TextAlignmentOptions.Left, FontStyles.Bold);
        foreach (var eff in obj.IncomingEffects.Take(5))
        {
            float contrib = eff.CalculateImpact();
            string sign = contrib >= 0 ? "▲" : "▼";
            Color c2 = contrib >= 0 ? HudTheme.Good : HudTheme.Bad;
            HudKit.Label(graphCard, 
                $"  {sign} {UIManager.Clean(eff.Source?.Name ?? "?")} → {contrib:+0.0;-0.0}", 
                17, c2);
        }
    }

    // Giden etkiler (sağ taraf)
    if (obj.OutgoingEffects.Count > 0)
    {
        HudKit.Label(graphCard, "▶ GİDEN ETKİLER", 18, HudTheme.Warn, 
            TextAlignmentOptions.Left, FontStyles.Bold);
        foreach (var eff in obj.OutgoingEffects.Take(5))
        {
            float contrib = eff.CalculateImpact();
            string sign = contrib >= 0 ? "▲" : "▼";
            Color c2 = contrib >= 0 ? HudTheme.Good : HudTheme.Bad;
            HudKit.Label(graphCard, 
                $"  {sign} → {UIManager.Clean(eff.Target?.Name ?? "?")} ({contrib:+0.0;-0.0})", 
                17, c2);
        }
    }
}

            // Kapat butonu
            HudKit.MakeButton(card, "Kapat", HudTheme.Line, HudTheme.Dim, 22, () => ui.CloseModal(), 56);
        });
    }
}
