using CommunityToolkit.Mvvm.Input;
using Microsoft.Windows.AppNotifications;
using MyNotes.Data.Models.Enums;
using MyNotes.Data.Services.Interfaces;
using MyNotes.Helpers;
using Windows.ApplicationModel;
using Microsoft.Windows.Globalization;
using WinUI3Localizer;

namespace MyNotes.ViewModels
{
    /// <summary>
    /// ViewModel for the settings page
    /// </summary>
    public partial class SettingsViewModel : BaseViewModel
    {
        private readonly ISettingService _settingService;
        private readonly ILocalizer _localizer;

        private string _versionDescription = string.Empty;
        private bool _saveWhenExit = true;
        private bool _alarmSoundLooping = true;
        private AppNotificationPriority _reminderNotificationPriority = AppNotificationPriority.High;
        private int _trashRetentionDays = 30;
        private bool _startupEnabled;
        private bool _startupToggleEnabled = true;
        private TimeFormat _timeFormat;
        private string _language = "en-US";

        public string VersionDescription
        {
            get => _versionDescription;
            set => SetProperty(ref _versionDescription, value);
        }

        public TimeFormat TimeFormat
        {
            get => _timeFormat;
            set => SetProperty(ref _timeFormat, value);
        }
        public string Language
        {
            get => _language;
            set => SetProperty(ref _language, value);
        }

        public bool SaveWhenExit
        {
            get => _saveWhenExit;
            set => SetProperty(ref _saveWhenExit, value);
        }

        public bool AlarmSoundLooping
        {
            get => _alarmSoundLooping;
            set => SetProperty(ref _alarmSoundLooping, value);
        }

        public AppNotificationPriority ReminderNotificationPriority
        {
            get => _reminderNotificationPriority;
            set => SetProperty(ref _reminderNotificationPriority, value);
        }

        public int TrashRetentionDays
        {
            get => _trashRetentionDays;
            set => SetProperty(ref _trashRetentionDays, value);
        }

        public bool StartupEnabled
        {
            get => _startupEnabled;
            set => SetProperty(ref _startupEnabled, value);
        }

        public bool StartupToggleEnabled
        {
            get => _startupToggleEnabled;
            set => SetProperty(ref _startupToggleEnabled, value);
        }

        public SettingsViewModel(ISettingService settingService)
        {
            _settingService = settingService;
            _localizer = Localizer.Get();
            _versionDescription = GetVersionDescription();
        }

        [RelayCommand]
        private async Task ResetSettingsAsync()
        {
            SaveWhenExit = true;
            AlarmSoundLooping = true;
            ReminderNotificationPriority = AppNotificationPriority.High;
            TrashRetentionDays = 30;
            TimeFormat = TimeFormat.TwelveHour;
            Language = "en-US";
            await SaveSettingAsync(DataKeys.SaveWhenExit, SaveWhenExit);
            await SaveSettingAsync(DataKeys.AlarmSoundLooping, AlarmSoundLooping);
            await SaveSettingAsync(DataKeys.ReminderNotificationPriority, ReminderNotificationPriority);
            await SaveSettingAsync(DataKeys.TrashRetentionDays, TrashRetentionDays);
            await SaveSettingAsync(DataKeys.HourFormat, TimeFormat);
            await SaveSettingAsync(DataKeys.Language, Language);
        }

        public async Task SetSaveWhenExitAsync(bool value)
        {
            SaveWhenExit = value;
            await SaveSettingAsync(DataKeys.SaveWhenExit, value);
        }

        public async Task SetAlarmSoundLoopingAsync(bool value)
        {
            AlarmSoundLooping = value;
            await SaveSettingAsync(DataKeys.AlarmSoundLooping, value);
        }

        public async Task SetReminderNotificationPriorityAsync(AppNotificationPriority value)
        {
            ReminderNotificationPriority = value;
            await SaveSettingAsync(DataKeys.ReminderNotificationPriority, value);
        }

        public async Task SetTrashRetentionDaysAsync(int value)
        {
            TrashRetentionDays = value;
            await SaveSettingAsync(DataKeys.TrashRetentionDays, value);
        }

        public async Task SetTimeFormatAsync(TimeFormat value)
        {
            TimeFormat = value;
            await SaveSettingAsync(DataKeys.HourFormat, value);
        }
        public async Task SetLanguageAsync(string value)
        {
            Language = value;
            await SaveSettingAsync(DataKeys.Language, value);
            ApplicationLanguages.PrimaryLanguageOverride = value;
            await _localizer.SetLanguage(value);
        }

        private async Task SaveSettingAsync<T>(string key, T value)
        {
            await _settingService.SetSettingAsync(key, value);
        }

        private async Task LoadSettingsAsync()
        {
            SaveWhenExit = await _settingService.GetSettingAsync<bool>(DataKeys.SaveWhenExit, true);
            AlarmSoundLooping = await _settingService.GetSettingAsync<bool>(DataKeys.AlarmSoundLooping, true);
            ReminderNotificationPriority = await _settingService.GetSettingAsync<AppNotificationPriority>(DataKeys.ReminderNotificationPriority, AppNotificationPriority.High);
            TrashRetentionDays = await _settingService.GetSettingAsync<int>(DataKeys.TrashRetentionDays, 30);
            TimeFormat = await _settingService.GetSettingAsync<TimeFormat>(DataKeys.HourFormat, TimeFormat.TwelveHour);
            Language = await _settingService.GetSettingAsync<string>(DataKeys.Language, "en-US")?? "en-US";

            await _localizer.SetLanguage(Language);
        }

        public async override Task Initialize()
        {
            await LoadSettingsAsync();
        }

        private static string GetVersionDescription()
        {
            Version version;

            if (RuntimeHelper.IsMSIX)
            {
                var packageVersion = Package.Current.Id.Version;
                version = new(packageVersion.Major, packageVersion.Minor, packageVersion.Build, packageVersion.Revision);
            }
            else
            {
                version = new Version(AppConfig.AppVersion);
            }

            return $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
        }
    }
}
