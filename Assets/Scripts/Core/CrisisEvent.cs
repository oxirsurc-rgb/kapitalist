using System;
using System.Collections.Generic;

namespace DemocracySim.Engine.Core
{
    public class CrisisOption
    {
        public string Label { get; set; } = string.Empty;
        public Action<SimulationEngine>? Effect { get; set; }
        public string ResultText { get; set; } = string.Empty;

        public CrisisOption() { }
        public CrisisOption(string label, Action<SimulationEngine> effect)
        {
            Label = label;
            Effect = effect;
        }
    }

    public class CrisisEvent
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Func<SimulationEngine, bool>? TriggerCondition { get; set; }
        public List<CrisisOption> Options { get; set; } = new List<CrisisOption>();
    }
}