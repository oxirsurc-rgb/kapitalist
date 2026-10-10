using System.Collections.Generic;
using System.Linq;
using DemocracySim.Engine.Core;

/// <summary>
/// FAZ 5: Küçük demo — 20 turda 5 hedef. Tüm hedefler tutarsa kazanılır; erken çöküşte standart GameOver devreye girer.
/// Başlatmak için GameManager.StartDemo() (ana menüye bir butonun OnClick'ine bağlanır).
/// </summary>
public static class DemoScenario
{
    public const int Length = 20;

    public class Goal
    {
        public string Key;
        public System.Func<SimulationEngine, bool> Check;
        public Goal(string key, System.Func<SimulationEngine, bool> check) { Key = key; Check = check; }
    }

    static float Gdp(SimulationEngine e) => e.AllObjects.FirstOrDefault(o => o.Id == "gdp")?.ActualValue ?? 0f;

    public static readonly Goal[] Goals =
    {
        new Goal("demo_goal_legit",  e => e.Legitimacy.CurrentLegitimacy >= 55f),
        new Goal("demo_goal_unrest", e => e.Universe.Unrest < 35f),
        new Goal("demo_goal_trade",  e => e.Universe.TradePartners.Count >= 1),
        new Goal("demo_goal_radical",e => !e.Universe.Radicalized.Any(kv => kv.Value)),
        new Goal("demo_goal_gdp",    e => Gdp(e) >= 55f),
    };

    public static bool AllGoalsMet(SimulationEngine e) => Goals.All(g => g.Check(e));

    public static string ProgressText(SimulationEngine e)
    {
        int left = Length - (e.CurrentTurn - e.Universe.DemoStartTurn);
        var lines = new List<string> { LocalizationManager.Get("demo_progress", left) };
        foreach (var g in Goals)
            lines.Add((g.Check(e) ? "✅ " : "⬜ ") + LocalizationManager.Get(g.Key));
        return string.Join("\n", lines);
    }
}
