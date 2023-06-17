using System.Collections.Specialized;
using System.Web;
using Microsoft.Windows.AppNotifications;
using MyNotes.Contracts.Services;

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
        // TODO: Handle notification invocations when your app is already running.

        //// // Navigate to a specific page based on the notification arguments.
        //// if (ParseArguments(args.Argument)["action"] == "Settings")
        //// {
        ////    App.MainWindow.DispatcherQueue.TryEnqueue(() =>
        ////    {
        ////        _navigationService.NavigateTo(typeof(SettingsViewModel).FullName!);
        ////    });
        //// }

        IDictionary<string,string> userInput = args.UserInput;
        string ar = args.Argument; 
        if (ar == "snooze")
        {
            App.MainWindow.DispatcherQueue.TryEnqueue(() =>
            {
                App.MainWindow.ShowMessageDialogAsync("Snooze time :" + userInput["snoozeTime"], "Notification Invoked");
                App.MainWindow.BringToFront();
            });
        }
        else if (ar == "dismiss")
        {
            App.MainWindow.DispatcherQueue.TryEnqueue(() =>
            {
                App.MainWindow.ShowMessageDialogAsync("Notification dismissed", "Notification Invoked");
                App.MainWindow.BringToFront();
            });
        }
        else
        {
            App.MainWindow.DispatcherQueue.TryEnqueue(() =>
            {
                App.MainWindow.ShowMessageDialogAsync("Notification notification itself clicked", "Notification Invoked");
                App.MainWindow.BringToFront();
            });
        }
    }

    public bool Show(string title,string message, string time)
    {
        string payload = new(@$"
            <toast scenario='reminder'>
              <visual>
                <binding template='ToastGeneric'>
                  <text>{title}</text>
                  <text>{message}</text>
                  <text>{time}</text>
                   <image src='ms-appx:///Assets/WindowIcon.ico' placement='appLogoOverride' hint-crop='circle'/>
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
                <action arguments='snooze' hint-inputId='snoozeTime' content='Snooze'/>
                <action arguments='dismiss' content='Dismiss'/>
              </actions>
            </toast>");
        AppNotification appNotification = new(string.Format(payload, AppContext.BaseDirectory));
        appNotification.Expiration = DateTime.Now.AddDays(1);
        appNotification.Priority = AppNotificationPriority.High;
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
