using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace DemocracySim.Engine.Core
{
    /// <summary>
    /// FAZ R: Tur sistemleri pipeline'ı.
    /// Sistemleri öncelik sırasına göre çalıştırır, hataları yakalar ve raporlar.
    /// </summary>
    public class TurnSystemRegistry
    {
        private readonly List<ITurnSystem> _systems = new List<ITurnSystem>();
        private readonly Dictionary<string, long> _lastExecutionMs = new Dictionary<string, long>();

        /// <summary>Bir sistemi pipeline'a kaydeder.</summary>
        public void Register(ITurnSystem system)
        {
            if (system == null) return;
            _systems.Add(system);
            _systems.Sort((a, b) => a.Priority.CompareTo(b.Priority));
        }

        // EK-8: Hata sayacı — UI'da gösterebilirsiniz
private int _errorCountThisTurn = 0;
public int ErrorCountThisTurn => _errorCountThisTurn;

public void ProcessAll(SimulationEngine engine)
{
    _errorCountThisTurn = 0;
    
    foreach (var system in _systems)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            system.ProcessTurn(engine);
        }
        catch (Exception ex)
        {
            _errorCountThisTurn++;
            SimLogger.Log(
                $"[Pipeline] {system.SystemName} hatası: {ex.Message}\n{ex.StackTrace}",
                SimLogger.LogLevel.Error);

#if UNITY_EDITOR
            // EK-8: Editör'de hatayı fırlat ki geliştirici görsün.
            // Build'de yut ki oyuncu oyunu kaybetmesin.
            throw;
#endif
        }
        finally
        {
            sw.Stop();
            _lastExecutionMs[system.SystemName] = sw.ElapsedMilliseconds;
        }
    }
}

        /// <summary>Performans raporu (hangi sistem ne kadar sürdü).</summary>
        public List<string> GetPerformanceReport()
        {
            return _lastExecutionMs
                .OrderByDescending(kv => kv.Value)
                .Select(kv => $"{kv.Key}: {kv.Value}ms")
                .ToList();
        }

        /// <summary>Kayıtlı sistem sayısı.</summary>
        public int Count => _systems.Count;
    }
}
