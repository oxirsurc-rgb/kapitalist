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


internal sealed class EconomyPresenter : PagePresenter
{
    public EconomyPresenter(UIManager ui) : base(ui) { }

    public override void Build(RectTransform c, SimulationEngine e)
    {
        // =====================================================
        // 1) MAKRO GÖSTERGELER
        // =====================================================
        var macro = ui.Card(c, "MAKRO GÖSTERGELER");
        ui.StatRow(macro, "Enflasyon", "%" + e.Economy.Inflation.ToString("F1"),
            e.Economy.Inflation / 50f,
            e.Economy.Inflation < 0f ? HudTheme.Bad : UIManager.GoodLow(e.Economy.Inflation, 6f, 15f),
            ui.DeltaStr("infl", false));
        ui.StatRow(macro, "Ulusal Borç", e.Economy.NationalDebt.ToString("F0") + " milyar",
            e.Economy.NationalDebt / 3000f,
            UIManager.GoodLow(e.Economy.NationalDebt, 1500f, 2500f),
            ui.DeltaStr("debt", false));
        ui.StatRow(macro, "Kredi Notu", e.Economy.CreditRating.ToString("F0") + " / 100",
            e.Economy.CreditRating / 100f,
            UIManager.GoodHigh(e.Economy.CreditRating, 60f, 30f),
            ui.DeltaStr("credit", true));

        // =====================================================
        // 2) BÜTÇE DENGESİ
        // =====================================================
                float revenue = e.CalculateTotalRevenue();
        float spending = e.CalculateTotalSpending();
        
        // FAZ 1 (K6): Faiz yükünü de hesaba kat
        float debtRatioCalc = e.Economy.NationalDebt / 2000f;
        float intRate = 0.02f + Mathf.Min(0.08f, debtRatioCalc * 0.03f);
        float interest = e.Economy.NationalDebt * intRate;
        float realSpending = spending + interest;
        
        float balance = revenue - realSpending;
        Color balanceColor = balance >= 0f ? HudTheme.Good : HudTheme.Bad;
        string balanceStr = (balance >= 0 ? "+" : "") + balance.ToString("F0") + " milyar";

        var budget = ui.Card(c, "BÜTÇE DENGESİ");

        ui.StatRow(budget, "Toplam Gelir (Vergiler)",
            revenue.ToString("F0") + " milyar",
            Mathf.Clamp01(revenue / 200f),
            HudTheme.Info, "");

        ui.StatRow(budget, "Toplam Gider (Politikalar)",
            spending.ToString("F0") + " milyar",
            Mathf.Clamp01(spending / 200f),
            HudTheme.Warn, "");
                    // FAZ 1 (K6): Faiz yükü göster
        float debtRatio = e.Economy.NationalDebt / 2000f;
        float interestRate = 0.02f + Mathf.Min(0.08f, debtRatio * 0.03f);
        float interestPayment = e.Economy.NationalDebt * interestRate;
        ui.StatRow(budget, "Faiz Yükü (Borç)",
            interestPayment.ToString("F0") + " milyar",
            Mathf.Clamp01(interestPayment / 300f),
            HudTheme.Bad, "");

        // FAZ 1 (K6): Gerçek toplam gider (politika + faiz)
        realSpending = spending + interestPayment;
        ui.StatRow(budget, "GERÇEK GİDER",
            realSpending.ToString("F0") + " milyar",
            Mathf.Clamp01(realSpending / 400f),
            HudTheme.Bad, "");

        // Denge satırı
        var balanceRow = HudKit.NewRect(budget, "BalanceRow");
        HudKit.VStack(balanceRow.gameObject, 4, 0);
        var balanceTop = HudKit.NewRect(balanceRow, "Top");
        HudKit.HStack(balanceTop.gameObject, 8, 0, TextAnchor.MiddleLeft, false, true);
        HudKit.Label(balanceTop, "DENGE", 22, HudTheme.Text, TextAlignmentOptions.Left, FontStyles.Bold);
        var balVal = HudKit.Label(balanceTop, balanceStr, 26, balanceColor, TextAlignmentOptions.Right, FontStyles.Bold);
        HudKit.Size(balVal.gameObject, prefW: 200);

                string balanceComment = balance >= 50f ? "Butce fazlasi - borc azaliyor"
                              : balance >= 0f  ? "Dengede - kucuk fazla"
                              : balance >= -50f ? "Kucuk acik - borc yavas artiyor"
                              : balance >= -150f ? "Buyuk acik - borc hizla artiyor!"
                              :                    "KRITIK: Iflas riski! Vergileri artir!";
        HudKit.Label(budget, balanceComment, 19, balanceColor);

        // =====================================================
        // 3) GİDER DAĞILIMI
        // =====================================================
        var spendingCard = ui.Card(c, "GİDER DAĞILIMI");
        var spendingDict = e.GetSpendingByCategory();
        if (spendingDict.Count == 0)
        {
            HudKit.Label(spendingCard, "Aktif politika yok.", 20, HudTheme.Dim);
        }
        else
        {
            float totalSpend = spendingDict.Values.Sum();
            foreach (var kv in spendingDict.OrderByDescending(x => x.Value))
            {
                float pct = totalSpend > 0 ? (kv.Value / totalSpend) * 100f : 0f;
                ui.StatRow(spendingCard,
                    $"{kv.Key} ({pct:F0}%)",
                    kv.Value.ToString("F0"),
                    kv.Value / Mathf.Max(1f, totalSpend),
                    HudTheme.Warn, "");
            }
        }

        // =====================================================
        // 4) GELİR DAĞILIMI
        // =====================================================
        var revenueCard = ui.Card(c, "GELİR DAĞILIMI (Vergiler)");
        var revenueDict = e.GetRevenueByCategory();
        if (revenueDict.Count == 0)
        {
            HudKit.Label(revenueCard, "Aktif vergi yok.", 20, HudTheme.Dim);
        }
        else
        {
            float totalRev = revenueDict.Values.Sum();
            foreach (var kv in revenueDict.OrderByDescending(x => x.Value))
            {
                float pct = totalRev > 0 ? (kv.Value / totalRev) * 100f : 0f;
                ui.StatRow(revenueCard,
                    $"{kv.Key} ({pct:F0}%)",
                    kv.Value.ToString("F0"),
                    kv.Value / Mathf.Max(1f, totalRev),
                    HudTheme.Info, "");
            }
        }

        // =====================================================
        // 5) SEKTÖRLER
        // =====================================================
        if (e.Sectors != null && e.Sectors.Sectors.Count > 0)
        {
            var sectorsCard = ui.Card(c, "EKONOMİK SEKTÖRLER");
            foreach (var sector in e.Sectors.Sectors)
            {
                var row = HudKit.NewRect(sectorsCard, "SectorRow");
                HudKit.VStack(row.gameObject, 4, 0);

                var top = HudKit.NewRect(row, "Top");
                HudKit.HStack(top.gameObject, 8, 0, TextAnchor.MiddleLeft, false, true);

                var nameLabel = HudKit.Label(top, UIManager.Clean(sector.Name), 22, HudTheme.Text);
                HudKit.Size(nameLabel.gameObject, flexW: 1);

                var pctLabel = HudKit.Label(top, $"GSYİH %{sector.GdpShare:F1}", 20, HudTheme.Gold,
                    TextAlignmentOptions.Right, FontStyles.Bold);
                HudKit.Size(pctLabel.gameObject, prefW: 150);

                HudKit.Bar(row, sector.GdpShare / 100f, HudTheme.Gold, 8f);

                HudKit.Label(row,
                    $"İstihdam: %{sector.Employment:F0}  |  Verimlilik: %{sector.Productivity:F0}",
                    17, HudTheme.Dim);
            }
        }
                // =====================================================
        // 5.5) TEKNOLOJİ (FAZ 2)
        // =====================================================
        if (e.Technology != null)
        {
            var techCard = ui.Card(c, "TEKNOLOJİ VE İNOVASYON");
            
            float tech = e.Technology.TechLevel;
            ui.StatRow(techCard, "Teknoloji Seviyesi", 
                $"%{tech:F1}", tech / 100f, UIManager.GoodHigh(tech, 60f, 30f), "");
            
            ui.StatRow(techCard, "Ar-Ge Bütçesi", 
                $"%{e.Technology.RndBudget:F0}", e.Technology.RndBudget / 100f, HudTheme.Info, "");
            
            ui.StatRow(techCard, "Patent Sayısı", 
                e.Technology.PatentCount.ToString(), 
                Mathf.Clamp01(e.Technology.PatentCount / 50f), 
                HudTheme.Gold, "");
            
            // Beyin dengesi
            float brain = e.Technology.BrainGain;
            Color brainColor = brain >= 1f ? HudTheme.Good 
                             : brain >= -1f ? HudTheme.Warn 
                             : HudTheme.Bad;
            string brainStr = brain >= 0f ? $"Beyin Kazanımı +{brain:F1}" : $"Beyin Kaybı {brain:F1}";
            HudKit.Label(techCard, brainStr, 20, brainColor);

            // Patent ilerleme barı
            float patentThreshold = 100f * (1f + e.Technology.PatentCount * 0.1f);
            float patentProgress = e.Technology.PatentProgress / patentThreshold;
            HudKit.Label(techCard, 
                $"Sonraki patent: %{patentProgress * 100:F0}", 
                18, HudTheme.Dim);
            HudKit.Bar(techCard, patentProgress, HudTheme.Info, 8f);

            // Etkiler
            HudKit.Label(techCard, 
                $"GSYİH Bonusu: +%{e.Technology.GetGdpBonus():F1}  |  " +
                $"Askeri Bonus: +%{e.Technology.GetMilitaryBonus():F1}  |  " +
                $"Yaptırım Direnci: +%{e.Technology.GetSanctionResistance():F0}", 
                18, HudTheme.Dim);
        }

    
        // =====================================================
        // 6) İSTATİSTİKLER
        // =====================================================
        var stats = ui.Card(c, "EKONOMİK İSTATİSTİKLER");
        var list = e.AllObjects.OfType<SimStatistic>().ToList();
        if (list.Count == 0) HudKit.Label(stats, "İstatistik verisi yok.", 21, HudTheme.Dim);
        foreach (var s in list)
        {
            float range = Mathf.Max(0.01f, s.MaxValue - s.MinValue);
            ui.StatRow(stats, UIManager.Clean(s.Name), s.ActualValue.ToString("F1"),
                (s.ActualValue - s.MinValue) / range,
                HudTheme.Info,
                ui.DeltaStr("obj:" + s.Id, true, true),
                s);
        }
    }
}
