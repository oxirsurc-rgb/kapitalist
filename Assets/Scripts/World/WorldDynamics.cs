using System;
using System.Collections.Generic;
using System.Linq;
using DemocracySim.Engine.Core;

namespace DemocracySim.Engine.World
{
    /// <summary>FAZ 4: Devrim bulaşması ve göç. Her dünya turunda ülke motorlarından ÖNCE çalışır.</summary>
    public static class WorldDynamics
    {

        public static bool IsUnstable(Country c)
        {
            var u = c.Engine.Universe;
            return u.RevoltTurns > 0 || u.GameOverReason != null || u.Unrest >= 75f;
        }

        static bool SameContinent(Country a, Country b)
            => !string.IsNullOrEmpty(a.Continent) && a.Continent == b.Continent;

        static float AvgSatisfaction(Country c)
            => c.Engine.Demographics.Count == 0 ? 50f : c.Engine.Demographics.Average(g => g.Satisfaction);

        // ---------------- Devrim bulaşması ----------------
        public static void ProcessContagion(WorldManager w)
        {
            var sources = w.Countries.Where(IsUnstable).ToList();
            if (sources.Count == 0) return;

            foreach (var t in w.Countries)
            {
                float add = 0f;
                string firstSource = null;
                foreach (var s in sources)
                {
                    if (s == t) continue;
                    float geo = SameContinent(s, t) ? 1f : 0.3f;
                    float ideo = Math.Abs(s.GlobalAlignment - t.GlobalAlignment) < 40f ? 1f : 0.5f;
                    add += 4f * geo * ideo;
                    if (firstSource == null) firstSource = s.Name;
                }
                if (add <= 0f) continue;

                // Memnuniyeti düşük halk daha bulaşıcıdır (0.5 - 1.5)
                float susceptibility = 0.5f + (100f - AvgSatisfaction(t)) / 100f;
                var u = t.Engine.Universe;
                u.ContagionPressure = Math.Clamp(u.ContagionPressure + add * susceptibility, 0f, 25f);

                if (t.IsPlayerControlled && add >= 3f && w.GlobalTurn % 3 == 0)
                    w.NotifyCountry(t, $"🔥 {firstSource} ve çevresindeki karışıklık sınırlarınıza yayılıyor; sokaklarda huzursuzluk artıyor.", true);
            }
        }

        // ---------------- Göç ----------------
        public static void ProcessMigration(WorldManager w)
        {
            // Net göç bakiyesi zamanla sönümlenir
            foreach (var c in w.Countries) c.Engine.Universe.MigrationBalance *= 0.9f;

            var sources = w.Countries.Where(c => c.Engine.Legitimacy.CurrentLegitimacy < 35f || c.Engine.Universe.Unrest > 60f).ToList();
            var dests = w.Countries.Where(c => c.Engine.Legitimacy.CurrentLegitimacy >= 50f && c.Engine.Universe.Unrest < 40f).ToList();
            if (sources.Count == 0 || dests.Count == 0) return;

            foreach (var s in sources)
            {
                var su = s.Engine.Universe;
                float severity = Math.Clamp(
                    Math.Max(0f, 35f - s.Engine.Legitimacy.CurrentLegitimacy) / 35f +
                    Math.Max(0f, su.Unrest - 60f) / 40f, 0.2f, 1.5f);

                var d = dests.Where(x => x != s)
                             .OrderByDescending(x => (SameContinent(s, x) ? 20f : 0f) + w.GdpOf(x) + SimRng.Next(0, 10))
                             .FirstOrDefault();
                if (d == null) continue;

                // Gidenler: beyin göçü benzeri kayıp, ama sokakta baskı da azalır
                w.AddGdp(s, -0.4f * severity);
                su.Unrest = Math.Max(0f, su.Unrest - 0.5f * severity);
                su.MigrationBalance -= severity;

                // Gelenler: işgücü katkısı, ama emek piyasası ve kimlik gerilimi
                w.AddGdp(d, 0.25f * severity);
                var du = d.Engine.Universe;
                du.MigrationBalance += severity;
                // FAZ 3: MigrationManager'a bildir
d.Engine.Migration?.OnMigrantArrival(severity);
                du.Unrest = Math.Clamp(du.Unrest + 0.5f * severity, 0f, 100f);
                var workers = d.Engine.Demographics.FirstOrDefault(g => g.Id == "workers");
                var cons = d.Engine.Demographics.FirstOrDefault(g => g.Id == "conservatives");
                workers?.AdjustSatisfaction(-0.4f * severity);
                cons?.AdjustSatisfaction(-0.8f * severity);

                if (severity >= 0.8f && w.GlobalTurn % 3 == 0)
                {
                    if (d.IsPlayerControlled) w.NotifyCountry(d, $"🧳 {s.Name}'dan göç dalgası geldi: işgücü artıyor ama muhafazakârlar ve işçiler tedirgin.", true);
                    if (s.IsPlayerControlled) w.NotifyCountry(s, $"🧳 Ülkenizden {d.Name}'a kitlesel göç var; nitelikli iş gücü kaybediyorsunuz.", true);
                }
            }
        }
    }
}
