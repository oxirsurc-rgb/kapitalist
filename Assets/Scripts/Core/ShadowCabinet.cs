using System;
using System.Collections.Generic;
using System.Linq;

namespace DemocracySim.Engine.Core
{
    /// <summary>
    /// Gölge kabine üyes. Muhalefetteyken oyuncunun atadığı sözcüler.
    /// İktidara gelince gerçek bakan olur.
    /// </summary>
    [Serializable]
    public class ShadowMinister
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Portfolio { get; set; }   // "Ekonomi", "Adalet", "Sosyal"
        public float Competence { get; set; }   // 0-100
        public float MediaSkill { get; set; }   // 0-100 (medyada ne kadar iyi)
        public float Loyalty { get; set; }      // Oyuncuya sadakat
        public int AppointedTurn { get; set; }  // Hangi turda atandı

        public ShadowMinister(string id, string name, string portfolio)
        {
            Id = id; Name = name; Portfolio = portfolio;
            Competence = 50f; MediaSkill = 50f; Loyalty = 70f;
        }
    }

    /// <summary>
    /// Gölge kabine yöneticisi. Sadece muhalefetteyken aktif.
    /// </summary>
    public class ShadowCabinet
    {
        public List<ShadowMinister> Members { get; set; } = new List<ShadowMinister>();

        // Sabitler
        public const int MaxMembers = 3;                // En fazla 3 gölge bakan
        public const float AppointmentCost = 25f;       // Atama maliyeti (sermaye)
        public const float DismissCost = 10f;           // Görevden alma maliyeti


        // İsim havuzu — portföye göre
        private static readonly Dictionary<string, string[]> NamePool = new Dictionary<string, string[]>
        {
            ["Ekonomi"] = new[] { "Prof. Dr. Ahmet Bey", "Dr. Selin Hanım", "Ekonomist Mert Bey", "Doç. Dr. Zeynep Hanım" },
            ["Adalet"]  = new[] { "Av. Kemal Bey", "Hukukçu Deniz Hanım", "Prof. Dr. Ayşe Hanım", "Hakim Sinan Bey" },
            ["Sosyal"]  = new[] { "Dr. Elif Hanım", "Sosyolog Cem Bey", "Eğitimci Fatma Hanım", "Psikolog Can Bey" }
        };

        /// <summary>Oyuncu muhalefetteyken gölge bakan atar.</summary>
        public string AppointShadowMinister(string portfolio, SimulationEngine e)
        {
            if (e.CurrentRole != SimulationEngine.PlayerRole.Opposition)
                return "Gölge kabine sadece muhalefetteyken kurulabilir.";

            if (Members.Count >= MaxMembers)
                return $"Gölge kabine dolu (max {MaxMembers} üye). Önce birini görevden al.";

            if (e.PoliticalCapital < AppointmentCost)
                return $"Yeterli siyasi sermaye yok ({AppointmentCost:F0} gerekir).";

            if (Members.Any(m => m.Portfolio == portfolio))
                return $"Bu portföyde zaten bir gölge bakan var: {Members.First(m => m.Portfolio == portfolio).Name}";

            // Yeni gölge bakan oluştur
            var minister = CreateShadowMinister(portfolio, e.CurrentTurn);
            Members.Add(minister);
            e.PoliticalCapital -= AppointmentCost;

            return $"{minister.Name} {portfolio} portföyüne gölge bakan olarak atandı. " +
                   $"(Yetkinlik: %{minister.Competence:F0}, Medya: %{minister.MediaSkill:F0})";
        }

        private ShadowMinister CreateShadowMinister(string portfolio, int currentTurn)
        {
            string[] pool = NamePool.ContainsKey(portfolio) ? NamePool[portfolio] : NamePool["Sosyal"];
            string name = pool[SimRng.Next(pool.Length)];
            string id = $"shadow_{portfolio.ToLower()}_{Guid.NewGuid().ToString().Substring(0, 4)}";

            var m = new ShadowMinister(id, name, portfolio)
            {
                Competence = 40f + (float)(SimRng.NextDouble() * 50f),  // 40-90
                MediaSkill = 40f + (float)(SimRng.NextDouble() * 50f),  // 40-90
                Loyalty = 60f + (float)(SimRng.NextDouble() * 30f),     // 60-90
                AppointedTurn = currentTurn
            };
            return m;
        }

        /// <summary>Gölge bakanı görevden al.</summary>
        public string DismissShadowMinister(string ministerId, SimulationEngine e)
        {
            var m = Members.FirstOrDefault(x => x.Id == ministerId);
            if (m == null) return "Gölge bakan bulunamadı.";

            if (e.PoliticalCapital < DismissCost)
                return $"Yeterli siyasi sermaye yok ({DismissCost:F0} gerekir).";

            e.PoliticalCapital -= DismissCost;
            Members.Remove(m);
            return $"{m.Name} görevden alındı.";
        }

        /// <summary>Her tur çağrılır. Gölge kabine pasif etki üretir.</summary>
        public void ProcessTurn(SimulationEngine e)
        {
            if (e.CurrentRole != SimulationEngine.PlayerRole.Opposition) return;

            // Her üye +2 sermaye üretir (medya faaliyetleri sayesinde)
            int bonus = Members.Count * 2;
            if (bonus > 0)
            {
                e.PoliticalCapital = Math.Min(e.MaxPoliticalCapital, e.PoliticalCapital + bonus);
            }

            // Medya becerisi yüksekse meşruiyet ufak artar
            float mediaBonus = Members.Sum(m => m.MediaSkill) / 300f;  // 0..1 arası
            if (mediaBonus > 0.5f)
            {
                e.Legitimacy.AdjustLegitimacy(mediaBonus * 2f);
            }

            // AI hükümetin yasalarını bloklama
            BlockGovernmentPolicies(e);
        }

        /// <summary>Gölge bakanlar hükümet yasalarını bloklar (oy baskısı).</summary>
        private void BlockGovernmentPolicies(SimulationEngine e)
        {
            // Meclis gündemindeki her yasa için blok şansı
            foreach (var policy in e.ProposedPolicies)
            {
                float blockChance = Members.Count * 0.15f;   // 3 üye = %45 blok şansı
                if (SimRng.NextDouble() < blockChance)
                {
                    // Yasa oylamasında baskıyı artır (bir sonraki ResolveVote'ta etkili olur)
                    // Bu, doğrudan baskı sistemiyle entegre olur
                    SimLogger.Log($"[Gölge Kabine] {policy.Name} yasası bloklanıyor!");
                }
            }
        }

        /// <summary>Oyuncu iktidara gelince gölge bakanları gerçek bakan yapar.</summary>
        public string PromoteToCabinet(SimulationEngine e)
        {
            if (Members.Count == 0) return "Gölge kabinede kimse yok.";

            var promoted = new List<string>();
            foreach (var m in Members.ToList())
            {
                // PoliticalActor olarak ekle
                var actor = new PoliticalActor(m.Id, m.Name, ActorRole.Minister)
                {
                    Ideology = e.PlayerGlobalAlignment,
                    Ambition = 40f,
                    Loyalty = m.Loyalty,
                    Competence = m.Competence,
                    Influence = 30f + m.MediaSkill * 0.2f,
                    Satisfaction = 70f
                };

                // Aynı ID'de actor varsa atla
                if (e.Actors.Any(a => a.Id == m.Id)) continue;

                e.AddActor(actor);
                promoted.Add(m.Name);
            }

            Members.Clear();

            if (promoted.Count == 0) return "Kimse terfi ettirilemedi.";
            return $"Gölge kabine dağıtıldı. {promoted.Count} isim bakanlığa atandı: {string.Join(", ", promoted)}";
        }

        public List<string> GetReport()
        {
            var lines = new List<string>();
            if (Members.Count == 0)
            {
                lines.Add("Gölge kabinede kimse yok. Atama yapmak için +25 sermaye gerekir.");
                return lines;
            }

            foreach (var m in Members)
            {
                lines.Add($"{m.Portfolio}: {m.Name} | Yetkinlik %{m.Competence:F0} | Medya %{m.MediaSkill:F0} | Sadakat %{m.Loyalty:F0}");
            }
            lines.Add($"Toplam tur bonusu: +{Members.Count * 2} sermaye/tur");
            return lines;
        }
    }
}