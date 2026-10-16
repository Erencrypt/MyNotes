using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MyNotes.Helpers;

namespace MyNotes.Views.Controls
{
    public sealed partial class AlertInfoBar : UserControl
    {
        public static readonly TimeSpan DefaultDuration = TimeSpan.FromSeconds(2);

        private Timer? _dismissTimer;

        public AlertInfoBar()
        {
            InitializeComponent();
        }

        public void Show(
            string title,
            string message,
            AlertInfoBarSeverity severity = AlertInfoBarSeverity.Informational,
            TimeSpan? duration = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(title);
            ArgumentException.ThrowIfNullOrWhiteSpace(message);

            StopDismissTimer();

            InnerInfoBar.Title = title;
            InnerInfoBar.Message = message;
            InnerInfoBar.Severity = MapSeverity(severity);
            InnerInfoBar.IsOpen = true;

            var showDuration = duration ?? DefaultDuration;
            if (showDuration > TimeSpan.Zero)
            {
                StartDismissTimer(showDuration);
            }
        }

        public void Hide()
        {
            StopDismissTimer();
            InnerInfoBar.IsOpen = false;
        }

        private void StartDismissTimer(TimeSpan duration)
        {
            var dueTimeMs = (int)Math.Min(duration.TotalMilliseconds, int.MaxValue);
            _dismissTimer = new Timer(_ =>
            {
                DispatcherQueue.TryEnqueue(Hide);
            }, null, dueTimeMs, Timeout.Infinite);
        }

        private void StopDismissTimer()
        {
            _dismissTimer?.Dispose();
            _dismissTimer = null;
        }

        private void InnerInfoBar_Closed(InfoBar sender, InfoBarClosedEventArgs args)
        {
            StopDismissTimer();
        }

        private static InfoBarSeverity MapSeverity(AlertInfoBarSeverity severity) => severity switch
        {
            AlertInfoBarSeverity.Success => InfoBarSeverity.Success,
            AlertInfoBarSeverity.Warning => InfoBarSeverity.Warning,
            AlertInfoBarSeverity.Error => InfoBarSeverity.Error,
            _ => InfoBarSeverity.Informational,
        };
    }
}
