using System;
using System.Collections.Generic;
using Mirror;

namespace DemocracySim.Engine.Core.Multiplayer.Online
{
    [Serializable]
    public struct NetworkPlayerInfo
    {
        public int ConnectionId;
        public string PlayerName;
        public string CountryId;
        public string CountryName;
        public bool IsReady;
        public bool IsHost;
    }

    public struct LobbyJoinMsg : NetworkMessage
    {
        public string PlayerName;
        public string CountryId;
    }

    public struct LobbyStateMsg : NetworkMessage
    {
        public NetworkPlayerInfo[] Players;
        public bool CanStart;
    }

    public struct LobbyReadyMsg : NetworkMessage
    {
        public bool IsReady;
    }

    public struct LobbySelectCountryMsg : NetworkMessage
    {
        public string CountryId;
    }

    public struct GameStartMsg : NetworkMessage
    {
        public uint Seed;
        public NetworkPlayerInfo[] Players;
    }

    public struct TurnFinishedMsg : NetworkMessage
    {
        public string CountryId;
        public int Turn;
    }

    public struct AdvanceTurnMsg : NetworkMessage
    {
        public int NewTurn;
        public string SummaryText;
    }

    public struct OnlineChatMessage : NetworkMessage
    {
        public string SenderName;
        public string Text;
    }
}
