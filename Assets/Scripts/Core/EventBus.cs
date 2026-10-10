using System;
using System.Collections.Generic;

namespace DemocracySim.Engine.Core
{
    /// <summary>
    /// Basit, ana thread'de çalışan event bus.
    /// Motor katmanı event yayınlar; UI katmanı dinler.
    /// Motor, UI'ı TANIMAZ — bu sayede test edilebilir ve yeniden kullanılabilir kalır.
    /// 
    /// KULLANIM:
    ///   Yayın: EventBus.Publish(new CrisisTriggeredEvent { CrisisId = "x", ... });
    ///   Dinle: EventBus.Subscribe<CrisisTriggeredEvent>(OnCrisis);
    /// </summary>
    public static class EventBus
    {
        private static readonly Dictionary<Type, List<Delegate>> _handlers = new Dictionary<Type, List<Delegate>>();

        public static void Subscribe<T>(Action<T> handler) where T : struct
        {
            if (handler == null) return;
            var type = typeof(T);
            if (!_handlers.TryGetValue(type, out var list))
            {
                list = new List<Delegate>();
                _handlers[type] = list;
            }
            if (!list.Contains(handler))
                list.Add(handler);
        }

        public static void Unsubscribe<T>(Action<T> handler) where T : struct
        {
            if (handler == null) return;
            if (_handlers.TryGetValue(typeof(T), out var list))
                list.Remove(handler);
        }

        public static void Publish<T>(T evt) where T : struct
        {
            if (!_handlers.TryGetValue(typeof(T), out var list)) return;
            // Kopya üzerinde döngü: handler içinde unsubscribe güvenli olsun
            var snapshot = list.ToArray();
            for (int i = 0; i < snapshot.Length; i++)
            {
                try { ((Action<T>)snapshot[i])?.Invoke(evt); }
                catch (Exception ex)
                {
                    SimLogger.Log($"[EventBus] {typeof(T).Name} handler hatası: {ex.Message}",
                        SimLogger.LogLevel.Error);
                }
            }
        }

        /// <summary>Sahne geçişlerinde tüm dinleyicileri temizle (opsiyonel).</summary>
        public static void Clear() => _handlers.Clear();
    }

    // ==========================================================
    // MOTOR EVENT'LERİ — Motor bu struct'ları yayınlar
    // ==========================================================

    /// <summary>Bir kriz tetiklendiğinde yayınlanır.</summary>
    public struct CrisisTriggeredEvent
    {
        public string CrisisId;
        public string Title;
        public float Severity;
    }

    /// <summary>Bir yasa kabul edildiğinde yayınlanır.</summary>
    public struct PolicyPassedEvent
    {
        public string PolicyId;
        public string PolicyName;
        public bool IsPlayerInitiated;
    }

    /// <summary>Bir yasa reddedildiğinde yayınlanır.</summary>
    public struct PolicyRejectedEvent
    {
        public string PolicyId;
        public string PolicyName;
    }

    /// <summary>Seçim sonucu açıklandığında yayınlanır.</summary>
    public struct ElectionResultEvent
    {
        public bool PlayerWon;
        public bool WasGoverning;
        public float PlayerSupport;
    }

    /// <summary>UI bildirimi için genel amaçlı event.</summary>
    public struct NotificationEvent
    {
        public string Message;
        public bool IsWarning;
    }

    /// <summary>Oyun bittiğinde yayınlanır.</summary>
    public struct GameOverEvent
    {
        public string Reason;
    }

    /// <summary>Başarım kazanıldığında yayınlanır.</summary>
    public struct AchievementUnlockedEvent
    {
        public string AchievementId;
        public string Name;
        public string Description;
    }

    /// <summary>Bir tur tamamlandığında yayınlanır.</summary>
    public struct TurnCompletedEvent
    {
        public int TurnNumber;
        public string CountryId;
    }
}