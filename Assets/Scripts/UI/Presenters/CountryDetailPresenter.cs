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


internal sealed class CountryDetailPresenter : PresenterBase
{
    public CountryDetailPresenter(UIManager ui) : base(ui) { }

    public void Populate()
    {
        if (ui.countryDetailHolder == null) return;
        HudKit.ClearChildren(ui.countryDetailHolder);

        if (ui.lastCountry == null || string.IsNullOrEmpty(ui.selectedCountry))
        {
            HudKit.Label(ui.countryDetailHolder, "Ülke seçin.", 20, HudTheme.Dim);
            return;
        }

        HudKit.Label(ui.countryDetailHolder, "Seçili ülke: " + UIManager.Clean(ui.selectedCountry), 22, HudTheme.Gold, 
            TextAlignmentOptions.Left, FontStyles.Bold);

        if (ui.selectedCountryDetail != null)
        {
            var c = ui.selectedCountryDetail;
            float legit = c.Engine.Legitimacy.CurrentLegitimacy;
            float unrest = c.Engine.Universe.Unrest;
            float gdp = c.Engine.AllObjects.Find(o => o.Id == "gdp")?.ActualValue ?? 0f;
            float army = c.Engine.Army.ArmySatisfaction;

            ui.StatRow(ui.countryDetailHolder, "Meşruiyet", $"%{legit:F0}", legit / 100f, UIManager.GoodHigh(legit, 55f, 35f), "");
            ui.StatRow(ui.countryDetailHolder, "Huzursuzluk", $"%{unrest:F0}", unrest / 100f, UIManager.GoodLow(unrest, 25f, 55f), "");
            ui.StatRow(ui.countryDetailHolder, "GSYİH", $"{gdp:F0}", gdp / 100f, HudTheme.Info, "");
            ui.StatRow(ui.countryDetailHolder, "Ordu Memnuniyeti", $"%{army:F0}", army / 100f, UIManager.GoodHigh(army, 55f, 30f), "");

            // FAZ 4: Oyuncuya karşı Trust skoru
            if (ui.lastCountry != null && c.Memory != null)
            {
                float trust = c.Memory.GetTrust(ui.lastCountry.Id);
                TrustTier tier = c.Memory.GetTier(ui.lastCountry.Id);
                Color trustColor = tier switch
                {
                    TrustTier.Enemy    => HudTheme.Bad,
                    TrustTier.Cold     => HudTheme.Warn,
                    TrustTier.Neutral  => HudTheme.Dim,
                    TrustTier.Friendly => HudTheme.Good,
                    TrustTier.Ally     => HudTheme.Good,
                    _ => HudTheme.Dim
                };
                string tierStr = tier switch
                {
                    TrustTier.Enemy    => "DÜŞMAN",
                    TrustTier.Cold     => "Soğuk",
                    TrustTier.Neutral  => "Nötr",
                    TrustTier.Friendly => "Dostane",
                    TrustTier.Ally     => "MÜTTEFİK",
                    _ => "?"
                };
                ui.StatRow(ui.countryDetailHolder, $"Bize Güven ({tierStr})", 
                    $"%{trust:F0}", trust / 100f, trustColor, "");
            }

            // Son eylemler
            if (c.RecentActions != null && c.RecentActions.Count > 0)
            {
                HudKit.Label(ui.countryDetailHolder, "SON EYLEMLER:", 19, HudTheme.Gold, 
                    TextAlignmentOptions.Left, FontStyles.Bold);
                foreach (var action in c.RecentActions)
                {
                    HudKit.Label(ui.countryDetailHolder, "• " + UIManager.Clean(action), 18, HudTheme.Dim);
                }
            }

            // FAZ 4: Trust geçmişi (son 3 olay)
            if (c.Memory != null && ui.lastCountry != null && 
                c.Memory.TrustHistory.TryGetValue(ui.lastCountry.Id, out var history) && 
                history.Count > 0)
            {
                HudKit.Label(ui.countryDetailHolder, "GÜVEN GEÇMİŞİ:", 19, HudTheme.Gold, 
                    TextAlignmentOptions.Left, FontStyles.Bold);
                foreach (var evt in history.Take(3))
                {
                    string sign = evt.Delta >= 0 ? "+" : "";
                    Color evtColor = evt.Delta >= 0 ? HudTheme.Good : HudTheme.Bad;
                    HudKit.Label(ui.countryDetailHolder, 
                        $"• {UIManager.Clean(evt.Reason)} ({sign}{evt.Delta:F0})", 
                        17, evtColor);
                }
            }
        }
    }
}
