using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using MyNotes.Views.Dialogs;
using System.Diagnostics;
using Windows.UI;
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
        private bool _useSystemAccentColor = true;
        private string? _customAccentColor;

        public bool UseSystemAccentColor
        {
            get => _useSystemAccentColor;
            set
            {
                if (SetProperty(ref _useSystemAccentColor, value))
                {
                    OnPropertyChanged(nameof(EffectiveAccentColor));
                }
            }
        }

        /// <summary>Custom accent color as "#AARRGGBB", or null if the user never picked one.</summary>
        public string? CustomAccentColor
        {
            get => _customAccentColor;
            set
            {
                if (SetProperty(ref _customAccentColor, value))
                {
                    OnPropertyChanged(nameof(EffectiveAccentColor));
                }
            }
        }

        /// <summary>The accent color currently in effect (system or custom).</summary>
        public Color EffectiveAccentColor =>
            !UseSystemAccentColor && !string.IsNullOrEmpty(CustomAccentColor)
                ? HexConverter.HexToColor(CustomAccentColor)
                : AccentColorHelper.SystemAccentColor;

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

        public async Task SetUseSystemAccentColorAsync(bool value)
        {
            UseSystemAccentColor = value;
            // First switch to custom: seed it with the current system accent so nothing jumps.
            CustomAccentColor ??= HexConverter.ColorToHex(AccentColorHelper.SystemAccentColor);
            await SaveAndApplyAccentAsync();
        }

        public async Task SetCustomAccentColorAsync(Color value)
        {
            CustomAccentColor = HexConverter.ColorToHex(value);
            UseSystemAccentColor = false;
            await SaveAndApplyAccentAsync();
        }

        private async Task SaveAndApplyAccentAsync()
        {
            await SaveSettingAsync(DataKeys.UseSystemAccentColor, UseSystemAccentColor);
            if (CustomAccentColor is not null)
            {
                await SaveSettingAsync(DataKeys.CustomAccentColor, CustomAccentColor);
            }

            AccentColorHelper.Apply(
                UseSystemAccentColor ? null : EffectiveAccentColor,
                App._mainWindow?.Content as FrameworkElement);
        }

        #region startup

        private StartupTask? _startupTask;

        public async Task LoadStartupStateAsync()
        {
            if (!RuntimeHelper.IsMSIX)
            {
                StartupEnabled = AppUtilities.StartupEntryExists(AppConfig.StartupRegistryValueName);
                return;
            }

            _startupTask = await StartupTask.GetAsync(AppConfig.StartupTaskId);

            switch (_startupTask.State)
            {
                case StartupTaskState.Enabled:
                    StartupEnabled = true;
                    break;
                case StartupTaskState.Disabled:
                    StartupEnabled = false;
                    break;
                case StartupTaskState.DisabledByUser:
                    await ShowStatusDialogAsync(_localizer.GetLocalizedString("Settings_DisabledByUser"));
                    StartupEnabled = false;
                    break;
                case StartupTaskState.DisabledByPolicy:
                    StartupEnabled = false;
                    StartupToggleEnabled = false;
                    break;
                case StartupTaskState.EnabledByPolicy:
                    StartupEnabled = true;
                    StartupToggleEnabled = true;
                    break;
            }
        }

        public async Task SetStartupEnabledAsync(bool value)
        {
            StartupEnabled = value;

            if (value)
            {
                await EnableStartupAsync();
            }
            else
            {
                DisableStartup();
            }
        }

        private async Task EnableStartupAsync()
        {
            if (RuntimeHelper.IsMSIX)
            {
                if (_startupTask is null)
                {
                    return;
                }

                var newState = await _startupTask.RequestEnableAsync();
                Debug.WriteLine("Request to enable startup, result = {0}", newState);
            }
            else if (AppUtilities.GetProcessPath() is string processPath)
            {
                AppUtilities.SetStartupEntry(AppConfig.StartupRegistryValueName, processPath);
            }
        }

        private void DisableStartup()
        {
            if (RuntimeHelper.IsMSIX)
            {
                if (_startupTask is null)
                {
                    return;
                }

                switch (_startupTask.State)
                {
                    case StartupTaskState.Enabled:
                        _startupTask.Disable();
                        Debug.WriteLine("Request to disable startup, result = {0}", _startupTask.State);
                        break;
                    case StartupTaskState.EnabledByPolicy:
                        Debug.WriteLine(_localizer.GetLocalizedString("Settings_EnabledByPolicy"));
                        break;
                }
            }
            else
            {
                AppUtilities.RemoveStartupEntry(AppConfig.StartupRegistryValueName);
            }
        }

        private static async Task ShowStatusDialogAsync(string message)
        {
            var dialog = new StatusDialog(message)
            {
                XamlRoot = await App.GetMainWindowXamlRootAsync()
            };

            await dialog.ShowAsync();
        }

        #endregion

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
            UseSystemAccentColor = await _settingService.GetSettingAsync(DataKeys.UseSystemAccentColor, true);
            CustomAccentColor = await _settingService.GetSettingAsync<string>(DataKeys.CustomAccentColor, null);

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
