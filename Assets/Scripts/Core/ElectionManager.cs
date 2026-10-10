using System;
using System.Collections.Generic;
using System.Linq;
using DemocracySim.Engine.Core;
using DemocracySim.Engine.Legislative;

namespace DemocracySim.Engine.Core
{
    public class ElectionManager
{
    public float LastPlayerSupport { get; private set; } = 0f;
    public List<Party> LastParties { get; private set; } = new List<Party>();

    // FAZ 2: Seçim modeli faktörleri
    public float LeaderImage { get; set; } = 50f;
    public int ConsecutiveTerms { get; set; } = 0;
    public float ScandalScar { get; set; } = 0f;

    // EK-13: Muhalefet momentum — hükümetin hatalarından faydalanma
    // 0-100 arası. Muhalefetteyken her tur, hükümetin kötü performansına göre artar.
    // Seçimde muhalefet oyuna bonus sağlar.
    public float OppositionMomentum { get; set; } = 0f;
    public const float MaxOppositionMomentum = 100f;

    public float IncumbencyFatigue => ConsecutiveTerms * BalanceConfig.Instance.IncumbencyFatiguePerTerm;
        public bool RunElection(SimulationEngine engine, PartyManager partyManager, bool log = true)
{
    if (engine == null || partyManager == null) return false;

    var voteShares = CalculateVoteShares(engine, partyManager);
    partyManager.AllocateSeats(voteShares);

    float playerSupport = voteShares.ContainsKey("player") ? voteShares["player"] : 0f;
    LastPlayerSupport = playerSupport;

    var playerParty = partyManager.Parties.FirstOrDefault(p => p.Id == "player");
    if (playerParty == null)
    {
        playerParty = new Party("player", "Sizin Partiniz", engine.PlayerGlobalAlignment, 0);
        partyManager.Parties.Add(playerParty);
    }
    playerParty.Ideology = engine.PlayerGlobalAlignment;

    // EK-20: Hükümet kurma kararı — gerçekçi koalisyon pazarlığı
    bool won;
    string reason;

    if (partyManager.HasMajority("player"))
    {
        // Tek başına çoğunluk → kesin kazandı
        won = true;
        reason = "tek başına çoğunluk";
    }
    else
    {
        // En büyük parti oyuncu mu?
        var largest = partyManager.GetLargestParty();
        if (largest == null || largest.Id != "player")
        {
            // Oyuncu en büyük parti değil → muhalefet
            won = false;
            reason = $"en büyük parti {largest?.Name ?? "?"} (oyuncu 2.)";
        }
        else
        {
            // Oyuncu en büyük parti → koalisyon kurabilir
            // Ama AI partiler ancak ideolojik yakınlık VE kendi çıkarı varsa katılır
            var coalition = partyManager.GetCoalitionCandidates("player", PartyManager.TotalSeats / 2);
            int totalSeats = playerParty.Seats + coalition.Sum(p => p.Seats);
            won = totalSeats > PartyManager.TotalSeats / 2;

            reason = won
                ? $"koalisyon ({coalition.Count} ortak, {totalSeats}/{PartyManager.TotalSeats})"
                : $"koalisyon kurulamadı ({totalSeats}/{PartyManager.TotalSeats})";
        }
    }

    engine.CurrentRole = won
        ? SimulationEngine.PlayerRole.Governing
        : SimulationEngine.PlayerRole.Opposition;

    if (won) ConsecutiveTerms++;
    else ConsecutiveTerms = 0;
    // EK-23: Yenilgi sayacı
if (won) engine.ConsecutiveElectionLosses = 0;
else engine.ConsecutiveElectionLosses++;

    ScandalScar = Math.Max(0f, ScandalScar - 20f);
    LastParties = partyManager.Parties.Where(p => p.Seats > 0).ToList();

    if (log)
    {
        SimLogger.Log($"[Seçim] Oyuncu: %{playerSupport:F1} → {(won ? $"KAZANDI ({reason})" : $"KAYBETTİ ({reason})")}");
    }
    return won;
}

        /// <summary>Her parti için oy yüzdesi hesapla (halk grupları bazında).</summary>
        private Dictionary<string, float> CalculateVoteShares(SimulationEngine engine, PartyManager partyManager)
{
    // FAZ 3.5: Voter transition sistemini kullan
    if (engine.VoterTransition != null && engine.VoterTransition.VotingBlocs.Count > 0)
    {
        var transitionShares = engine.VoterTransition.GetVoteShares(engine.Demographics);
        if (transitionShares.Count > 0)
        {
            ApplyElectionModifiers(engine, transitionShares);
            return transitionShares;
        }
    }

    var shares = new Dictionary<string, float>();
    foreach (var p in partyManager.Parties) shares[p.Id] = 0f;
    if (!shares.ContainsKey("player")) shares["player"] = 0f;

    float totalWeight = 0f;

    foreach (var group in engine.Demographics)
    {
        float groupWeight = group.Influence;
        totalWeight += groupWeight;

        float groupAlignment = GroupIdeologyBias(group.Id);
        float playerIdeo = engine.PlayerGlobalAlignment;

        // EK-20: TÜM partiler aynı formülle hesaplanır.
        // Taban oy YOK. Sadece memnuniyet + ideolojik uyum.
        // Oyuncu partisi için memnuniyet daha ağırlıklı (iktidar performansı).

        // Oyuncu partisi: halk memnuniyeti (iktidar performansı) + ideoloji
        float playerMemSat = group.Satisfaction;
        float playerIdeoMatch = 100f - Math.Abs(playerIdeo - groupAlignment);
        float playerSupport = (playerMemSat * 0.5f) + (playerIdeoMatch * 0.3f);
        shares["player"] += playerSupport * groupWeight;

        // AI partiler: sadece ideolojik uyum (memnuniyet yok)
        foreach (var party in partyManager.Parties)
        {
            if (party.Id == "player") continue;
            float partyIdeoMatch = 100f - Math.Abs(party.Ideology - groupAlignment);
            float partySupport = partyIdeoMatch * 0.5f;   // EK-20: sabit +20 YOK
            shares[party.Id] += partySupport * groupWeight;
        }
    }

    // Normalize et — toplam 100 olsun
    float total = shares.Values.Sum();
    if (total <= 0f) return shares;

    var keys = shares.Keys.ToList();
    foreach (var k in keys) shares[k] = (shares[k] / total) * 100f;

    ApplyElectionModifiers(engine, shares);
    return shares;
}

        private void ApplyElectionModifiers(SimulationEngine engine, Dictionary<string, float> shares)
        {
            bool governing = engine.CurrentRole == SimulationEngine.PlayerRole.Governing;

            if (governing)
            {
                float imageEffect = (LeaderImage - 50f) * BalanceConfig.Instance.LeaderImageWeight;
                float fatigueEffect = -IncumbencyFatigue;
                float scandalEffect = -ScandalScar * 0.5f;

                float inflationPenalty = Math.Max(0f, (float)engine.Economy.Inflation - 10f) * 1.2f;
                float unemploymentVal = engine.Registry.GetValue(ObjectRegistry.Ids.Unemployment, 10f);
                float unemploymentPenalty = Math.Max(0f, unemploymentVal - 10f) * 1.0f;
                float economicPenalty = Math.Min(25f, inflationPenalty + unemploymentPenalty);

                shares["player"] += imageEffect + fatigueEffect + scandalEffect - economicPenalty;
            }
            else
            {
                shares["player"] += (LeaderImage - 50f) * 0.15f;
            }

            // Normalize tekrar
                       // Normalize — toplam 100 olsun, negatif değerleri kırp
            var keys = shares.Keys.ToList();
            foreach (var k in keys) shares[k] = Math.Max(0f, shares[k]);

            float total = shares.Values.Sum();
            if (total <= 0f)
            {
                // Beklenmedik durum: eşit dağıt
                float equal = 100f / Math.Max(1, keys.Count);
                foreach (var k in keys) shares[k] = equal;
                return;
            }

            foreach (var k in keys) shares[k] = (shares[k] / total) * 100f;
                    // EK-13: Muhalefet momentumunu seçim oyuna uygula
        // Muhalefetteki oyuncu, iktidarın kötü gidişatından faydalanır
        if (!governing && OppositionMomentum > 0f)
        {
            // Momentum 0-100 → Oy bonusu 0-15
            float momentumBonus = (OppositionMomentum / MaxOppositionMomentum) * 15f;
            shares["player"] = Math.Min(100f, shares["player"] + momentumBonus);
            SimLogger.Log($"[Seçim] Muhalefet momentumu: %{OppositionMomentum:F0} → +{momentumBonus:F1} oy bonusu");
        }
        }

        /// <summary>Halk grubunun ideolojik eğilimi (-100 sol, +100 sağ).</summary>
       private float GroupIdeologyBias(string groupId)
{
    return groupId switch
    {
        "workers"       => -50f,
        "intellectuals" => -30f,
        "capitalists"   =>  50f,
        "conservatives" =>  60f,
        // FAZ 3: Yeni gruplar
        "youth"         => -20f,   // Gençler genelde muhalif
        "rural"         =>  40f,   // Kırsal muhafazakar
        "retirees"      =>  20f,   // Emekliler statükocu
        "students"      => -40f,   // Öğrenciler sol eğilimli
        _               =>   0f
    };
}

        public void AdjustLeaderImage(float delta)
        {
            LeaderImage = Math.Clamp(LeaderImage + delta, 0f, 100f);
        }

        public void AddScandalScar(float amount)
        {
            ScandalScar = Math.Clamp(ScandalScar + amount, 0f, 100f);
        }

            /// <summary>
    /// EK-13: Her turda çağrılır. Muhalefetteyken hükümetin performansına göre
    /// momentum artar. İktidardayken momentum sıfırlanır.
    /// </summary>
    public void ProcessOppositionMomentum(SimulationEngine e)
    {
        if (e.CurrentRole == SimulationEngine.PlayerRole.Governing)
        {
            // İktidardayken momentum yavaşça sıfırlanır (oyuncunun kendi performansı)
            OppositionMomentum = Math.Max(0f, OppositionMomentum - 5f);
            return;
        }

        // Muhalefetteyiz — hükümetin kötü performansı momentumu artırır
        float delta = 0f;

        // 1) Hükümet meşruiyeti düşükse (50'nin altı)
        float legit = e.Legitimacy.CurrentLegitimacy;
        if (legit < 50f) delta += (50f - legit) * 0.15f;   // Max +7.5/tur

        // 2) Sokak huzursuzluğu yüksekse
        if (e.Universe.Unrest > 40f) delta += (e.Universe.Unrest - 40f) * 0.1f;   // Max +6/tur

        // 3) Enflasyon yüksekse
        if (e.Economy.Inflation > 10f) delta += (float)Math.Min(5f, (e.Economy.Inflation - 10f) * 0.3f);

        // 4) Radikalleşmiş gruplar varsa
        if (e.Universe.Radicalized != null)
        {
            int radicals = e.Universe.Radicalized.Count(kv => kv.Value);
            delta += radicals * 1.5f;
        }

        // 5) Muhalefet partisinin meclis gücü (sandalye oranı)
        var player = e.PartyManager.Parties.FirstOrDefault(p => p.Id == "player");
        if (player != null && player.Seats > 0)
            delta += player.Seats * 0.08f;   // 30 sandalye = +2.4/tur

        // Momentum'u güncelle
        OppositionMomentum = Math.Clamp(OppositionMomentum + delta, 0f, MaxOppositionMomentum);

        // Log (sadece önemli değişim olduğunda)
        if (delta >= 5f)
        {
            SimLogger.Log($"[Muhalefet] Momentum +{delta:F1} → %{OppositionMomentum:F0}. " +
                          "Halk hükümetten memnun değil.", SimLogger.LogLevel.Warning);
        }
    }
    }
}