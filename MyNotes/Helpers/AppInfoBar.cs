using MyNotes.Views.Controls;
using System.Diagnostics;

namespace MyNotes.Helpers
{
    public static class AppInfoBar
    {
        public static readonly TimeSpan DefaultDuration = AlertInfoBar.DefaultDuration;
        //TODO: localize evety instance its used
        public static void Show(
            string title,
            string message,
            AlertInfoBarSeverity severity = AlertInfoBarSeverity.Informational,
            TimeSpan? duration = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(title);
            ArgumentException.ThrowIfNullOrWhiteSpace(message);

            if (MainWindow.AppAlertInfoBar is null)
            {
                Debug.WriteLine($"AppInfoBar not initialized yet. Title: {title}. Message: {message}");
                return;
            }
            if (App._mainWindow == null)
            { return; }
            App._mainWindow.DispatcherQueue.TryEnqueue(() =>
            {
                var infoBar = MainWindow.AppAlertInfoBar;
                if (infoBar is null)
                {
                    Debug.WriteLine($"AppInfoBar was null when trying to show: {title} - {message}");
                    return;
                }

                infoBar.Show(title, message, severity, duration ?? DefaultDuration);
            });
        }
    }
    public enum AlertInfoBarSeverity
    {
        Informational,
        Success,
        Warning,
        Error
    }
}
