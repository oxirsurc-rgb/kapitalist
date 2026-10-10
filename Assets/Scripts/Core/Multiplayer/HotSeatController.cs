using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using DemocracySim.Engine.Core.Multiplayer;
using DemocracySim.Engine.World;

namespace DemocracySim.Engine.Core
{
    /// <summary>
    /// FAZ 23.1: Hot-Seat kontrolcüsü.
    /// Sıra yönetimi + "sıradaki oyuncu" gizleme ekranı.
    /// GameManager'dan çağrılır.
    /// </summary>
    public class HotSeatController
    {
        public event Action<PlayerSlot> OnShowPassScreen;   // "Sıra Oyuncu X'de"
        public event Action<PlayerSlot> OnTurnStarted;      // Yeni oyuncu başladı
        public event Action<int> OnRoundCompleted;          // Tur bitti (tüm oyuncular oynadı)

        private readonly WorldManager _world;
        private bool _waitingForPass = false;

        public HotSeatController(WorldManager world)
        {
            _world = world;
        }

        /// <summary>
        /// Mevcut oyuncunun turu bitti → sonraki oyuncuya geç.
        /// </summary>
        public void RequestNextPlayer()
{
    var session = SessionManager.ActiveSession;
    if (session == null || !session.IsHotSeat) return;

    var coord = SessionManager.Coordinator;
    if (coord == null)
    {
        SimLogger.Log("[HotSeat] TurnCoordinator yok!", SimLogger.LogLevel.Error);
        return;
    }

    // Mevcut oyuncu bitirdi → coordinator ilerletsin
    coord.FinishCurrentTurn();

    // Sıradaki oyuncunun pass screen'ini göster
    _waitingForPass = true;
    OnShowPassScreen?.Invoke(session.CurrentPlayer);
}

        /// <summary>
        /// Oyuncu "Hazırım" dedi → tur başlasın.
        /// </summary>
        public void ConfirmPlayerReady()
{
    if (!_waitingForPass) return;
    _waitingForPass = false;

    var session = SessionManager.ActiveSession;
    var current = session?.CurrentPlayer;
    if (current == null) return;

    SimLogger.Log($"[HotSeat] Sıra başlıyor: {current.PlayerName} ({current.CountryName})");
    OnTurnStarted?.Invoke(current);
}

        /// <summary>Şu an "pass screen" gösteriliyor mu?</summary>
        public bool IsWaitingForPass => _waitingForPass;

        /// <summary>Sıradaki oyuncunun ülkesini al.</summary>
        public Country GetCurrentCountry()
        {
            return SessionManager.Coordinator?.GetCurrentCountry();
        }

        /// <summary>Rapor (UI için).</summary>
        public List<string> GetStatusReport()
        {
            return SessionManager.Coordinator?.GetReport() ?? new List<string>();
        }
    }
}