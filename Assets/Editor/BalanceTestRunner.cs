#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using DemocracySim.Engine.Core;
using DemocracySim.Engine.Data;
using DemocracySim.Engine.World;

/// <summary>
/// FAZ 5: Otomatik denge testi. Menü: Tools > Kapitalist > Denge Testi (20 dünya x 60 tur).
/// Tüm ülkeleri AI olarak (gerçek WorldManager döngüsüyle) oynatır; çöküş oranı, meşruiyet, GSYİH ve
/// huzursuzluk dağılımını ölçer, hedef aralığın dışındaki değerleri raporda ⚠️ ile işaretler.
/// Rapor: <proje>/balance_report.txt
/// </summary>
public static class BalanceTestRunner
{
    const int Worlds = 20, Turns = 60;

    [MenuItem("Tools/Kapitalist/Denge Testi")]
    public static void Run()
    {
        var profiles = DataManager.LoadCountryProfiles();
        if (profiles.Count == 0) { Debug.LogError("[Denge] countries.json yüklenemedi."); return; }

        var oldLog = SimLogger.OnLog;
        SimLogger.OnLog = (m, l) => { };   // test sırasında log seli olmasın

        var legit = new List<float>(); var gdp = new List<float>(); var unrest = new List<float>();
        var minGdp = new List<float>(); var maxGdp = new List<float>();
        int collapses = 0, memberships = 0, tradePairs = 0, crashes = 0, radicalTurns = 0;
        var errors = new List<string>();

        try
        {
            for (int w = 0; w < Worlds; w++)
            {
                EditorUtility.DisplayProgressBar("Denge Testi", $"Dünya {w + 1}/{Worlds}", (float)w / Worlds);
                AIStateController.CollapseCount = 0;
                var world = new WorldManager();
                foreach (var p in profiles)
                {
                    var c = new Country(p.id, p.name, false) { GlobalAlignment = p.globalAlignment, Continent = p.continent };
                    DataManager.LoadWorld(c.Engine, "");
                    DataManager.AddDefaultDemographics(c.Engine, p);
                    c.Engine.Legitimacy.SetLegitimacy(p.startingLegitimacy);
                    c.Engine.PoliticalCapital = p.startingCapital;
                    world.Countries.Add(c);
                }

                try
                {
                    for (int t = 0; t < Turns; t++)
                    {
                        world.ProcessWorldTurn();
                        radicalTurns += world.Countries.Sum(c => c.Engine.Universe.Radicalized.Count(kv => kv.Value));
                    }
                }
                catch (Exception ex) { crashes++; errors.Add($"Dünya {w}: {ex.GetType().Name}: {ex.Message}"); }

                collapses += AIStateController.CollapseCount;
                memberships += world.Organizations.Sum(o => o.MemberIds.Count);
                tradePairs += world.Countries.Sum(c => c.Engine.Universe.TradePartners.Count) / 2;
                var gdps = world.Countries.Select(c => world.GdpOf(c)).ToList();
                gdp.AddRange(gdps); minGdp.Add(gdps.Min()); maxGdp.Add(gdps.Max());
                legit.AddRange(world.Countries.Select(c => c.Engine.Legitimacy.CurrentLegitimacy));
                unrest.AddRange(world.Countries.Select(c => c.Engine.Universe.Unrest));
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            SimLogger.OnLog = oldLog;
        }

        float countryTurns = Worlds * profiles.Count * Turns;
        float collapsePer100 = collapses / countryTurns * 100f;
        float avgLegit = legit.Average(), avgGdp = gdp.Average(), avgUnrest = unrest.Average();

        var sb = new StringBuilder();
        sb.AppendLine($"DENGE TESTİ — {Worlds} dünya x {Turns} tur x {profiles.Count} ülke ({DateTime.Now:yyyy-MM-dd HH:mm})");
        sb.AppendLine("-------------------------------------------------------");
        sb.AppendLine($"Motor hatası (çökme)         : {crashes}  {Flag(crashes == 0)}");
        sb.AppendLine($"Çöküş/isyan (100 ülke-turda) : {collapsePer100:F2}  hedef 0.2-3.0  {Flag(collapsePer100 >= 0.2f && collapsePer100 <= 3f)}");
        sb.AppendLine($"Ort. final meşruiyet         : {avgLegit:F1}  hedef 35-75  {Flag(avgLegit >= 35f && avgLegit <= 75f)}");
        sb.AppendLine($"Ort. final huzursuzluk       : {avgUnrest:F1}  hedef 10-45  {Flag(avgUnrest >= 10f && avgUnrest <= 45f)}");
        sb.AppendLine($"Ort. final GSYİH             : {avgGdp:F1}  hedef 35-80  {Flag(avgGdp >= 35f && avgGdp <= 80f)}");
        sb.AppendLine($"GSYİH dibe vuran dünya (<=5) : {minGdp.Count(v => v <= 5f)}/{Worlds}  {Flag(minGdp.Count(v => v <= 5f) <= Worlds / 5)}");
        sb.AppendLine($"GSYİH tavana yapışan (>=99)  : {maxGdp.Count(v => v >= 99f)}/{Worlds}  {Flag(maxGdp.Count(v => v >= 99f) <= Worlds / 5)}");
        sb.AppendLine($"Radikal grup-turu / dünya    : {radicalTurns / (float)Worlds:F1}");
        sb.AppendLine($"Ort. örgüt üyeliği / dünya   : {memberships / (float)Worlds:F1}  {Flag(memberships / (float)Worlds >= 2f)}");
        sb.AppendLine($"Ort. ticaret anlaşması/dünya : {tradePairs / (float)Worlds:F1}  {Flag(tradePairs / (float)Worlds >= 1f)}");
        foreach (var err in errors.Take(5)) sb.AppendLine("HATA: " + err);

        string path = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "balance_report.txt");
        File.WriteAllText(path, sb.ToString());
        Debug.Log(sb.ToString() + "\nRapor: " + path);
    }

    static string Flag(bool ok) => ok ? "✅" : "⚠️";
}
#endif
