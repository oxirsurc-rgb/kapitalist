using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;
using DemocracySim.Engine.World;

namespace DemocracySim.Engine.Core.Multiplayer.Online
{
    /// <summary>
    /// Mirror tabanlı Çevrim İçi Multiplayer Yöneticisi.
    /// Sunucu (Host) ve İstemci (Client) bağlantılarını, lobi durumunu ve tur senkronizasyonunu yönetir.
    /// </summary>
    [DisallowMultipleComponent]
    public class DemocracyNetworkManager : NetworkManager
    {
        public static new DemocracyNetworkManager singleton => (DemocracyNetworkManager)NetworkManager.singleton;
        public static DemocracyNetworkManager Instance { get; private set; }

        public bool IsOnlineActive => NetworkServer.active || NetworkClient.active;
        public bool IsServerHost => NetworkServer.active;
        public bool IsClientOnly => NetworkClient.isConnected && !NetworkServer.active;

        public string LocalPlayerName { get; set; } = "Oyuncu";
        public string LocalCountryId { get; set; } = "turkey";

        // Lobi Durumu
        public readonly List<NetworkPlayerInfo> LobbyPlayers = new List<NetworkPlayerInfo>();
        public readonly List<string> ChatMessages = new List<string>();

        // Event'ler (UI Presenter dinler)
        public event Action<List<NetworkPlayerInfo>, bool> OnLobbyStateChanged;
        public event Action<GameStartMsg> OnGameStarted;
        public event Action<int, string> OnTurnAdvanced;
        public event Action<string, string> OnChatReceived;
        public event Action<string> OnNetworkLog;

        private readonly Dictionary<int, NetworkPlayerInfo> _serverPlayers = new Dictionary<int, NetworkPlayerInfo>();
        private readonly HashSet<string> _finishedCountriesInTurn = new HashSet<string>();

        public override void Awake()
        {
            base.Awake();
            if (Instance == null) Instance = this;

            // Transport yapılandırması
            if (transport == null)
            {
                var telepathy = GetComponent<TelepathyTransport>();
                if (telepathy == null) telepathy = gameObject.AddComponent<TelepathyTransport>();
                transport = telepathy;
                Transport.active = telepathy;
            }

            // Sahne otomatik geçişini kapatıyoruz (DemocracySim tek sahne uGUI üzerinde çalışır)
            dontDestroyOnLoad = true;
            runInBackground = true;
        }

        public static DemocracyNetworkManager EnsureInstance()
        {
            if (Instance != null) return Instance;
            var go = new GameObject("DemocracyNetworkManager");
            var manager = go.AddComponent<DemocracyNetworkManager>();
            return manager;
        }

        // ═══════════════════════════════════════════════════════════════════
        // HOST / CLIENT BAŞLATMA
        // ═══════════════════════════════════════════════════════════════════
        public void HostGame(string playerName, string countryId)
        {
            LocalPlayerName = playerName;
            LocalCountryId = countryId;
            networkAddress = "localhost";
            StartHost();
            Log($"[Network] Host başlatıldı (Port: {GetPort()}).");
        }

        public void JoinGame(string ipAddress, string playerName, string countryId)
        {
            LocalPlayerName = playerName;
            LocalCountryId = countryId;
            networkAddress = string.IsNullOrWhiteSpace(ipAddress) ? "localhost" : ipAddress.Trim();
            StartClient();
            Log($"[Network] Sunucuya bağlanılıyor: {networkAddress}...");
        }

        public void DisconnectSession()
        {
            if (NetworkServer.active && NetworkClient.isConnected)
            {
                StopHost();
            }
            else if (NetworkClient.isConnected)
            {
                StopClient();
            }
            else if (NetworkServer.active)
            {
                StopServer();
            }
            _serverPlayers.Clear();
            LobbyPlayers.Clear();
            _finishedCountriesInTurn.Clear();
            Log("[Network] Bağlantı kesildi.");
        }

        public ushort GetPort()
        {
            if (transport is TelepathyTransport t) return t.port;
            return 7777;
        }

        // ═══════════════════════════════════════════════════════════════════
        // SERVER TARAFI
        // ═══════════════════════════════════════════════════════════════════
        public override void OnStartServer()
        {
            base.OnStartServer();
            _serverPlayers.Clear();
            _finishedCountriesInTurn.Clear();

            NetworkServer.RegisterHandler<LobbyJoinMsg>(OnServerLobbyJoin);
            NetworkServer.RegisterHandler<LobbyReadyMsg>(OnServerLobbyReady);
            NetworkServer.RegisterHandler<LobbySelectCountryMsg>(OnServerLobbySelectCountry);
            NetworkServer.RegisterHandler<TurnFinishedMsg>(OnServerTurnFinished);
            NetworkServer.RegisterHandler<OnlineChatMessage>(OnServerChatMessage);
        }

        public override void OnServerConnect(NetworkConnectionToClient conn)
        {
            base.OnServerConnect(conn);
            Log($"[Server] Yeni bağlantı: ID {conn.connectionId}");
        }

        public override void OnServerDisconnect(NetworkConnectionToClient conn)
        {
            if (_serverPlayers.ContainsKey(conn.connectionId))
            {
                var p = _serverPlayers[conn.connectionId];
                _serverPlayers.Remove(conn.connectionId);
                Log($"[Server] {p.PlayerName} ayrıldı.");
                BroadcastLobbyState();
            }
            base.OnServerDisconnect(conn);
        }

        private void OnServerLobbyJoin(NetworkConnectionToClient conn, LobbyJoinMsg msg)
        {
            bool isHost = conn.connectionId == 0;
            var info = new NetworkPlayerInfo
            {
                ConnectionId = conn.connectionId,
                PlayerName = string.IsNullOrWhiteSpace(msg.PlayerName) ? $"Oyuncu_{conn.connectionId}" : msg.PlayerName,
                CountryId = msg.CountryId,
                CountryName = msg.CountryId,
                IsReady = isHost,
                IsHost = isHost
            };
            _serverPlayers[conn.connectionId] = info;
            Log($"[Server] {info.PlayerName} lobiye katıldı ({info.CountryId}).");
            BroadcastLobbyState();
        }

        private void OnServerLobbyReady(NetworkConnectionToClient conn, LobbyReadyMsg msg)
        {
            if (_serverPlayers.TryGetValue(conn.connectionId, out var info))
            {
                info.IsReady = msg.IsReady;
                _serverPlayers[conn.connectionId] = info;
                BroadcastLobbyState();
            }
        }

        private void OnServerLobbySelectCountry(NetworkConnectionToClient conn, LobbySelectCountryMsg msg)
        {
            if (_serverPlayers.TryGetValue(conn.connectionId, out var info))
            {
                info.CountryId = msg.CountryId;
                info.CountryName = msg.CountryId;
                _serverPlayers[conn.connectionId] = info;
                BroadcastLobbyState();
            }
        }

        private void BroadcastLobbyState()
        {
            var list = _serverPlayers.Values.ToArray();
            bool canStart = list.Length >= 1 && list.All(p => p.IsReady);
            var msg = new LobbyStateMsg { Players = list, CanStart = canStart };
            NetworkServer.SendToAll(msg);
        }

        public void ServerStartGame()
        {
            if (!NetworkServer.active) return;
            uint seed = (uint)UnityEngine.Random.Range(1000, 999999);
            var msg = new GameStartMsg
            {
                Seed = seed,
                Players = _serverPlayers.Values.ToArray()
            };
            NetworkServer.SendToAll(msg);
            Log("[Server] Oyun başlatma sinyali tüm istemcilere gönderildi!");
        }

        private void OnServerTurnFinished(NetworkConnectionToClient conn, TurnFinishedMsg msg)
        {
            if (!_serverPlayers.TryGetValue(conn.connectionId, out var info)) return;

            _finishedCountriesInTurn.Add(info.CountryId);
            Log($"[Server] {info.PlayerName} ({info.CountryId}) turunu tamamladı.");

            // Tüm insan oyuncular turunu bitirdi mi?
            int humanCount = _serverPlayers.Count;
            if (_finishedCountriesInTurn.Count >= humanCount)
            {
                _finishedCountriesInTurn.Clear();
                int nextTurn = msg.Turn + 1;
                Log($"[Server] Tüm oyuncular turunu tamamladı! Tur {nextTurn} başlatılıyor.");

                // Tüm istemcilere tur atlama sinyali gönder
                NetworkServer.SendToAll(new AdvanceTurnMsg
                {
                    NewTurn = nextTurn,
                    SummaryText = $"Tur {nextTurn} başladı!"
                });
            }
        }

        private void OnServerChatMessage(NetworkConnectionToClient conn, OnlineChatMessage msg)
        {
            // Tüm oyunculara sohbeti dağıt
            NetworkServer.SendToAll(msg);
        }

        // ═══════════════════════════════════════════════════════════════════
        // CLIENT TARAFI
        // ═══════════════════════════════════════════════════════════════════
        public override void OnStartClient()
        {
            base.OnStartClient();
            NetworkClient.RegisterHandler<LobbyStateMsg>(OnClientLobbyState);
            NetworkClient.RegisterHandler<GameStartMsg>(OnClientGameStart);
            NetworkClient.RegisterHandler<AdvanceTurnMsg>(OnClientAdvanceTurn);
            NetworkClient.RegisterHandler<OnlineChatMessage>(OnClientChatMessage);
        }

        public override void OnClientConnect()
        {
            base.OnClientConnect();
            Log("[Client] Sunucuya bağlanıldı. Lobi kaydı yapılıyor...");
            NetworkClient.Send(new LobbyJoinMsg
            {
                PlayerName = LocalPlayerName,
                CountryId = LocalCountryId
            });
        }

        public override void OnClientDisconnect()
        {
            Log("[Client] Sunucu bağlantısı koptu.");
            base.OnClientDisconnect();
        }

        private void OnClientLobbyState(LobbyStateMsg msg)
        {
            LobbyPlayers.Clear();
            if (msg.Players != null) LobbyPlayers.AddRange(msg.Players);
            OnLobbyStateChanged?.Invoke(LobbyPlayers, msg.CanStart);
        }

        private void OnClientGameStart(GameStartMsg msg)
        {
            Log($"[Client] Çevrim içi simülasyon başladı! Seed: {msg.Seed}");
            OnGameStarted?.Invoke(msg);
        }

        private void OnClientAdvanceTurn(AdvanceTurnMsg msg)
        {
            Log($"[Client] Tur {msg.NewTurn} senkronize edildi.");
            OnTurnAdvanced?.Invoke(msg.NewTurn, msg.SummaryText);
        }

        private void OnClientChatMessage(OnlineChatMessage msg)
        {
            string line = $"{msg.SenderName}: {msg.Text}";
            ChatMessages.Add(line);
            if (ChatMessages.Count > 40) ChatMessages.RemoveAt(0);
            OnChatReceived?.Invoke(msg.SenderName, msg.Text);
        }

        // ═══════════════════════════════════════════════════════════════════
        // İSTEMCİ EYLEMLERİ
        // ═══════════════════════════════════════════════════════════════════
        public void SendReadyToggle(bool isReady)
        {
            if (!NetworkClient.isConnected) return;
            NetworkClient.Send(new LobbyReadyMsg { IsReady = isReady });
        }

        public void SendSelectCountry(string countryId)
        {
            if (!NetworkClient.isConnected) return;
            LocalCountryId = countryId;
            NetworkClient.Send(new LobbySelectCountryMsg { CountryId = countryId });
        }

        public void SendTurnFinished(int currentTurn)
        {
            if (!NetworkClient.isConnected) return;
            NetworkClient.Send(new TurnFinishedMsg
            {
                CountryId = LocalCountryId,
                Turn = currentTurn
            });
            Log("[Network] Tur bitirme sinyali sunucuya iletildi. Diğer oyuncular bekleniyor...");
        }

        public void SendChat(string text)
        {
            if (!NetworkClient.isConnected || string.IsNullOrWhiteSpace(text)) return;
            NetworkClient.Send(new OnlineChatMessage
            {
                SenderName = LocalPlayerName,
                Text = text.Trim()
            });
        }

        private void Log(string msg)
        {
            Debug.Log(msg);
            OnNetworkLog?.Invoke(msg);
        }
    }
}
