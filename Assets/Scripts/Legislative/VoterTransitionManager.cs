using System;
using System.Collections.Generic;
using System.Linq;
using DemocracySim.Engine.Core;

namespace DemocracySim.Engine.Legislative
{
    [Serializable]
    public class VoterTransitionManager
    {
        public Dictionary<string, Dictionary<string, float>> VotingBlocs { get; set; }
            = new Dictionary<string, Dictionary<string, float>>();

        public const float MinShiftRate = 0.02f;
        public const float MaxShiftRate = 0.05f;

        public void Initialize(List<DemographicGroup> groups, PartyManager partyManager)
        {
            VotingBlocs.Clear();
            foreach (var g in groups)
            {
                var blocs = new Dictionary<string, float>();
                float remaining = 100f;
                var parties = partyManager.Parties.Where(p => p.Seats > 0).ToList();

                foreach (var p in parties)
                {
                    float bias = GroupIdeologyBias(g.Id);
                    float affinity = 1f - Math.Abs(p.Ideology - bias) / 200f;
                    float share = Math.Max(5f, affinity * 30f);
                    blocs[p.Id] = share;
                    remaining -= share;
                }

                if (remaining > 0f && parties.Count > 0)
                {
                    var closest = parties.OrderBy(p => Math.Abs(p.Ideology - GroupIdeologyBias(g.Id))).First();
                    blocs[closest.Id] += remaining;
                }

                VotingBlocs[g.Id] = blocs;
            }
        }

        public void ProcessTurn(SimulationEngine e)
        {
            if (e.Demographics == null || VotingBlocs.Count == 0) return;

            foreach (var g in e.Demographics)
            {
                if (!VotingBlocs.ContainsKey(g.Id)) continue;
                var blocs = VotingBlocs[g.Id];

                float dissatisfaction = Math.Max(0f, 50f - g.Satisfaction) / 50f;
                if (dissatisfaction <= 0f) continue;

                float shiftRate = MinShiftRate + (MaxShiftRate - MinShiftRate) * dissatisfaction;

                var govParty = e.PartyManager.Parties.FirstOrDefault(p => p.IsInGovernment);
                var oppParty = e.PartyManager.Parties
                    .Where(p => !p.IsInGovernment && p.Seats > 0)
                    .OrderByDescending(p => p.Seats)
                    .FirstOrDefault();

                if (govParty != null && oppParty != null && blocs.ContainsKey(govParty.Id))
                {
                    float shift = blocs[govParty.Id] * shiftRate;
                    blocs[govParty.Id] -= shift;
                    blocs[oppParty.Id] = blocs.GetValueOrDefault(oppParty.Id, 0f) + shift;
                }
            }
        }

        public Dictionary<string, float> GetVoteShares(List<DemographicGroup> groups)
        {
            var shares = new Dictionary<string, float>();
            float totalWeight = 0f;

            foreach (var g in groups)
            {
                float weight = g.Influence;
                totalWeight += weight;
                if (!VotingBlocs.ContainsKey(g.Id)) continue;

                foreach (var kv in VotingBlocs[g.Id])
                {
                    if (!shares.ContainsKey(kv.Key)) shares[kv.Key] = 0f;
                    shares[kv.Key] += kv.Value * weight;
                }
            }

            float total = shares.Values.Sum();
            if (total > 0f)
                foreach (var k in shares.Keys.ToList()) shares[k] = shares[k] / total * 100f;

            return shares;
        }

        private static float GroupIdeologyBias(string groupId)
        {
            return groupId switch
            {
                "workers"       => -50f,
                "intellectuals" => -30f,
                "capitalists"   =>  50f,
                "conservatives" =>  60f,
                _               =>   0f
            };
        }
    }
}