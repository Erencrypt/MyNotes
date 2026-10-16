using Microsoft.Windows.AppNotifications;
using MyNotes.Data.Services.Interfaces;
using MyNotes.Helpers;
using MyNotes.Services.Interfaces;
using MyNotes.Views;
using WinUI3Localizer;

namespace MyNotes.Services
{
    public class AppNotificationService : IAppNotificationService
    {
        private readonly ILocalizer _localizer = Localizer.Get();
        private readonly ISettingService _settingService;
        private string SnoozeText =string.Empty;
        private string DismissText = string.Empty;
        private string ShowDeletedButtonText = string.Empty;
        public AppNotificationService(ISettingService settingService)
        {
            _settingService = settingService;
        }

        ~AppNotificationService()
        {
            Unregister();
        }

        public async Task InitializeAsync()
        {
            AppNotificationManager.Default.NotificationInvoked += OnNotificationInvoked;

            AppNotificationManager.Default.Register();
            SnoozeText = _localizer.GetLocalizedString("AppNotification_Snooze");
            DismissText = _localizer.GetLocalizedString("AppNotification_Dismiss");
            ShowDeletedButtonText = _localizer.GetLocalizedString("AppNotification_SeeDeletedReminders");
        }

        public void OnNotificationInvoked(AppNotificationManager sender, AppNotificationActivatedEventArgs args)
        {
            IDictionary<string, string> userInput = args.UserInput;
            string ar = args.Argument;
            // argument format: action:reminderId  e.g. "snooze:00000000-0000-0000-0000-000000000000"
            if (string.IsNullOrEmpty(ar)) ar = string.Empty;
            var parts = ar.Split(':', 2);
            var action = parts.Length > 0 ? parts[0] : string.Empty;
            var id = parts.Length > 1 ? parts[1] : string.Empty;

            if (App._mainWindow==null) {return;}

            if (action == "snooze" && Guid.TryParse(id, out var snoozeId))
            {
                App._mainWindow.DispatcherQueue.TryEnqueue(async () =>
                {
                    int input = Convert.ToInt32(userInput["snoozeTime"]);
                    await App.GetService<IReminderScheduler>().SnoozeAsync(snoozeId, TimeSpan.FromMinutes(input));
                });
            }
            else if (action == "dismiss" && Guid.TryParse(id, out var dismissId))
            {
                App._mainWindow.DispatcherQueue.TryEnqueue(async () =>
                {
                    await App.GetService<IReminderScheduler>().DismissAsync(dismissId);
                });
            }
            else if (ar == "trash")
            {
                App._mainWindow.DispatcherQueue.TryEnqueue(() =>
                {
                    TrashPage.NtfInvoke = true;
                    NavigationHelper.NavigateTo<TrashPage>();
                });
            }
            else
            {
                App._mainWindow.DispatcherQueue.TryEnqueue(() =>
                {
                    //This code will work if there is no arguments passed.(notification itself clicked)
                    App._mainWindow.Activate();
                });
            }
        }

        public async Task<bool> ShowReminderAsync(Guid reminderId, string title, string message, string time, bool isAlarm)
        {
            string alarmtext = string.Empty;
            if (isAlarm)
            {
                var loopAlarmSound = await _settingService.GetSettingAsync<bool>(DataKeys.AlarmSoundLooping, true);
                alarmtext = $"<audio src='ms-winsoundevent:Notification.Looping.Alarm' loop='{loopAlarmSound.ToString().ToLowerInvariant()}'/>";
            }
            string payload = new(@$"
            <toast scenario='reminder'>
              <visual>
                <binding template='ToastGeneric'>
                  <text hint-maxLines='1'>{title}</text>
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
                <action arguments='snooze:{reminderId}' hint-inputId='snoozeTime' content='{SnoozeText}'/>
                <action arguments='dismiss:{reminderId}' content='{DismissText}'/>
              </actions>
                {alarmtext}
            </toast>");
            AppNotification appNotification = new(string.Format(payload, AppContext.BaseDirectory))
            {
                ExpiresOnReboot = true,
                Priority = await _settingService.GetSettingAsync<AppNotificationPriority>(
                    DataKeys.ReminderNotificationPriority,
                    AppNotificationPriority.High)
            };
            AppNotificationManager.Default.Show(appNotification);
            return appNotification.Id != 0;
        }
        public bool ShowDeletedMessage(string title, string message)
        {
            string payload = new(@$"
            <toast launch='trash'>
              <visual>
                <binding template='ToastGeneric'>
                  <text>{title}</text>
                  <text>{message}</text>
                </binding>
              </visual>
              <actions>
                <action arguments='trash' content='{ShowDeletedButtonText}'/>
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
                Expiration = DateTime.Now.AddMinutes(10),
                Priority = AppNotificationPriority.High
            };
            AppNotificationManager.Default.Show(appNotification);
            return appNotification.Id != 0;
        }
        public void Unregister()
        {
            AppNotificationManager.Default.Unregister();
        }
    }

}
