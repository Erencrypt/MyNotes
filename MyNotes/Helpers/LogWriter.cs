using System.Diagnostics;
using System.Text;

namespace MyNotes.Helpers
{
    internal static class LogWriter
    {
        private static readonly string filePath = Path.Combine(AppConfig.MyNotesFolderPath, "log.txt");
        // The reminder worker thread and the UI thread can log at the same time.
        private static readonly Lock _lock = new();

        public enum LogLevel { Debug, Info, Warning, Error }

        public static void Log(string logMessage, LogLevel logLevel)
        {
            if (logLevel == LogLevel.Debug)
            {
#if DEBUG
                Debug.Print("Debug Log: {0}", logMessage);
#endif
                return;
            }

            try
            {
                var entry = FormatEntry(logMessage, logLevel);
                lock (_lock)
                {
                    Directory.CreateDirectory(AppConfig.MyNotesFolderPath);
                    File.AppendAllText(filePath, entry);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to write log entry: {ex.Message}");
            }
        }

        private static string FormatEntry(string logMessage, LogLevel logLevel)
        {
            var now = DateTime.Now;
            var sb = new StringBuilder();
            sb.Append("Log Entry : ");
            sb.AppendLine($"{now.ToLongTimeString()} {now.ToLongDateString()}");
            sb.AppendLine($"Log Level : {logLevel}");
            sb.AppendLine($"  :{logMessage}");
            sb.AppendLine("-------------------------------\r\n");
            return sb.ToString();
        }

        /// <summary>
        /// Trims the log file to the newest 500 lines once it reaches 1000 lines.
        /// </summary>
        public static void CheckLogFile()
        {
            try
            {
                lock (_lock)
                {
                    if (!File.Exists(filePath))
                    {
                        return;
                    }

                    var lines = File.ReadAllLines(filePath);
                    if (lines.Length >= 1000)
                    {
                        File.WriteAllLines(filePath, lines.Skip(lines.Length - 500));
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to trim log file: {ex.Message}");
            }
        }
    }
}
