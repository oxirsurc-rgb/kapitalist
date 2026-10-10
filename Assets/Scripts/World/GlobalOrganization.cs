using System;
using System.Collections.Generic;
using System.Linq;
using DemocracySim.Engine.Core;

namespace DemocracySim.Engine.World
{
    public enum OrgType { Military, Economic, Diplomatic }

    public class GlobalOrganization
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public OrgType Type { get; set; }
        public float IdeologicalAlignment { get; set; }
        public List<string> MemberIds { get; set; } = new List<string>();
        public string LeaderCountryId { get; set; }

        // Üyelik kriterleri (GSYİH 0-100 ölçeğindedir)
        public float RequiredLegitimacy { get; set; } = 0f;
        public float RequiredGdp { get; set; } = 0f;

        // FAZ 4: katılmak için gereken ideolojik yakınlık ve üyelikte kalma toleransı
        public float JoinTolerance { get; set; } = 30f;
        public float StayTolerance { get; set; } = 60f;

        // FAZ 4: üyelik aidatı (tur başına siyasi sermaye) ve ayrılma cezası
        public float DuesPerTurn { get; set; } = 0f;
        public float LeavePenalty { get; set; } = 5f;

        public GlobalOrganization(string id, string name, OrgType type, float alignment)
        {
            Id = id; Name = name; Type = type; IdeologicalAlignment = alignment;
        }

        public bool CanJoin(float countryAlignment)
            => Math.Abs(countryAlignment - IdeologicalAlignment) < JoinTolerance;

        /// <summary>Katılamama nedenini döndürür; null ise katılabilir (UI'da gerekçe göstermek için).</summary>
        public string JoinBlocker(Country country)
        {
            if (country == null || country.Engine == null) return "Geçersiz ülke.";
            if (Math.Abs(country.GlobalAlignment - IdeologicalAlignment) >= JoinTolerance)
                return "İdeolojik olarak uyumsuzsunuz.";
            if (country.Engine.Legitimacy.CurrentLegitimacy < RequiredLegitimacy)
                return $"Meşruiyet yetersiz (en az {RequiredLegitimacy:F0}).";
            if (RequiredGdp > 0f)
            {
                var gdp = country.Engine.AllObjects.FirstOrDefault(o => o.Id == "gdp");
                if (gdp == null || gdp.ActualValue < RequiredGdp) return $"GSYİH yetersiz (en az {RequiredGdp:F0}).";
            }
            return null;
        }

        public bool CanJoin(Country country) => JoinBlocker(country) == null;

        public void AssignLeader(string countryId) { LeaderCountryId = countryId; }

        public void ApplyMembershipEffects(SimulationEngine e)
        {
            if (e == null) return;
            if (Type == OrgType.Economic)
            {
                var gdp = e.AllObjects.FirstOrDefault(o => o.Id == "gdp");
                if (gdp != null) gdp.ActualValue = Math.Clamp(gdp.ActualValue + 2f, 0f, 100f);
            }
            else if (Type == OrgType.Diplomatic) e.Legitimacy.AdjustLegitimacy(1f);
            else if (Type == OrgType.Military)
                e.Army.LoadState(Math.Clamp(e.Army.ArmySatisfaction + 2f, 0f, 100f), e.Army.MilitaryStrength, e.Army.LoyaltyToLeader);
        }

        public void Join(Country c, WorldManager w)
        {
            if (MemberIds.Contains(c.Id)) return;
            MemberIds.Add(c.Id);
            ApplyMembershipEffects(c.Engine);
            // Üyelik, diğer üyelerle ilişkileri anında ısıtır
            foreach (var m in MemberIds) if (m != c.Id) w.Diplomacy.ImproveRelation(c.Id, m, 8f);
        }

        public void Leave(Country c, WorldManager w, string reason)
        {
            if (!MemberIds.Remove(c.Id)) return;
            c.Engine.Legitimacy.AdjustLegitimacy(-LeavePenalty);
            foreach (var m in MemberIds) w.Diplomacy.DamageRelation(c.Id, m, 10f);
            w.NotifyCountry(c, $"🚪 {Name}: {reason}", true);
        }

        // ================================================================
        // FAZ 4: Üyeliğin GERÇEK SONUÇLARI (her dünya turunda çalışır)
        // ================================================================
        public void ProcessTurn(WorldManager w)
        {
            // 1) Kriteri kaybedenler ihraç edilir
            foreach (var id in MemberIds.ToList())
            {
                var c = w.Countries.FirstOrDefault(x => x.Id == id);
                if (c == null) { MemberIds.Remove(id); continue; }
                bool badLegit = c.Engine.Legitimacy.CurrentLegitimacy < RequiredLegitimacy - 15f;
                bool badAlign = Math.Abs(c.GlobalAlignment - IdeologicalAlignment) > StayTolerance + (Type == OrgType.Military ? 0f : 40f);
                if (badLegit || badAlign)
                    Leave(c, w, badLegit ? "Meşruiyetiniz çöktüğü için üyelikten çıkarıldınız!" : "İdeolojik kopuş nedeniyle üyelikten çıkarıldınız!");
            }
            if (MemberIds.Count == 0) { LeaderCountryId = null; return; }

            var members = MemberIds.Select(id => w.Countries.FirstOrDefault(c => c.Id == id)).Where(c => c != null).ToList();

            // 2) Lider: en yüksek GSYİH'li üye
            LeaderCountryId = members.OrderByDescending(c => w.GdpOf(c)).First().Id;

            // 3) Aidat
            if (DuesPerTurn > 0f)
                foreach (var c in members)
                    c.Engine.PoliticalCapital = Math.Max(0f, c.Engine.PoliticalCapital - DuesPerTurn);

            // 4) Türe özgü sonuçlar
            foreach (var c in members)
            {
                var e = c.Engine;
                switch (Type)
                {
                    case OrgType.Military:
                        e.Army.LoadState(Math.Clamp(e.Army.ArmySatisfaction + 0.2f, 0f, 100f), e.Army.MilitaryStrength, e.Army.LoyaltyToLeader);
                        // Blok gerginliği yükselince üyeler savaş korkusunu yaşar
                        if (w.GlobalTension > 60f) e.Universe.Unrest = Math.Clamp(e.Universe.Unrest + 0.5f, 0f, 100f);
                        break;
                    case OrgType.Economic:
                        w.AddGdp(c, 0.2f);
                        break;
                    case OrgType.Diplomatic:
                        e.Universe.SanctionLevel = Math.Max(0f, e.Universe.SanctionLevel - 1f);
                        break;
                }
            }

            // 5) Üyeler arası ilişkiler ısınır; ekonomik örgütte otomatik ticaret anlaşması
            for (int i = 0; i < members.Count; i++)
                for (int j = i + 1; j < members.Count; j++)
                {
                    w.Diplomacy.ImproveRelation(members[i].Id, members[j].Id, Type == OrgType.Military ? 0.4f : 0.15f);
                    if (Type == OrgType.Economic) w.Trade.Sign(members[i], members[j], w, false);
                }
        }
    }
}
