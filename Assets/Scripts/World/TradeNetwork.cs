using System;
using System.Collections.Generic;
using System.Linq;
using DemocracySim.Engine.Core;

namespace DemocracySim.Engine.World
{
    /// <summary>
    /// FAZ 4: Ülkeler arası ticaret ağı. Kaynak gerçeklik her ülkenin Universe.TradePartners listesidir
    /// (oyuncunun mevcut "Ticaret Anlaşması" menüsü ve kayıt sistemi bozulmaz); bu sınıf listeleri
    /// simetrik tutar, AI anlaşmaları yapar ve ilişkiler bozulunca / yaptırımda anlaşmaları koparır.
    /// </summary>
    public class TradeNetwork
    {
        public const float BreakRelation = -30f;

        public bool HasAgreement(Country a, Country b)
            => a.Engine.Universe.TradePartners.Contains(b.Id) && b.Engine.Universe.TradePartners.Contains(a.Id);

        public bool Sign(Country a, Country b, WorldManager w, bool announce = true)
        {
            if (a == b || HasAgreement(a, b)) return false;
            if (!a.Engine.Universe.TradePartners.Contains(b.Id)) a.Engine.Universe.TradePartners.Add(b.Id);
            if (!b.Engine.Universe.TradePartners.Contains(a.Id)) b.Engine.Universe.TradePartners.Add(a.Id);
            w.Diplomacy.ImproveRelation(a.Id, b.Id, 5f);
            if (announce)
            {
                w.NotifyCountry(a, $"🤝 {b.Name} ile ticaret anlaşması imzalandı.");
                w.NotifyCountry(b, $"🤝 {a.Name} ile ticaret anlaşması imzalandı.");
            }
            return true;
        }

        public void Break(Country a, Country b, WorldManager w, string reason)
        {
            bool had = a.Engine.Universe.TradePartners.Remove(b.Id) | b.Engine.Universe.TradePartners.Remove(a.Id);
            if (!had) return;
            w.AddGdp(a, -0.5f); w.AddGdp(b, -0.5f);
            w.NotifyCountry(a, $"💔 {b.Name} ile ticaret anlaşması koptu: {reason}", true);
            w.NotifyCountry(b, $"💔 {a.Name} ile ticaret anlaşması koptu: {reason}", true);
        }

        public void ProcessTurn(WorldManager w)
        {
            // 1) Tek taraflı kayıtları (ör. oyuncunun yaptığı anlaşma) karşı tarafa yansıt
            foreach (var c in w.Countries)
                foreach (var pid in c.Engine.Universe.TradePartners.ToList())
                {
                    var p = w.Countries.FirstOrDefault(x => x.Id == pid);
                    if (p == null) { c.Engine.Universe.TradePartners.Remove(pid); continue; }
                    if (!p.Engine.Universe.TradePartners.Contains(c.Id)) p.Engine.Universe.TradePartners.Add(c.Id);
                }

            // 2) Bozulan ilişkiler anlaşmaları koparır
            foreach (var c in w.Countries)
                foreach (var pid in c.Engine.Universe.TradePartners.ToList())
                {
                    var p = w.Countries.First(x => x.Id == pid);
                    if (string.Compare(c.Id, p.Id) > 0) continue;   // çifti bir kez işle
                    if (w.Diplomacy.GetRelation(c.Id, p.Id) < BreakRelation)
                        Break(c, p, w, "diplomatik ilişkiler çöktü");
                }

            // 3) Ağır yaptırım altındaki ülkenin anlaşmalarını partnerler azaltır
            foreach (var c in w.Countries)
            {
                var u = c.Engine.Universe;
                if (u.SanctionLevel < 60f || u.TradePartners.Count == 0 || SimRng.NextDouble() > 0.2) continue;
                var p = w.Countries.FirstOrDefault(x => x.Id == u.TradePartners[SimRng.Next(u.TradePartners.Count)]);
                if (p != null) Break(c, p, w, "ambargo");
            }

                        // 4) FAZ 4: AI ülkeler Trust bazlı ticaret anlaşması arar
            foreach (var c in w.Countries)
            {
                if (c.IsPlayerControlled) continue;
                if (c.Engine.Universe.TradePartners.Count >= 4 || SimRng.NextDouble() > 0.08) continue;

                // FAZ 4: Trust skoru 35'in üstünde olanlarla ticaret yap
                var cand = w.Countries
                    .Where(p => p != c && !HasAgreement(c, p) && p.Engine.Universe.TradePartners.Count < 4)
                    .Where(p => c.Memory.WillTradeWith(p.Id))   // ← YENİ
                    .OrderByDescending(p => c.Memory.GetTrust(p.Id))   // ← Trust'a göre sırala
                    .FirstOrDefault();

                if (cand != null)
                {
                    Sign(c, cand, w);
                    // Trust bonusu
                    c.Memory.ChangeTrust(cand.Id, DiplomaticMemory.TradeAgreement, "Ticaret anlaşması");
                    cand.Memory.ChangeTrust(c.Id, DiplomaticMemory.TradeAgreement, "Ticaret anlaşması");
                }
            }
        }
    }
}
