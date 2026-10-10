using System;
using System.Collections.Generic;

namespace DemocracySim.Engine.Core.Multiplayer
{
    public enum MultiplayerMode
    {
        SinglePlayer,    // Tek oyuncu (mevcut sistem)
        HotSeat,         // Aynı bilgisayar, sırayla
        PBEM,            // Dosya gönder/al
        P2P              // Online P2P
    }

    /// <summary>
    /// FAZ 23.1: Oturum verisi — tüm multiplayer modellerinin ortak temeli.
    /// Hot-Seat, PBEM, P2P hepsi bunu kullanır.
    /// </summary>
    [Serializable]
    public class SessionData
    {
        public MultiplayerMode Mode { get; set; } = MultiplayerMode.SinglePlayer;
        public uint MasterSeed { get; set; }
        public List<PlayerSlot> Players { get; set; } = new List<PlayerSlot>();
        public int CurrentPlayerIndex { get; set; } = 0;
        public int CurrentTurn { get; set; } = 0;
        public string SessionName { get; set; } = "Oturum";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime LastPlayedAt { get; set; } = DateTime.Now;

        public PlayerSlot CurrentPlayer =>
            (CurrentPlayerIndex >= 0 && CurrentPlayerIndex < Players.Count)
                ? Players[CurrentPlayerIndex]
                : null;

        public bool IsHotSeat => Mode == MultiplayerMode.HotSeat;
        public bool IsPBEM => Mode == MultiplayerMode.PBEM;
        public bool IsP2P => Mode == MultiplayerMode.P2P;
        public bool IsSingle => Mode == MultiplayerMode.SinglePlayer;
    }

    /// <summary>Bir oyuncu slotu — hangi ülkeyi yönetiyor?</summary>
    [Serializable]
    public class PlayerSlot
    {
        public int Index { get; set; }
        public string PlayerName { get; set; } = "Oyuncu";
        public string CountryId { get; set; }
        public string CountryName { get; set; }
        public bool IsHuman { get; set; } = true;
        public bool HasFinishedTurn { get; set; } = false;
        public string Color { get; set; } = "#F2B84B";   // UI için

        public PlayerSlot() { }

        public PlayerSlot(int index, string playerName, string countryId, string countryName)
        {
            Index = index;
            PlayerName = playerName;
            CountryId = countryId;
            CountryName = countryName;
        }
    }
}