using System;
using System.IO;

namespace OllamaUsageTracker
{
    public static class Logger
    {
        private const string LogFile = "debug.log";
        public static bool IsDebugEnabled { get; set; } = false;

        public static void Log(string message)
        {
            if (!IsDebugEnabled) return;

            try
            {
                File.AppendAllText(LogFile, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}{Environment.NewLine}");
            }
            catch { }
        }
    }
}
