using Microsoft.Windows.AppNotifications;
using MyNotes.Data.Services.Interfaces;
using MyNotes.Helpers;
using MyNotes.Services.Interfaces;
using MyNotes.Views;
using System.Security;
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
        private string ShowUpdateAvailableMessageText = string.Empty;

        private AppNotificationPriority NotificationPriority = AppNotificationPriority.High;
        private bool _isInitialized;

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
            if (_isInitialized)
            {
                return;
            }

            SnoozeText = _localizer.GetLocalizedString("AppNotification_Snooze");
            DismissText = _localizer.GetLocalizedString("AppNotification_Dismiss");
            ShowDeletedButtonText = _localizer.GetLocalizedString("AppNotification_SeeDeletedReminders");
            ShowUpdateAvailableMessageText = _localizer.GetLocalizedString("AppNotification_UpdateAvailable");
            NotificationPriority = await _settingService.GetSettingAsync<AppNotificationPriority>(
                DataKeys.ReminderNotificationPriority,
                AppNotificationPriority.High);

            AppNotificationManager.Default.NotificationInvoked += OnNotificationInvoked;
            try
            {
                AppNotificationManager.Default.Register();
                _isInitialized = true;
                LogWriter.Log("App notification manager registered.", LogWriter.LogLevel.Info);
            }
            catch
            {
                AppNotificationManager.Default.NotificationInvoked -= OnNotificationInvoked;
                throw;
            }
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
            else if (ar == "update")
            {
                App._mainWindow.DispatcherQueue.TryEnqueue(() =>
                {
                    NavigationHelper.NavigateTo<SettingsPage>();
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
                  <text hint-maxLines='1'>{EscapeXml(title)}</text>
                  <text>{EscapeXml(message)}</text>
                  <text>{EscapeXml(time)}</text>
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
                <action arguments='snooze:{reminderId}' hint-inputId='snoozeTime' content='{EscapeXml(SnoozeText)}'/>
                <action arguments='dismiss:{reminderId}' content='{EscapeXml(DismissText)}'/>
              </actions>
                {alarmtext}
            </toast>");
            AppNotification appNotification = new(payload)
            {
                ExpiresOnReboot = true,
                Priority = NotificationPriority
            };
            AppNotificationManager.Default.Show(appNotification);
            return appNotification.Id != 0;
        }
        public Task<bool> ShowDeletedMessage(string title, string message)
        {
            string payload = new(@$"
            <toast launch='trash'>
              <visual>
                <binding template='ToastGeneric'>
                  <text>{EscapeXml(title)}</text>
                  <text>{EscapeXml(message)}</text>
                </binding>
              </visual>
              <actions>
                <action arguments='trash' content='{EscapeXml(ShowDeletedButtonText)}'/>
              </actions>
            </toast>");
            AppNotification appNotification = new(payload)
            {
                ExpiresOnReboot = true,
                Expiration = DateTime.Now.AddMinutes(10),
                Priority = NotificationPriority
            };
            AppNotificationManager.Default.Show(appNotification);
            return Task.FromResult(appNotification.Id != 0);
        }
        public Task<bool> ShowInfoMessage(string title, string message)
        {
            string payload = new(@$"
            <toast>
              <visual>
                <binding template='ToastGeneric'>
                  <text>{EscapeXml(title)}</text>
                  <text>{EscapeXml(message)}</text>
                </binding>
              </visual>
            </toast>");
            AppNotification appNotification = new(payload)
            {
                ExpiresOnReboot = true,
                Expiration = DateTime.Now.AddMinutes(10),
                Priority = NotificationPriority
            };
            AppNotificationManager.Default.Show(appNotification);
            return Task.FromResult(appNotification.Id != 0);
        }

        public Task<bool> ShowUpdateAvailableMessage(string title, string message)
        {
            string payload = new(@$"
            <toast launch='update'>
              <visual>
                <binding template='ToastGeneric'>
                  <text>{EscapeXml(title)}</text>
                  <text>{EscapeXml(message)}</text>
                </binding>
              </visual>
              <actions>
                <action arguments='update' content='{EscapeXml(ShowUpdateAvailableMessageText)}'/>
              </actions>
            </toast>");

            AppNotification appNotification = new(payload)
            {
                ExpiresOnReboot = true,
                Expiration = DateTime.Now.AddMinutes(10),
                Priority = NotificationPriority
            };

            AppNotificationManager.Default.Show(appNotification);
            return Task.FromResult(appNotification.Id != 0);
        }

        private static string EscapeXml(string value) => SecurityElement.Escape(value) ?? string.Empty;

        public void Unregister()
        {
            if (!_isInitialized)
            {
                return;
            }

            AppNotificationManager.Default.NotificationInvoked -= OnNotificationInvoked;
            AppNotificationManager.Default.Unregister();
            _isInitialized = false;
        }
    }

}
