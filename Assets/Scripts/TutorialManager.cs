using System;
using System.Linq;
using UnityEngine;
using DemocracySim.Engine.Core;

/// <summary>
/// FAZ 5: Bağlamsal öğretici. Sıradaki adımın koşulu sağlanınca tek bir ipucu gösterir (turda en fazla bir).
/// İlerleme Universe.TutorialStep içinde tutulduğundan kayıtla birlikte saklanır.
/// Kapatmak için PlayerPrefs "Tutorial" = 0 (SettingsManager'a bir toggle bağlanabilir).
/// </summary>
public static class TutorialManager
{
    private class Step
    {
        public string Key;
        public Func<SimulationEngine, bool> Trigger;
        public Step(string key, Func<SimulationEngine, bool> trigger) { Key = key; Trigger = trigger; }
    }

    private static readonly Step[] Steps =
    {
        new Step("tut_welcome",    e => true),
        new Step("tut_policy",     e => e.CurrentTurn >= 1),
        new Step("tut_capital",    e => e.CurrentTurn >= 2),
        new Step("tut_vote",       e => e.CurrentTurn >= 3),
        new Step("tut_groups",     e => e.CurrentTurn >= 4 || e.Demographics.Any(g => g.Satisfaction < 35f)),
        new Step("tut_unrest",     e => e.Universe.Unrest >= 30f),
        new Step("tut_coalition",  e => e.CurrentTurn >= 6 || e.Universe.Partners.Any(p => p.Satisfaction < 35f)),
        new Step("tut_world",      e => e.CurrentTurn >= 8),
        new Step("tut_election",   e => e.TurnUntilElection <= 4),
        new Step("tut_opposition", e => e.CurrentRole == SimulationEngine.PlayerRole.Opposition),
    };

    public static bool Enabled => PlayerPrefs.GetInt("Tutorial", 1) == 1;
    public static int StepCount => Steps.Length;

    public static void Reset(SimulationEngine e)
    {
        e.Universe.TutorialStep = 0;
        e.Universe.TutorialDone = false;
    }

    /// <summary>Her turun sonunda çağrılır. show(metin, uyarıMı) ipucunu arayüze iletir.</summary>
    public static void Check(SimulationEngine e, Action<string, bool> show)
    {
        var u = e.Universe;
        if (!Enabled || u.TutorialDone) return;
        if (u.TutorialStep >= Steps.Length) { u.TutorialDone = true; return; }

        var step = Steps[u.TutorialStep];
        if (!step.Trigger(e)) return;

        show("💡 " + LocalizationManager.Get(step.Key), false);
        u.TutorialStep++;
        if (u.TutorialStep >= Steps.Length) u.TutorialDone = true;
    }
}
