using Microsoft.Windows.AppNotifications;
using MyNotes.Contracts.Services;
using MyNotes.ViewModels;
using MyNotes.Views;
using MyNotes.Helpers;
using System.Collections.Specialized;
using System.Web;

namespace MyNotes.Notifications;

public class AppNotificationService : IAppNotificationService
{
    private readonly INavigationService _navigationService;

    public AppNotificationService(INavigationService navigationService)
    {
        _navigationService = navigationService;
    }

    ~AppNotificationService()
    {
        Unregister();
    }

    public void Initialize()
    {
        AppNotificationManager.Default.NotificationInvoked += OnNotificationInvoked;

        AppNotificationManager.Default.Register();
    }

    public void OnNotificationInvoked(AppNotificationManager sender, AppNotificationActivatedEventArgs args)
    {
        IDictionary<string, string> userInput = args.UserInput;
        string ar = args.Argument;
        if (ar == "snooze")
        {
            App.MainWindow.DispatcherQueue.TryEnqueue(() =>
            {
                int input = Convert.ToInt32(userInput["snoozeTime"]);
                DateTime dt = Convert.ToDateTime(App.InvokedReminders[0].DateTime);
                App.InvokedReminders[0].DateTime = dt.AddMinutes(input).ToString();
                App.ReminderSnoozed();
            });
        }
        else if (ar == "dismiss")
        {
            App.MainWindow.DispatcherQueue.TryEnqueue(() =>
            {
                App.ReminderDismissed();
            });
        }
        else if (ar == "trash")
        {
            App.MainWindow.DispatcherQueue.TryEnqueue(() =>
            {
                TrashPage.NtfInvoke = true;
                _navigationService.NavigateTo(typeof(TrashViewModel).FullName!);
            });
        }
        else
        {
            /*App.MainWindow.DispatcherQueue.TryEnqueue(() =>
            {
                App.MainWindow.ShowMessageDialogAsync("Notification notification itself clicked", "Notification Invoked");
                App.MainWindow.BringToFront();
            });*/
        }
    }

    public bool ShowReminder(string title, string message, string time)
    {
        string snooze = "AppNotification_Snooze".GetLocalized();
        string dismiss = "AppNotification_Dismiss".GetLocalized();
        string payload = new(@$"
            <toast scenario='reminder'>
              <visual>
                <binding template='ToastGeneric'>
                  <text>{title}</text>
                  <text>{message}</text>
                  <text>{time}</text>
                </binding>
              </visual>
              <actions>
                <input id='snoozeTime' type='selection' defaultInput='15'>
                  <selection id='1' content='1 minute'/>
                  <selection id='15' content='15 minutes'/>
                  <selection id='60' content='1 hour'/>
                  <selection id='240' content='4 hours'/>
                  <selection id='1440' content='1 day'/>
                </input>
                <action arguments='snooze' hint-inputId='snoozeTime' content='{snooze}'/>
                <action arguments='dismiss' content='{dismiss}'/>
              </actions>
            </toast>");
        AppNotification appNotification = new(string.Format(payload, AppContext.BaseDirectory))
        {
            Priority = AppNotificationPriority.High
        };
        AppNotificationManager.Default.Show(appNotification);
        return appNotification.Id != 0;
    }
    public bool ShowDeletedMessage(string title, string message)
    {
        string buttonText = "AppNotification_SeeDeletedReminders".GetLocalized();
        string payload = new(@$"
            <toast>
              <visual>
                <binding template='ToastGeneric'>
                  <text>{title}</text>
                  <text>{message}</text>
                </binding>
              </visual>
              <actions>
                <action arguments='trash' content='{buttonText}'/>
              </actions>
            </toast>");
        AppNotification appNotification = new(string.Format(payload, AppContext.BaseDirectory))
        {
            ExpiresOnReboot = true,
            Expiration = DateTime.Now.AddMinutes(10),
            Priority = AppNotificationPriority.High
        };
        AppNotificationManager.Default.Show(appNotification);
        return appNotification.Id != 0;
    }
    public bool ShowInfoMessage(string title, string message)
    {
        string payload = new(@$"
            <toast>
              <visual>
                <binding template='ToastGeneric'>
                  <text>{title}</text>
                  <text>{message}</text>
                </binding>
              </visual>
            </toast>");
        AppNotification appNotification = new(string.Format(payload, AppContext.BaseDirectory))
        {
            ExpiresOnReboot = true,
            Expiration = DateTime.Now.AddHours(1),
            Priority = AppNotificationPriority.High
        };
        AppNotificationManager.Default.Show(appNotification);
        return appNotification.Id != 0;
    }
    public NameValueCollection ParseArguments(string arguments)
    {
        return HttpUtility.ParseQueryString(arguments);
    }

    public void Unregister()
    {
        AppNotificationManager.Default.Unregister();
    }
}
