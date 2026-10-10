using System;
using System.Collections.Generic;
using System.Linq;
using DemocracySim.Engine.World;

namespace DemocracySim.Engine.Core.Multiplayer
{
    /// <summary>
    /// FAZ 23.1: Sıra yöneticisi — tüm multiplayer modelleri için ortak.
    /// Kim sırada? Tur bitti mi? Sonraki oyuncu kim?
    /// </summary>
    public class TurnCoordinator
    {
        public SessionData Session { get; private set; }
        public WorldManager World { get; private set; }

        public event Action<PlayerSlot> OnTurnStarted;
        public event Action<PlayerSlot> OnTurnFinished;
        public event Action OnRoundCompleted;

        public TurnCoordinator(SessionData session, WorldManager world)
        {
            Session = session;
            World = world;
        }

        /// <summary>Sıradaki oyuncunun ülkesini al.</summary>
        public Country GetCurrentCountry()
        {
            var slot = Session.CurrentPlayer;
            if (slot == null) return null;
            return World.Countries.FirstOrDefault(c => c.Id == slot.CountryId);
        }

        /// <summary>Mevcut oyuncu turunu bitirir.</summary>
        public void FinishCurrentTurn()
        {
            var slot = Session.CurrentPlayer;
            if (slot == null) return;

            slot.HasFinishedTurn = true;
            OnTurnFinished?.Invoke(slot);

            // Tüm oyuncular bitirdi mi?
            if (Session.Players.All(p => p.HasFinishedTurn))
            {
                CompleteRound();
            }
            else
            {
                AdvanceToNextUnfinishedPlayer();
            }
        }

        /// <summary>Sonraki oyuncuya geç (bitirmemiş olan).</summary>
        private void AdvanceToNextUnfinishedPlayer()
        {
            int start = Session.CurrentPlayerIndex;
            for (int i = 1; i <= Session.Players.Count; i++)
            {
                int next = (start + i) % Session.Players.Count;
                if (!Session.Players[next].HasFinishedTurn)
                {
                    Session.CurrentPlayerIndex = next;
                    OnTurnStarted?.Invoke(Session.CurrentPlayer);
                    return;
                }
            }
        }

        /// <summary>Tur tamamlandı — herkes sırayı bitirdi.</summary>
        private void CompleteRound()
        {
            // Dünya turunu ilerlet
            World.ProcessWorldTurn();

            // Tur sayacını artır
            Session.CurrentTurn++;
            Session.LastPlayedAt = DateTime.Now;

            // Oyuncu bayraklarını sıfırla
            foreach (var p in Session.Players)
                p.HasFinishedTurn = false;

            Session.CurrentPlayerIndex = 0;
            OnRoundCompleted?.Invoke();
            OnTurnStarted?.Invoke(Session.CurrentPlayer);
        }

        /// <summary>Yeni turu başlat — tüm oyuncular sıfırdan başlar.</summary>
        public void StartFirstTurn()
        {
            foreach (var p in Session.Players)
                p.HasFinishedTurn = false;
            Session.CurrentPlayerIndex = 0;
            OnTurnStarted?.Invoke(Session.CurrentPlayer);
        }

        /// <summary>Rapor: kim sırada, kaç kişi bekliyor.</summary>
        public List<string> GetReport()
        {
            var lines = new List<string>();
            lines.Add($"Mod: {Session.Mode}");
            lines.Add($"Tur: {Session.CurrentTurn}");
            lines.Add($"Sırada: {Session.CurrentPlayer?.PlayerName} ({Session.CurrentPlayer?.CountryName})");
            lines.Add("Oyuncular:");
            foreach (var p in Session.Players)
            {
                string status = p.HasFinishedTurn ? "✓ bitti" : "⏳ bekliyor";
                string marker = p.Index == Session.CurrentPlayerIndex ? " ← SIRA" : "";
                lines.Add($"  [{p.Index}] {p.PlayerName} — {p.CountryName} [{status}]{marker}");
            }
            return lines;
        }
    }
}