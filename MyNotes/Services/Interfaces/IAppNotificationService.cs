using Microsoft.Windows.AppNotifications;

namespace MyNotes.Services.Interfaces
{
    public interface IAppNotificationService
    {
        Task InitializeAsync();

        Task<bool> ShowReminderAsync(Guid reminderId, string title, string message, string time, bool isAlarm);
        bool ShowDeletedMessage(string title, string message);
        bool ShowInfoMessage(string title, string message);
        void OnNotificationInvoked(AppNotificationManager sender, AppNotificationActivatedEventArgs args);
        void Unregister();
    }
}
