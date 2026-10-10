using System;
using System.Collections.Generic;
using System.Linq;
using DemocracySim.Engine.Core;

namespace DemocracySim.Engine.Legislative
{
    /// <summary>
    /// FAZ 2: Parti yönetimi — seçim, sandalye dağılımı, koalisyon.
    /// Araştırma: D'Hondt orantılı temsil sistemi.
    /// </summary>
    [Serializable]
    public class PartyManager
    {
        public const int TotalSeats = 100;
        public const float ElectionThreshold = 10f;     // %10 baraj

        public List<Party> Parties { get; set; } = new List<Party>();

        public PartyManager()
        {
            InitializeDefaultParties();
        }

        private void InitializeDefaultParties()
        {
            Parties.Add(new Party("workers",      "İşçi Partisi",       -70f, 0));
            Parties.Add(new Party("conservative", "Muhafazakar Parti",   70f, 0));
            Parties.Add(new Party("nationalist",  "Milliyetçi Parti",    85f, 0));
            Parties.Add(new Party("liberal",      "Liberal Parti",       20f, 0));
            Parties.Add(new Party("green",        "Yeşil Parti",        -30f, 0));
        }

        /// <summary>
        /// FAZ 2: Seçim sonuçlarına göre sandalyeleri D'Hondt sistemiyle dağıt.
        /// voteShares: parti id → oy yüzdesi (toplam 100).
        /// </summary>
                /// <summary>
        /// FAZ 2: Seçim sonuçlarına göre sandalyeleri D'Hondt sistemiyle dağıt.
        /// Toplam 100 sandalye dağıtılır — hiçbir parti boşta kalmaz.
        /// </summary>
        public void AllocateSeats(Dictionary<string, float> voteShares)
        {
            // 1. Sandalyeleri sıfırla
            foreach (var p in Parties) p.Seats = 0;

            if (voteShares == null || voteShares.Count == 0)
            {
                SimLogger.Log("[Meclis] Oy payı verisi yok, sandalye dağıtılamadı.", SimLogger.LogLevel.Error);
                return;
            }

            // 2. Baraj kontrolü — %10'un altındaki partileri filtrele
            var eligible = voteShares
                .Where(kv => kv.Value >= ElectionThreshold)
                .ToDictionary(kv => kv.Key, kv => kv.Value);

            // 3. Barajı geçen yoksa, en yüksek oy alan partileri al (en az 3 parti)
            if (eligible.Count < 3)
            {
                eligible = voteShares
                    .OrderByDescending(kv => kv.Value)
                    .Take(Math.Min(3, voteShares.Count))
                    .ToDictionary(kv => kv.Key, kv => kv.Value);
                SimLogger.Log($"[Meclis] Barajı geçen parti yok, ilk {eligible.Count} parti meclise girdi.", SimLogger.LogLevel.Warning);
            }

            // 4. D'Hondt yöntemi — toplam 100 sandalye dağıt
            var quotients = new Dictionary<string, float>();
            foreach (var kv in eligible) quotients[kv.Key] = Math.Max(1f, kv.Value);

            for (int seat = 0; seat < TotalSeats; seat++)
            {
                if (quotients.Count == 0) break;

                var winner = quotients.OrderByDescending(kv => kv.Value).First();
                var party = Parties.FirstOrDefault(p => p.Id == winner.Key);
                if (party == null) continue;

                party.Seats++;
                quotients[winner.Key] = eligible[winner.Key] / (party.Seats + 1);
            }

            // 5. Debug log
            int totalAllocated = Parties.Sum(p => p.Seats);
            SimLogger.Log($"[Meclis] {totalAllocated} sandalye dağıtıldı. Partiler: {string.Join(", ", Parties.Where(p => p.Seats > 0).Select(p => $"{p.Name}={p.Seats}"))}");
        }
                // ============================================================
        // FAZ 2 AŞAMA D: GÜVENSİZLİK ÖNERGESİ
        // ============================================================

        /// <summary>Aktif güvensizlik önergesi var mı?</summary>
        public bool IsVoteOfNoConfidenceActive { get; set; } = false;
        public int NoConfidenceTurnsLeft { get; set; } = 0;

        public const float NoConfidenceCapitalCost = 25f;
        public const float NoConfidenceMinSupport = 30f;  // %30 muhalefet desteği
        public const int NoConfidenceDuration = 3;         // 3 tur oylama

        /// <summary>Oyuncu güvensizlik önergesi verebilir mi?</summary>
        public string CanFileNoConfidence(SimulationEngine e)
        {
            if (e.CurrentRole != SimulationEngine.PlayerRole.Opposition)
                return "Güvensizlik önergesi sadece muhalefetteyken verilebilir.";
            if (IsVoteOfNoConfidenceActive)
                return $"Zaten aktif bir önerge var ({NoConfidenceTurnsLeft} tur kaldı).";
            if (e.PoliticalCapital < NoConfidenceCapitalCost)
                return $"Yeterli sermaye yok ({NoConfidenceCapitalCost:F0} gerekir).";

            float support = CalculateOppositionSupport(e);
            if (support < NoConfidenceMinSupport)
                return $"Muhalefet desteği yetersiz (%{support:F0}, en az %{NoConfidenceMinSupport:F0} gerekir).";

            return null;
        }

        /// <summary>Muhalefet desteğini hesapla (halk memnuniyeti + meşruiyet eksikliği).</summary>
        public float CalculateOppositionSupport(SimulationEngine e)
        {
            float support = 0f;

            // 1) Halk memnuniyetsizliği (yüksekse muhalefet güçlenir)
            float avgSatisfaction = 50f;
            if (e.Demographics.Count > 0)
                avgSatisfaction = e.Demographics.Average(g => g.Satisfaction);
            support += Math.Max(0f, (100f - avgSatisfaction)) * 0.4f;

            // 2) Meşruiyet düşüklüğü
            support += Math.Max(0f, (100f - e.Legitimacy.CurrentLegitimacy)) * 0.3f;

            // 3) Huzursuzluk
            support += e.Universe.Unrest * 0.2f;

            // 4) Radikalleşmiş gruplar
            if (e.Universe.Radicalized != null)
            {
                int radicals = e.Universe.Radicalized.Count(kv => kv.Value);
                support += radicals * 5f;
            }

            // 5) Oyuncunun partisinin sandalye oranı (ne kadar büyükse o kadar güçlü)
            var player = Parties.FirstOrDefault(p => p.Id == "player");
            if (player != null)
                support += player.Seats * 0.3f;

            return Math.Clamp(support, 0f, 100f);
        }

        /// <summary>Güvensizlik önergesi ver.</summary>
        public string FileNoConfidence(SimulationEngine e)
        {
            string blocker = CanFileNoConfidence(e);
            if (blocker != null) return blocker;

            e.PoliticalCapital -= NoConfidenceCapitalCost;
            IsVoteOfNoConfidenceActive = true;
            NoConfidenceTurnsLeft = NoConfidenceDuration;

            float support = CalculateOppositionSupport(e);

            SimLogger.Log($"[Güvensizlik] Muhalefet önerge verdi! Destek: %{support:F0}, {NoConfidenceDuration} tur oylama.", SimLogger.LogLevel.Warning);

            return $"Güvensizlik önergesi verildi! ({NoConfidenceCapitalCost:F0} sermaye harcandı)\n" +
                   $"Muhalefet desteği: %{support:F0}. {NoConfidenceDuration} tur sonra oylama.";
        }

        /// <summary>Her tur ilerlet. Oylama sonucunu döndürür.</summary>
        public string ProcessNoConfidenceTurn(SimulationEngine e)
        {
            if (!IsVoteOfNoConfidenceActive) return null;

            NoConfidenceTurnsLeft--;

            if (NoConfidenceTurnsLeft > 0)
            {
                SimLogger.Log($"[Güvensizlik] Önerge oylaması: {NoConfidenceTurnsLeft} tur kaldı.");
                return null;
            }

            // Oylama zamanı
            IsVoteOfNoConfidenceActive = false;
            return ResolveNoConfidence(e);
        }

        /// <summary>Önerge oylamasını sonuçlandır.</summary>
        private string ResolveNoConfidence(SimulationEngine e)
        {
            float support = CalculateOppositionSupport(e);

            // Rastgelelik faktörü ±10
            float roll = support + (float)(SimRng.NextDouble() * 20 - 10);

            // Parti dağılımına bak — iktidarın sandalye sayısı
            // FAZ 3.5 DÜZELTME: Oyuncu partisi de hükümet sandalyelerine dahil
int govSeats = Parties
    .Where(p => p.IsInGovernment)
    .Sum(p => p.Seats);
            int oppSeats = TotalSeats - govSeats;

            // Oy hesabı: muhalefet + kaçak oylar
            float noConfidenceVotes = Math.Min(oppSeats + roll * 0.5f, TotalSeats);
            float govVotes = TotalSeats - noConfidenceVotes;

            if (noConfidenceVotes > TotalSeats / 2)
            {
                // Hükümet düştü
                e.CurrentRole = SimulationEngine.PlayerRole.Governing;
                e.TurnUntilElection = 12;
                e.Legitimacy.AdjustLegitimacy(-5f);

                SimLogger.Log($"[Güvensizlik] HÜKÜMET DÜŞTÜ! Muhalefet kazandı. Oy: {noConfidenceVotes:F0}/{TotalSeats}", SimLogger.LogLevel.Warning);

                return $"HÜKÜMET DÜŞTÜ! Muhalefet kazandı. (%{noConfidenceVotes:F0} destek)\n" +
                       $"Oyuncu iktidara geçti. Meclis yeni hükümeti onayladı.";
            }
            else
            {
                // Önerge başarısız
                e.Legitimacy.AdjustLegitimacy(-3f);
                e.PoliticalCapital = Math.Max(0f, e.PoliticalCapital - 10f);

                SimLogger.Log($"[Güvensizlik] Önerge başarısız. Oy: {noConfidenceVotes:F0}/{TotalSeats}", SimLogger.LogLevel.Warning);

                return $"Güvensizlik önergesi BAŞARISIZ oldu. (%{noConfidenceVotes:F0} destek yetersiz)\n" +
                       $"Hükümet ayakta. Muhalefet prestij kaybetti.";
            }
        }
                /// <summary>FAZ 2: Milletvekillerini ideolojik yakınlığa göre partilere ata.</summary>
        public void AssignActorsToParties(List<PoliticalActor> actors)
        {
            // Önce tüm partilerin üye listesini temizle
            foreach (var p in Parties) p.MemberIds.Clear();

            foreach (var actor in actors)
            {
                if (actor.Role != ActorRole.MP && actor.Role != ActorRole.OppositionLeader) continue;

                // En yakın partiyi bul (ideolojik mesafe)
                Party closest = null;
                float minDistance = float.MaxValue;
                foreach (var p in Parties)
                {
                    if (p.Id == "player") continue;   // Oyuncuya kimse atanmaz
                    float dist = Math.Abs(p.Ideology - actor.Ideology);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        closest = p;
                    }
                }

                if (closest != null)
                {
                    actor.PartyId = closest.Id;
                    closest.MemberIds.Add(actor.Id);
                }
            }

            SimLogger.Log($"[Meclis] {actors.Count(a => !string.IsNullOrEmpty(a.PartyId))} milletvekili partilere atandı.");
        }

        /// <summary>Oyuncunun partisi mecliste çoğunluğa sahip mi?</summary>
        public bool HasMajority(string partyId)
        {
            var p = Parties.FirstOrDefault(x => x.Id == partyId);
            return p != null && p.Seats > TotalSeats / 2;
        }

        /// <summary>En büyük parti (formateur) hangisi?</summary>
        public Party GetLargestParty()
        {
            return Parties.OrderByDescending(p => p.Seats).FirstOrDefault();
        }

        /// <summary>Bir parti için uygun koalisyon ortaklarını bul (ideolojik yakınlık).</summary>
        public List<Party> GetCoalitionCandidates(string partyId, int seatsNeeded)
{
    var main = Parties.FirstOrDefault(p => p.Id == partyId);
    if (main == null) return new List<Party>();

    // EK-20: AI partiler ancak ideolojik yakınlık < 60 ise koalisyona katılır.
    // Aksi halde "gönülsüz" katılır ve hükümeti zayıflatır.
    var candidates = Parties
        .Where(p => p.Id != partyId && p.Seats > 0)
        .Where(p => Math.Abs(p.Ideology - main.Ideology) < 60f)   // 🔴 YENİ: ideolojik filtre
        .OrderBy(p => Math.Abs(p.Ideology - main.Ideology))       // En yakın önce
        .ToList();

    var result = new List<Party>();
    int totalSeats = main.Seats;
    foreach (var c in candidates)
    {
        // EK-20: Parti büyüklüğüne göre katılma olasılığı
        // Büyük partiler daha gönüllü katılır
        float willingness = c.Seats / 30f;   // 30 sandalye = %100 gönüllü
        float ideologyFit = 1f - (Math.Abs(c.Ideology - main.Ideology) / 60f);
        float chance = Math.Min(1f, willingness + ideologyFit * 0.5f);

        // Deterministik RNG ile karar ver
        var rng = KapitalistRng.For("coalition_negotiation");
        if (rng.NextDouble() > chance) continue;   // Bu parti katılmıyor

        result.Add(c);
        totalSeats += c.Seats;
        if (totalSeats > TotalSeats / 2) break;
    }
    return result;
}

        /// <summary>Meclis kompozisyonunu döndürür (UI için).</summary>
        public List<string> GetReport()
        {
            var lines = new List<string>();
            lines.Add($"MECLİS KOMPOZİSYONU ({TotalSeats} sandalye)");
            foreach (var p in Parties.OrderByDescending(p => p.Seats))
            {
                if (p.Seats == 0) continue;
                string gov = p.IsInGovernment ? " [HÜKÜMET]" : "";
                lines.Add($"  {p.Name} ({p.SideLabel()}): {p.Seats} sandalye{gov}");
            }
            return lines;
        }
                // ============================================================
        // FAZ 2: KOALİSYON YÖNETİMİ
        // ============================================================

        /// <summary>Aktif koalisyon ortakları (oyuncunun partisi hariç).</summary>
        public List<string> CoalitionPartnerIds { get; set; } = new List<string>();

        /// <summary>Oyuncunun partisi çoğunluğa sahip mi?</summary>
        public bool PlayerHasMajority()
        {
            var player = Parties.FirstOrDefault(p => p.Id == "player");
            return player != null && player.Seats > TotalSeats / 2;
        }

        /// <summary>Oyuncu + ortaklar toplam çoğunluk sağlıyor mu?</summary>
        public bool HasCoalitionMajority()
        {
            var player = Parties.FirstOrDefault(p => p.Id == "player");
            if (player == null) return false;

            int total = player.Seats;
            foreach (var id in CoalitionPartnerIds)
            {
                var p = Parties.FirstOrDefault(x => x.Id == id);
                if (p != null) total += p.Seats;
            }
            return total > TotalSeats / 2;
        }

        /// <summary>Koalisyona ortak ekle.</summary>
        public string AddCoalitionPartner(string partyId, SimulationEngine e)
        {
            if (partyId == "player") return "Kendi partinizi ortak yapamazsınız.";
            var party = Parties.FirstOrDefault(p => p.Id == partyId);
            if (party == null) return "Parti bulunamadı.";
            if (party.Seats == 0) return "Bu partinin mecliste sandalyesi yok.";
            if (CoalitionPartnerIds.Contains(partyId)) return "Bu parti zaten koalisyon ortağı.";

            // İdeolojik mesafe kontrolü
            var player = Parties.FirstOrDefault(p => p.Id == "player");
            float distance = Math.Abs(party.Ideology - (player?.Ideology ?? 0f));
            if (distance > 80f)
                return $"İdeolojik fark çok büyük (mesafe: {distance:F0}). Koalisyon kurulamaz.";

            // Politik sermaye maliyeti
            float cost = 25f + distance * 0.5f;
            if (e.PoliticalCapital < cost)
                return $"Yeterli sermaye yok ({cost:F0} gerekir).";

            e.PoliticalCapital -= cost;
            CoalitionPartnerIds.Add(partyId);
            party.IsInGovernment = true;
            party.Satisfaction = 60f;

            return $"{party.Name} koalisyona katıldı! (-{cost:F0} sermaye)";
        }

        /// <summary>Koalisyondan ortağı çıkar.</summary>
        public string RemoveCoalitionPartner(string partyId)
        {
            if (!CoalitionPartnerIds.Contains(partyId)) return "Bu parti zaten ortak değil.";
            var party = Parties.FirstOrDefault(p => p.Id == partyId);
            CoalitionPartnerIds.Remove(partyId);
            if (party != null) party.IsInGovernment = false;
            return $"{party?.Name} koalisyondan çıkarıldı.";
        }

        /// <summary>Her turda koalisyon ortaklarının memnuniyetini güncelle.</summary>
        public void ProcessCoalitionTurn(SimulationEngine e)
        {
            foreach (var id in CoalitionPartnerIds.ToList())
            {
                var party = Parties.FirstOrDefault(p => p.Id == id);
                if (party == null) continue;

                // İdeolojik uyuma göre memnuniyet değişimi
                var player = Parties.FirstOrDefault(p => p.Id == "player");
                float distance = Math.Abs(party.Ideology - (player?.Ideology ?? 0f));
                float delta = distance < 30f ? +0.5f : (distance > 60f ? -1.5f : 0f);
                
                // Oyuncunun meşruiyeti düşükse ortaklar tedirgin olur
                if (e.Legitimacy.CurrentLegitimacy < 40f) delta -= 0.5f;

                party.Satisfaction = Math.Clamp(party.Satisfaction + delta, 0f, 100f);

                // Ortak çekilme kontrolü
                if (party.Satisfaction < 20f && e.CurrentRole == SimulationEngine.PlayerRole.Governing)
                {
                    RemoveCoalitionPartner(id);
                    e.Legitimacy.AdjustLegitimacy(-10f);
                    e.Universe.Unrest = Math.Clamp(e.Universe.Unrest + 15f, 0f, 100f);
                    SimLogger.Log($"[Koalisyon] {party.Name} hükümetten çekildi! Meşruiyet -10.", SimLogger.LogLevel.Warning);
                    continue;
                }

                // Düşük memnuniyet uyarısı
                if (party.Satisfaction < 30f)
                    SimLogger.Log($"[Koalisyon] {party.Name} ortaklıktan memnun değil (%{party.Satisfaction:F0}).", SimLogger.LogLevel.Warning);
            }
        }
                /// <summary>FAZ 2: Oyun başında meclisi doldur (seçim öncesi).</summary>
        public void AllocateInitialSeats()
        {
            // FAZ 3.5 DÜZELTME: Oyuncu partisini başlangıçta meclise ekle
if (Parties.All(p => p.Id != "player"))
{
    Parties.Add(new Party("player", "Sizin Partiniz", 0f, 0));
}
            // Başlangıçta her partiye eşit dağıt (veya ideolojiye göre ağırlıklı)
            int baseSeats = TotalSeats / Parties.Count;
            int remainder = TotalSeats % Parties.Count;

            for (int i = 0; i < Parties.Count; i++)
            {
                Parties[i].Seats = baseSeats + (i < remainder ? 1 : 0);
            }
            SimLogger.Log($"[Meclis] Başlangıç sandalyeleri dağıtıldı. Toplam: {TotalSeats}");
        }

        /// <summary>Parti disiplini ile oy kararı — parti yönü belirler.</summary>
        public int GetPartyVoteDirection(Party party, SimPolicy policy, SimulationEngine engine)
        {
            // Parti ideolojisi ile yasa ideolojisi arasındaki mesafe
            float diff = Math.Abs(party.Ideology - policy.IdeologicalAlignment);
            // 0-30 → +1 (destekle), 30-70 → 0 (çekimser), 70+ → -1 (karşı çık)
            if (diff < 30f) return 1;
            if (diff < 70f) return 0;
            return -1;
        }
    }
}