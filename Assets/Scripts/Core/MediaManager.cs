using System;
using System.Collections.Generic;
using System.Linq;

namespace DemocracySim.Engine.Core
{
    [Serializable]
    public class MediaOutlet
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public float Ideology { get; set; }
        public float Credibility { get; set; } = 70f;
        public float Reach { get; set; } = 50f;
        public float TrustInGovernment { get; set; } = 50f;

        public MediaOutlet(string id, string name, float ideology)
        {
            Id = id; Name = name; Ideology = ideology;
        }
    }

    public class MediaManager
    {
        public List<MediaOutlet> Outlets { get; set; } = new List<MediaOutlet>();

        public MediaManager()
        {
            Outlets.Add(new MediaOutlet("state_tv", "Devlet Televizyonu", 0f) { Credibility = 50f, Reach = 80f });
            Outlets.Add(new MediaOutlet("opposition_press", "Muhalif Gazete", -50f) { Credibility = 80f, Reach = 40f });
            Outlets.Add(new MediaOutlet("social_media", "Sosyal Medya", 0f) { Credibility = 30f, Reach = 90f });
            Outlets.Add(new MediaOutlet("intl_press", "Uluslararası Basın", 0f) { Credibility = 90f, Reach = 30f });
        }

        public void ProcessTurn(SimulationEngine e, float policyAlignment)
        {
            foreach (var o in Outlets)
            {
                float diff = Math.Abs(policyAlignment - o.Ideology);
                if (diff < 25f) o.TrustInGovernment += 1f;
                else if (diff > 60f) o.TrustInGovernment -= 2f;
                o.TrustInGovernment = Math.Clamp(o.TrustInGovernment, 0f, 100f);
            }
        }

        public string HoldPressConference(SimulationEngine e)
        {
            float avgTrust = Outlets.Average(o => o.TrustInGovernment);
            float legitChange = (avgTrust - 50f) * 0.2f;
            e.Legitimacy.AdjustLegitimacy(legitChange);
            e.PoliticalCapital = Math.Max(0f, e.PoliticalCapital - 5f);
            return legitChange >= 0
                ? $"Basın toplantısı olumlu geçti (meşruiyet {legitChange:+0.0;-0.0})."
                : $"Basın toplantısı beklediğiniz gibi gitmedi (meşruiyet {legitChange:+0.0;-0.0}).";
        }

        public void PressureOutlet(string outletId, SimulationEngine e)
        {
            var o = Outlets.FirstOrDefault(x => x.Id == outletId);
            if (o == null) return;
            o.Credibility -= 10f;
            o.TrustInGovernment += 15f;
            e.Legitimacy.AdjustLegitimacy(-3f);
        }
    }
}