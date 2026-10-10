using System;
using System.Collections.Generic;
using System.Linq;

namespace DemocracySim.Engine.Core
{
    public enum FactionType
    {
        Reformist,      // Sol, hızlı değişim
        Conservative,   // Sağ, statüko
        Nationalist,    // Aşırı sağ, güçlü devlet
        Populist,       // Merkez, kısa vadeli memnuniyet
        Technocrat      // Merkez-sol, verimlilik
    }

    [Serializable]
    public class Faction
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public FactionType Type { get; set; }
        public float Ideology { get; set; }          // -100..+100
        public float Support { get; set; } = 50f;    // 0..100 parti içi destek
        public float Influence { get; set; } = 1f;   // Oy ağırlığı
        public bool IsChallenging { get; set; } = false;
        public int ChallengeTurnsLeft { get; set; } = 0;

        public Faction(string id, string name, FactionType type, float ideology)
        {
            Id = id; Name = name; Type = type; Ideology = ideology;
        }
    }

    public class FactionManager
    {
        public List<Faction> Factions { get; set; } = new List<Faction>();
        public string RulingFactionId { get; set; } = "reformist";


        // --- Meydan okuma sabitleri ---
        public const float ChallengeThreshold = 20f;    // %20 altı = meydan okuma
        public const int ChallengeDuration = 3;         // 3 tur sürer

        public FactionManager()
        {
            Factions.Add(new Faction("reformist", "Reformistler", FactionType.Reformist, -60f));
            Factions.Add(new Faction("conservative", "Muhafazakarlar", FactionType.Conservative, 60f));
            Factions.Add(new Faction("nationalist", "Milliyetçiler", FactionType.Nationalist, 80f));
            Factions.Add(new Faction("populist", "Popülistler", FactionType.Populist, 0f));
            Factions.Add(new Faction("technocrat", "Teknokratlar", FactionType.Technocrat, -10f));
        }

        /// <summary>Her tur çağrılır. Yasaların ideolojik yönü fraksiyon desteğini kaydırır.</summary>
        public void ProcessTurn(SimulationEngine e, float policyAlignment)
        {
            foreach (var f in Factions)
            {
                // Yasaya uyum: yakınsa destek artar, uzaksa düşer
                float diff = Math.Abs(policyAlignment - f.Ideology);
                if (diff < 30f) f.Support += 2f;
                else if (diff > 70f) f.Support -= 3f;
                f.Support = Math.Clamp(f.Support, 0f, 100f);

                // Meydan okuma kontrolü
                if (!f.IsChallenging && f.Support < ChallengeThreshold && f.Id != RulingFactionId)
                {
                    // %15 ihtimalle meydan okuma başlat
                    if (SimRng.NextDouble() < 0.15)
                    {
                        f.IsChallenging = true;
                        f.ChallengeTurnsLeft = ChallengeDuration;
                        e.Universe.Messages.Add(new UniverseMessage
                        {
                            Text = $"⚠️ LİDERLİK MEYDAN OKUMASI: {f.Name} fraksiyonu liderliğinize karşı harekete geçti! " +
                                   $"{ChallengeDuration} tur içinde durumu düzeltmezseniz parti içi oylama yapılacak.",
                            IsWarning = true
                        });
                    }
                }

                // Devam eden meydan okuma
                if (f.IsChallenging)
                {
                    f.ChallengeTurnsLeft--;
                    if (f.ChallengeTurnsLeft <= 0)
                    {
                        ResolveChallenge(e, f);
                    }
                }
            }
        }

        /// <summary>Meydan okuma sonucunu çözer.</summary>
        private void ResolveChallenge(SimulationEngine e, Faction f)
        {
            f.IsChallenging = false;

            // Fraksiyonun desteği hâlâ %20'nin altındaysa oylama yapılır
            if (f.Support < ChallengeThreshold)
            {
                // Oy oranı: fraksiyon desteği + rastgelelik
                float challengeVotes = f.Support + (float)(SimRng.NextDouble() * 20 - 10);
                float playerVotes = 100f - challengeVotes;

                if (challengeVotes > playerVotes)
                {
                    // Oyuncu kaybetti: liderlik gider
                    e.Universe.GameOverReason = $"PARTİ İÇİ DARBE: {f.Name} fraksiyonu liderliğinizi devirdi!";
                    e.Universe.Messages.Add(new UniverseMessage
                    {
                        Text = $"💥 LİDERLİK KAYBEDİLDİ: {f.Name} fraksiyonu parti içi oylamayı kazandı!",
                        IsWarning = true
                    });
                }
                else
                {
                    // Oyuncu kazandı ama yara aldı
                    e.Legitimacy.AdjustLegitimacy(-10f);
                    e.PoliticalCapital = Math.Max(0f, e.PoliticalCapital - 20f);
                    e.Universe.Messages.Add(new UniverseMessage
                    {
                        Text = $"✅ MEYDAN OKUMA ATLATILDI: {f.Name} fraksiyonunun girişimi başarısız oldu ama parti yara aldı.",
                        IsWarning = false
                    });
                }
            }
            else
            {
                // Destek yükseldiği için meydan okuma kendiliğinden bitti
                e.Universe.Messages.Add(new UniverseMessage
                {
                    Text = $"{f.Name} fraksiyonu sakinleşti, meydan okuma sona erdi.",
                    IsWarning = false
                });
            }
        }

        /// <summary>Bir fraksiyonun desteğini değiştirir (oyuncu eylemi).</summary>
        public void AdjustSupport(string factionId, float delta)
        {
            var f = Factions.FirstOrDefault(x => x.Id == factionId);
            if (f != null)
            {
                f.Support = Math.Clamp(f.Support + delta, 0f, 100f);
                if (f.Support >= ChallengeThreshold) f.IsChallenging = false;
            }
        }

        /// <summary>Oyuncu bir fraksiyona taviz verir (sermaye karşılığı).</summary>
        public string ConcedeToFaction(string factionId, SimulationEngine e, float cost)
        {
            var f = Factions.FirstOrDefault(x => x.Id == factionId);
            if (f == null) return "Fraksiyon bulunamadı.";
            if (e.PoliticalCapital < cost) return $"Yeterli sermaye yok ({cost:F0} gerekir).";

            e.PoliticalCapital -= cost;
            f.Support = Math.Clamp(f.Support + 15f, 0f, 100f);
            f.IsChallenging = false; // Taviz verilince meydan okuma durur

            return $"{f.Name} fraksiyonuna taviz verildi (+15 destek). Meydan okuma sona erdi.";
        }

        /// <summary>Bir fraksiyondan bakan atar (kabine dengesi).</summary>
     /// <summary>Bir fraksiyondan bakan atar (kabine dengesi).</summary>
public string AppointFactionMinister(string factionId, SimulationEngine e)
{
    var f = Factions.FirstOrDefault(x => x.Id == factionId);
    if (f == null) return "Fraksiyon bulunamadı.";
    if (e.PoliticalCapital < 10f) return "Yeterli siyasi sermaye yok (10 gerekir).";

    // --- YENİ: Kabine dolu mu kontrol et ---
    const int MaxMinisters = 5;   // Kabinede en fazla 5 bakan
    int currentMinisters = e.Actors.Count(a => a.Role == ActorRole.Minister);
    if (currentMinisters >= MaxMinisters)
        return $"Kabine dolu ({MaxMinisters} bakan). Önce birini görevden al.";

    // --- YENİ: Gerçek bir bakan aktörü oluştur ---
    var minister = CreateFactionMinister(f, e);
    e.AddActor(minister);

    // Fraksiyon desteği artar
    f.Support = Math.Clamp(f.Support + 20f, 0f, 100f);
    f.IsChallenging = false;
    e.PoliticalCapital = Math.Max(0f, e.PoliticalCapital - 10f);

string traitStr = minister.Traits.Count > 0 ? $" ({minister.Traits[0]})" : "";
return $"{f.Name} kanadından {minister.Name}{traitStr} kabineye girdi. " +
       $"Sadakat %{minister.Loyalty:F0}, Yetkinlik %{minister.Competence:F0}. Fraksiyon desteği +20.";}

/// <summary>Fraksiyon tipine uygun rastgele bir bakan oluşturur.</summary>
private static PoliticalActor CreateFactionMinister(Faction f, SimulationEngine e)
{
    // İsim havuzu (fraksiyona göre)
    string[] reformistNames   = { "Elif Hanım", "Mert Bey", "Selin Hanım", "Can Bey" };
    string[] conservativeNames = { "Hakan Bey", "Ayşe Hanım", "Fatih Bey", "Zehra Hanım" };
    string[] nationalistNames = { "Kaan Bey", "Ece Hanım", "Alp Bey", "Damla Hanım" };
    string[] populistNames    = { "Burak Bey", "Sude Hanım", "Onur Bey", "İpek Hanım" };
    string[] technocratNames  = { "Prof. Dr. Aylin Hanım", "Doç. Dr. Sinan Bey", "Dr. Ceren Hanım", "Doç. Dr. Emre Bey" };

    string[] pool = f.Type switch
    {
        FactionType.Reformist   => reformistNames,
        FactionType.Conservative=> conservativeNames,
        FactionType.Nationalist => nationalistNames,
        FactionType.Populist    => populistNames,
        FactionType.Technocrat  => technocratNames,
        _                       => reformistNames
    };

    string name = pool[SimRng.Next(pool.Length)];
    string id = $"min_{f.Id}_{Guid.NewGuid().ToString().Substring(0, 4)}";

    var minister = new PoliticalActor(id, name, ActorRole.Minister)
    {
        Ideology    = f.Ideology + (float)(SimRng.NextDouble() * 20 - 10),   // Fraksiyona yakın ideoloji
        Ambition    = 40f + (float)(SimRng.NextDouble() * 40),                // 40-80 arası
        Loyalty     = 60f + (float)(SimRng.NextDouble() * 30),                // 60-90 arası (yeni atandı, sadık)
        Competence  = 50f + (float)(SimRng.NextDouble() * 40),                // 50-90 arası
        Influence   = 20f + (float)(SimRng.NextDouble() * 30),                // 20-50 arası
        Satisfaction = 70f
    };

    // Fraksiyona göre trait ekle
    switch (f.Type)
    {
        case FactionType.Reformist:   minister.Traits.Add(ActorTrait.Ambitious); break;
        case FactionType.Conservative: minister.Traits.Add(ActorTrait.Cautious); break;
        case FactionType.Nationalist: minister.Traits.Add(ActorTrait.LoyalistsHeart); break;
        case FactionType.Populist:    minister.Traits.Add(ActorTrait.Populist); break;
        case FactionType.Technocrat:  minister.Traits.Add(ActorTrait.Cautious); break;
    }

    return minister;
}

        /// <summary>Fraksiyon raporu (UI için).</summary>
        public List<string> GetReport()
        {
            var lines = new List<string>();
            foreach (var f in Factions)
            {
                string status = f.IsChallenging ? $"⚠️ MEYDAN OKUMA ({f.ChallengeTurnsLeft} tur)" : "";
                string side = f.Ideology < -20f ? "Sol" : (f.Ideology > 20f ? "Sağ" : "Merkez");
                lines.Add($"{f.Name} ({side}): %{f.Support:F0} {status}");
            }
            return lines;
        }
    }
}