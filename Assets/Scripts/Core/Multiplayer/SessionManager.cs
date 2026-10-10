using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using UnityEngine;
using DemocracySim.Engine.World;

namespace DemocracySim.Engine.Core.Multiplayer
{
    /// <summary>
    /// FAZ 23.1: Oturum yöneticisi.
    /// Hot-Seat ve PBEM için oturum oluşturma, kaydetme, yükleme.
    /// </summary>
    public static class SessionManager
    {
        public static SessionData ActiveSession { get; private set; }
        public static TurnCoordinator Coordinator { get; private set; }

        public static bool IsMultiplayer => ActiveSession != null && !ActiveSession.IsSingle;

        private static string SaveDir =>
            Path.Combine(Application.persistentDataPath, "sessions");

        // ═══════════════════════════════════════════════════════════
        // HOT-SEAT BAŞLAT
        // ═══════════════════════════════════════════════════════════
        public static SessionData CreateHotSeat(List<PlayerSlot> players, uint seed)
        {
            if (players == null || players.Count < 2)
            {
                SimLogger.Log("[Session] Hot-Seat için en az 2 oyuncu gerekli!", SimLogger.LogLevel.Error);
                return null;
            }

            ActiveSession = new SessionData
            {
                Mode = MultiplayerMode.HotSeat,
                MasterSeed = seed,
                Players = players,
                SessionName = $"Hot-Seat ({players.Count} oyuncu)",
                CurrentTurn = 0,
                CurrentPlayerIndex = 0
            };

            KapitalistRng.Initialize(seed);
            SimLogger.Log($"[Session] Hot-Seat başlatıldı: {players.Count} oyuncu, seed {seed}");

            return ActiveSession;
        }

        // ═══════════════════════════════════════════════════════════
        // TUR KOORDİNATÖRÜNÜ BAŞLAT
        // ═══════════════════════════════════════════════════════════
        public static void StartTurnCoordination(WorldManager world)
        {
            if (ActiveSession == null) return;

            Coordinator = new TurnCoordinator(ActiveSession, world);
            Coordinator.StartFirstTurn();

            SimLogger.Log($"[Session] Sıra başladı: {ActiveSession.CurrentPlayer.PlayerName}");
        }

        // ═══════════════════════════════════════════════════════════
        // PBEM İÇİN OTURUM KAYDET
        // ═══════════════════════════════════════════════════════════
        public static string SaveSession(string fileName = null)
        {
            if (ActiveSession == null) return null;

            Directory.CreateDirectory(SaveDir);
            string path = Path.Combine(SaveDir,
                fileName ?? $"{ActiveSession.SessionName}_{DateTime.Now:yyyyMMdd_HHmm}.session.json");

            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(ActiveSession, options);
                File.WriteAllText(path, json);
                SimLogger.Log($"[Session] Kaydedildi: {path}");
                return path;
            }
            catch (Exception e)
            {
                SimLogger.Log($"[Session] Kayıt hatası: {e.Message}", SimLogger.LogLevel.Error);
                return null;
            }
        }

        public static SessionData LoadSession(string path)
        {
            if (!File.Exists(path))
            {
                SimLogger.Log($"[Session] Dosya yok: {path}", SimLogger.LogLevel.Error);
                return null;
            }

            try
            {
                string json = File.ReadAllText(path);
                ActiveSession = JsonSerializer.Deserialize<SessionData>(json);
                SimLogger.Log($"[Session] Yüklendi: {ActiveSession.SessionName}, tur {ActiveSession.CurrentTurn}");
                return ActiveSession;
            }
            catch (Exception e)
            {
                SimLogger.Log($"[Session] Yükleme hatası: {e.Message}", SimLogger.LogLevel.Error);
                return null;
            }
        }

        public static void EndSession()
        {
            ActiveSession = null;
            Coordinator = null;
            SimLogger.Log("[Session] Oturum kapatıldı.");
        }
    }
}