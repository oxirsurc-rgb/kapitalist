using System;

namespace DemocracySim.Engine.Core
{
    public static class SimLogger
    {
        public enum LogLevel { Info, Warning, Error }
        
        // Unity'deyken Debug.Log'a, konsoldayken Console.WriteLine'a yönlendirir.
        public static Action<string, LogLevel> OnLog;

        public static void Log(string message, LogLevel level = LogLevel.Info)
        {
            string prefix = level switch {
                LogLevel.Warning => "[WARNING]",
                LogLevel.Error => "[ERROR]",
                _ => "[INFO]"
            };
            
            string formatted = $"{prefix} {message}";
            OnLog?.Invoke(formatted, level);
            
            // Eğer dışarıdan bir dinleyici yoksa varsayılan konsola yaz
            if (OnLog == null) Console.WriteLine(formatted);
        }
    }
}
