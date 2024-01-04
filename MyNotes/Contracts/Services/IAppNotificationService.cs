using Microsoft.Windows.AppNotifications;
using System.Collections.Specialized;

namespace MyNotes.Contracts.Services;

public interface IAppNotificationService
{
    void Initialize();

    bool ShowReminder(string title, string message, string time, bool isAlarm);
    bool ShowDeletedMessage(string title, string message);
    bool ShowInfoMessage(string title, string message);


    NameValueCollection ParseArguments(string arguments);

    void Unregister();
    void OnNotificationInvoked(AppNotificationManager sender, AppNotificationActivatedEventArgs args);
}
