using System.Collections.Generic;
using System.Linq;

namespace DemocracySim.Engine.Core
{
    /// <summary>
    /// EK-25: Bakan piyasası. Her tur rastgele adaylar üretilir.
    /// Oyuncu bu adayları işe alabilir veya mevcut bakanları görevden alabilir.
    /// </summary>
    public static class MinistryMarket
    {
        public const int MaxCandidates = 6;
        public const int RefreshIntervalTurns = 3;   // 3 turda bir piyasa yenilenir
        public const float HireCost = 30f;
        public const float FiringCost = 15f;

        // Aday isim havuzu
        static readonly string[] FirstNames = {
            "Ali", "Ayşe", "Mehmet", "Fatma", "Hasan", "Zeynep",
            "Mustafa", "Elif", "Ahmet", "Merve", "Emre", "Selin",
            "Kemal", "Deniz", "Can", "Ece", "Burak", "İpek",
            "Mert", "Ceren", "Onur", "Sude", "Alp", "Damla"
        };
        static readonly string[] LastNames = {
            "Yılmaz", "Kaya", "Demir", "Şahin", "Çelik", "Yıldız",
            "Yıldırım", "Öztürk", "Aydın", "Özdemir", "Arslan", "Doğan",
            "Kılıç", "Aslan", "Çetin", "Kara", "Koç", "Kurt",
            "Özkan", "Şimşek", "Polat", "Erdoğan", "Güneş", "Yalçın"
        };
        static readonly string[] Portfolios = {
            "Ekonomi", "Sağlık", "Savunma", "Eğitim", "Adalet",
            "Dışişleri", "İçişleri", "Çevre", "Teknoloji", "Enerji"
        };
        static readonly string[] Traits = { "Deneyimli", "Genç", "Akademisyen",
            "İş İnsanı", "Bürokrat", "Aktivist", "Popülist", "Teknokrat" };

        /// <summary>Her tur çağrılır. Piyasayı yeniler.</summary>
        public static void ProcessTurn(SimulationEngine e)
        {
            if (!e.IsPlayerCountry) return;

            // 3 turda bir piyasa yenilenir
            if (e.CurrentTurn > 0 && e.CurrentTurn % RefreshIntervalTurns != 0) return;

            RefreshMarket(e);
        }

        /// <summary>Piyasayı yeniden doldur.</summary>
        public static void RefreshMarket(SimulationEngine e)
        {
            e.Universe.MinistryMarket.Clear();

            var rng = KapitalistRng.For("ministry_market");

            for (int i = 0; i < MaxCandidates; i++)
            {
                var candidate = GenerateCandidate(rng);
                e.Universe.MinistryMarket.Add(candidate);
            }
        }

        private static MinistryCandidate GenerateCandidate(GameRandom rng)
        {
            string firstName = FirstNames[rng.Next(FirstNames.Length)];
            string lastName = LastNames[rng.Next(LastNames.Length)];
            string portfolio = Portfolios[rng.Next(Portfolios.Length)];
            string trait = Traits[rng.Next(Traits.Length)];

            return new MinistryCandidate
            {
                Id = System.Guid.NewGuid().ToString().Substring(0, 8),
                Name = $"{firstName} {lastName}",
                Portfolio = portfolio,
                Trait = trait,
                Competence = 40f + rng.NextFloat() * 50f,      // 40-90
                Loyalty = 50f + rng.NextFloat() * 40f,         // 50-90
                Ideology = -100f + rng.NextFloat() * 200f      // -100..+100
            };
        }

        /// <summary>Adayı işe al.</summary>
        public static string Hire(SimulationEngine e, string candidateId)
        {
            var candidate = e.Universe.MinistryMarket.FirstOrDefault(c => c.Id == candidateId);
            if (candidate == null) return "Aday bulunamadı.";

            if (e.PoliticalCapital < HireCost)
                return $"Yeterli siyasi sermaye yok ({HireCost:F0} gerekir).";

            // Aynı portföyde başka bakan var mı?
            var existing = e.Actors.FirstOrDefault(a =>
                a.Role == ActorRole.Minister && a.Portfolio == candidate.Portfolio);
            if (existing != null)
                return $"{candidate.Portfolio} portföyünde zaten bir bakan var: {existing.Name}. Önce onu görevden al.";

            // Bakan sayısı kontrolü
            int ministerCount = e.Actors.Count(a => a.Role == ActorRole.Minister);
            if (ministerCount >= 8)
                return "Kabine dolu (en fazla 8 bakan).";

            e.PoliticalCapital -= HireCost;

            // PolitikActor oluştur
            var actor = new PoliticalActor(candidate.Id, candidate.Name, ActorRole.Minister)
            {
                Portfolio = candidate.Portfolio,
                Ideology = candidate.Ideology,
                Loyalty = candidate.Loyalty,
                Competence = candidate.Competence,
                Ambition = 40f + 40f * (float)System.Math.Abs(candidate.Ideology) / 100f,
                Influence = 30f + candidate.Competence * 0.3f,
                Satisfaction = 70f
            };
            if (!string.IsNullOrEmpty(candidate.Trait))
            {
                actor.BackgroundTrait = candidate.Trait;
                actor.Traits.Add(ParseTrait(candidate.Trait));
            }
            e.Actors.Add(actor);

            // Piyasadan çıkar
            e.Universe.MinistryMarket.Remove(candidate);

            return $"[OK] {candidate.Name} {candidate.Portfolio} Bakanı olarak atandı! " +
                   $"Yetkinlik: %{candidate.Competence:F0}, Sadakat: %{candidate.Loyalty:F0}";
        }

        /// <summary>Bakanı görevden al.</summary>
        public static string Fire(SimulationEngine e, string actorId)
        {
            var minister = e.Actors.FirstOrDefault(a => a.Id == actorId && a.Role == ActorRole.Minister);
            if (minister == null) return "Bakan bulunamadı.";

            if (e.PoliticalCapital < FiringCost)
                return $"Yeterli siyasi sermaye yok ({FiringCost:F0} gerekir).";

            e.PoliticalCapital -= FiringCost;
            e.Actors.Remove(minister);
            e.Legitimacy.AdjustLegitimacy(-2f);   // Küçük meşruiyet kaybı

            // Diğer bakanlar tedirgin olur
            foreach (var a in e.Actors.Where(a => a.Role == ActorRole.Minister))
                a.Loyalty = System.Math.Max(0f, a.Loyalty - 3f);

            return $"[OK] {minister.Name} görevden alındı. (-{FiringCost:F0} sermaye, -2 meşruiyet)";
        }

        public static ActorTrait ParseTrait(string s) => s switch
        {
            "İş İnsanı" or "BusinessPerson" => ActorTrait.BusinessPerson,
            "Aktivist" or "Activist" => ActorTrait.Activist,
            "Popülist" or "Populist" => ActorTrait.Populist,
            "Teknokrat" or "Technocrat" => ActorTrait.Technocrat,
            "Akademisyen" or "Academic" => ActorTrait.Academic,
            "Bürokrat" or "Bureaucrat" => ActorTrait.Bureaucrat,
            "Hırslı" or "Ambitious" => ActorTrait.Ambitious,
            "Temkinli" or "Cautious" => ActorTrait.Cautious,
            "Yolsuz" or "Corrupt" => ActorTrait.Corrupt,
            _ => ActorTrait.LoyalistsHeart
        };
    }

    /// <summary>Piyasadaki bir bakan adayı.</summary>
    [System.Serializable]
    public class MinistryCandidate
    {
        public string Id;
        public string Name;
        public string Portfolio;
        public string Trait;
        public float Competence;
        public float Loyalty;
        public float Ideology;
    }
}