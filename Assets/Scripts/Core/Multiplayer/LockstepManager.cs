using System;
using System.Collections.Generic;
using System.Linq;
using DemocracySim.Engine.Core.Commands;
using DemocracySim.Engine.World;

namespace DemocracySim.Engine.Core.Multiplayer
{
    /// <summary>
    /// FAZ 23: Lockstep multiplayer manager.
    /// Tüm oyuncular aynı turda aynı komutları uygular.
    /// Deterministik simülasyon + senkronizasyon.
    /// </summary>
    public class LockstepManager
    {
        public enum State { Idle, WaitingForPlayers, Simulating, Finished }

        public State CurrentState { get; private set; } = State.Idle;
        public int CurrentTurn { get; private set; } = 0;
        public int PlayerCount { get; private set; } = 0;

        private readonly CommandQueue _queue = new CommandQueue();
        private readonly List<string> _playerCountries = new List<string>();
        private readonly Dictionary<int, HashSet<string>> _readyPlayers = new Dictionary<int, HashSet<string>>();

        /// <summary>Yeni oyun başlat.</summary>
        public void StartGame(List<string> playerCountryIds, uint seed)
        {
            _playerCountries.Clear();
            _playerCountries.AddRange(playerCountryIds);
            PlayerCount = playerCountryIds.Count;
            _readyPlayers.Clear();
            _queue.Clear();
            CurrentTurn = 0;
            CurrentState = State.WaitingForPlayers;

            KapitalistRng.Initialize(seed);
            SimLogger.Log($"[Lockstep] Oyun başladı. {PlayerCount} oyuncu, seed: {seed}");
        }

        /// <summary>Oyuncu komutunu kaydet.</summary>
        public void SubmitCommand(SimCommand cmd)
        {
            if (CurrentState != State.WaitingForPlayers && CurrentState != State.Simulating)
                return;

            if (cmd.Turn != CurrentTurn)
            {
                SimLogger.Log($"[Lockstep] Komut farklı turdan: {cmd.Turn} (şu an {CurrentTurn})",
                    SimLogger.LogLevel.Warning);
                return;
            }

            _queue.Enqueue(cmd);

            // Oyuncuyu hazır olarak işaretle
            if (!_readyPlayers.ContainsKey(CurrentTurn))
                _readyPlayers[CurrentTurn] = new HashSet<string>();
            _readyPlayers[CurrentTurn].Add(cmd.CountryId);

            SimLogger.Log($"[Lockstep] {cmd.CountryId} komutu kaydedildi: {cmd.Type}");

            // Tüm oyuncular hazır mı?
            if (_readyPlayers[CurrentTurn].Count >= PlayerCount)
            {
                SimLogger.Log($"[Lockstep] Tur {CurrentTurn} için tüm oyuncular hazır!");
            }
        }

        /// <summary>Tüm oyuncular hazır mı?</summary>
        public bool AllPlayersReady()
        {
            return _readyPlayers.ContainsKey(CurrentTurn)
                && _readyPlayers[CurrentTurn].Count >= PlayerCount;
        }

        /// <summary>Tur'u simüle et (tüm komutları uygula).</summary>
        public void SimulateTurn(WorldManager world)
        {
            if (!AllPlayersReady())
            {
                SimLogger.Log($"[Lockstep] Tur {CurrentTurn} için hazır değil " +
                              $"({_readyPlayers.GetValueOrDefault(CurrentTurn, new HashSet<string>()).Count}/{PlayerCount})");
                return;
            }

            CurrentState = State.Simulating;

            // Komutları deterministik sırada uygula
            var turnCommands = _queue.GetForTurn(CurrentTurn);
            foreach (var cmd in turnCommands)
            {
                ApplyCommand(world, cmd);
            }

            // Dünya turunu ilerlet
            world.ProcessWorldTurn();

            CurrentTurn++;
            CurrentState = State.WaitingForPlayers;

            SimLogger.Log($"[Lockstep] Tur {CurrentTurn - 1} tamamlandı.");
        }

        private void ApplyCommand(WorldManager world, SimCommand cmd)
        {
            var country = world.Countries.FirstOrDefault(c => c.Id == cmd.CountryId);
            if (country == null) return;

            var e = country.Engine;
            switch (cmd.Type)
            {
                case CommandType.ProposePolicy:
                    e.ProposePolicy(cmd.TargetId);
                    break;
                case CommandType.ChangePolicyValue:
                    var pol = e.AllObjects.OfType<SimPolicy>().FirstOrDefault(p => p.Id == cmd.TargetId);
                    if (pol != null) pol.ActualValue += cmd.Value;
                    break;
                case CommandType.SignTradeAgreement:
                    e.Universe.TradePartners.Add(cmd.TargetId);
                    break;
                case CommandType.NextTurn:
                    // Zaten ProcessWorldTurn çağrılıyor
                    break;
                default:
                    SimLogger.Log($"[Lockstep] Uygulanmayan komut: {cmd.Type}",
                        SimLogger.LogLevel.Warning);
                    break;
            }
        }

        /// <summary>Replay için komut geçmişini dışa aktar.</summary>
        public string ExportReplay()
        {
            return _queue.Serialize();
        }

        /// <summary>Replay'den yükle.</summary>
        public void LoadReplay(string json)
        {
            _queue.Deserialize(json);
            SimLogger.Log($"[Lockstep] Replay yüklendi: {_queue.Count} komut");
        }
    }
}