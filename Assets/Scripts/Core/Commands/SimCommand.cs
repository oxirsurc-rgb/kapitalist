using System;

namespace DemocracySim.Engine.Core.Commands
{
    /// <summary>
    /// FAZ 23: Multiplayer için komut pattern.
    /// Her oyuncu eylemi bir SimCommand olarak paketlenir.
    /// Aynı komut dizisi + aynı seed = aynı simülasyon.
    /// </summary>
    [Serializable]
    public struct SimCommand
    {
        public int Turn;                // Hangi turda yapıldı
        public string CountryId;        // Hangi ülke
        public CommandType Type;        // Komut tipi
        public string TargetId;         // Hedef (policy, event, vs.)
        public float Value;             // Değer (frame, choice index, vs.)

        public SimCommand(int turn, string countryId, CommandType type, string targetId = null, float value = 0f)
        {
            Turn = turn;
            CountryId = countryId;
            Type = type;
            TargetId = targetId;
            Value = value;
        }

        public override string ToString()
        {
            return $"[T{Turn}] {CountryId} → {Type}({TargetId}, {Value})";
        }
    }

    public enum CommandType
    {
        // Oyuncu eylemleri
        ProposePolicy,      // Value: frame seçimi
        ChangePolicyValue,  // Value: delta
        StartCampaign,      // TargetId: policy_id
        GivePopulistPromise,// TargetId: group_id
        PersuadeMinister,   // TargetId: actor_id
        NeutralizeRival,    // TargetId: actor_id
        Propaganda,
        TriggerScandal,

        // Diplomatik
        SignTradeAgreement, // TargetId: country_id
        ImproveRelation,    // TargetId: country_id, Value: amount

        // Seçim
        StartRally,         // TargetId: group_id, Value: theme
        FoundParty,         // TargetId: party_id
        ConcedeFaction,     // TargetId: faction_id

        // Kriz/Olay
        CrisisChoice,       // Value: choice_index

        // Sistem
        NextTurn,           // Oyuncu turu bitir
        EndSimulation
    }
}