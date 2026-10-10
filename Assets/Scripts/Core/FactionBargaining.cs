using System;
using System.Collections.Generic;
using System.Linq;
using DemocracySim.Engine.Core;

namespace DemocracySim.Engine.Core
{
    [Serializable]
    public class FactionDemand
    {
        public string PolicyId { get; set; }
        public float TargetValue { get; set; }
        public float SupportReward { get; set; } = 15f;
        public float RejectionPenalty { get; set; } = 10f;
        public bool IsFulfilled { get; set; } = false;
    }

    public class FactionBargaining
    {
        public Dictionary<string, FactionDemand> ActiveDemands { get; set; }
            = new Dictionary<string, FactionDemand>();


        public void ProcessTurn(SimulationEngine e)
        {
            if (e.CurrentTurn % 3 != 0) return;

            foreach (var f in e.Factions.Factions)
            {
                if (ActiveDemands.ContainsKey(f.Id)) continue;
                if (SimRng.NextDouble() > 0.3) continue;

                var demand = GenerateDemand(f, e);
                if (demand != null) ActiveDemands[f.Id] = demand;
            }
        }

        private FactionDemand GenerateDemand(Faction f, SimulationEngine e)
        {
            var policies = e.AllObjects.OfType<SimPolicy>()
                .Where(p => Math.Abs(p.IdeologicalAlignment - f.Ideology) < 40f)
                .ToList();

            if (policies.Count == 0) return null;
            var policy = policies[SimRng.Next(policies.Count)];

            float target = policy.IdeologicalAlignment > 0
                ? Math.Min(100f, policy.ActualValue + 20f)
                : Math.Max(0f, policy.ActualValue - 20f);

            return new FactionDemand
            {
                PolicyId = policy.Id,
                TargetValue = target,
                SupportReward = 10f + (float)SimRng.NextDouble() * 10f,
                RejectionPenalty = 5f + (float)SimRng.NextDouble() * 10f
            };
        }

        public string AcceptDemand(string factionId, SimulationEngine e)
        {
            if (!ActiveDemands.TryGetValue(factionId, out var demand))
                return "Bu fraksiyonun aktif bir talebi yok.";

            var policy = e.AllObjects.OfType<SimPolicy>()
                .FirstOrDefault(p => p.Id == demand.PolicyId);
            if (policy == null) return "Talep edilen yasa bulunamadı.";

            policy.ActualValue = demand.TargetValue;
            policy.IsActive = true;
            if (policy.Intensity <= 0f) policy.Intensity = 1f;

            e.Factions.AdjustSupport(factionId, demand.SupportReward);
            ActiveDemands.Remove(factionId);

            return $"{factionId} fraksiyonunun talebi kabul edildi: {policy.Name} " +
                   $"→ {demand.TargetValue:F0}. Destek +{demand.SupportReward:F0}.";
        }

        public string RejectDemand(string factionId, SimulationEngine e)
        {
            if (!ActiveDemands.TryGetValue(factionId, out var demand))
                return "Bu fraksiyonun aktif bir talebi yok.";

            e.Factions.AdjustSupport(factionId, -demand.RejectionPenalty);
            ActiveDemands.Remove(factionId);

            return $"{factionId} fraksiyonunun talebi reddedildi. " +
                   $"Destek -{demand.RejectionPenalty:F0}. Meydan okuma riski arttı.";
        }
    }
}