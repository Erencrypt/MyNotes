using System.Diagnostics;
using Windows.Storage;

namespace MyNotes.Helpers
{
    internal class LogWriter
    {
        private static readonly string filePath = App.StorageFolder.Path + "\\" + "log.txt";
        public enum LogLevel { Debug, Info, Warning, Error }
        public LogWriter(string logMessage, LogLevel logLevel)
        {
            Log(logMessage, logLevel);
        }
        public static async void Log(string logMessage, LogLevel logLevel)
        {
            try
            {
                if (logLevel == LogLevel.Debug)
                {
#if DEBUG
                    Debug.Print("Debug Log: {0}",logMessage);
#endif
                }
                else
                {
                    using StreamWriter w = File.AppendText(filePath);
                    Loger(logMessage, w, logLevel);
                }
            }
            catch (Exception ex)
            {
                await Console.Out.WriteLineAsync(ex.Message);
            }
        }

        private static void Loger(string logMessage, TextWriter txtWriter, LogLevel logLevel)
        {
            try
            {
                txtWriter.Write("Log Entry : ");
                txtWriter.WriteLine("{0} {1}", DateTime.Now.ToLongTimeString(), DateTime.Now.ToLongDateString());
                txtWriter.WriteLine("Log Level : {0}", logLevel);
                txtWriter.WriteLine("  :{0}", logMessage);
                txtWriter.WriteLine("-------------------------------\r\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public static async void CheckLogFile()
        {
            await App.StorageFolder.CreateFileAsync("log.txt", CreationCollisionOption.OpenIfExists);
            var lines = File.ReadAllLines(filePath);

            if (lines.Length >= 1000)
            {
                File.WriteAllLines(filePath, lines.Skip(500).ToArray());
            }
        }
    }
}
