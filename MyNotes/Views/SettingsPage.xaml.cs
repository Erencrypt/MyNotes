using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.AppNotifications;
using MyNotes.Data.Models.Enums;
using MyNotes.Data.Services.Interfaces;
using MyNotes.Helpers;
using MyNotes.ViewModels;
using System.Diagnostics;
using Windows.ApplicationModel;
using WinUI3Localizer;

namespace MyNotes.Views;
//TODO: add an option for using system theme color or custom one with color picker
public sealed partial class SettingsPage : Page
{
    private readonly ILocalizer _localizer = Localizer.Get();
    private StartupTask? _startupTask;

    public SettingsViewModel ViewModel { get; }

    public SettingsPage()
    {
        InitializeComponent();

        ViewModel = App.GetService<SettingsViewModel>();
        DataContext = ViewModel;

        Loaded += SettingsPage_Loaded;
    }

    private async void SettingsPage_Loaded(object sender, RoutedEventArgs e)
    {
        Loaded -= SettingsPage_Loaded;
        ViewModel.IsLoading = true;

        await ViewModel.Initialize();
        SaveTogle.IsOn = ViewModel.SaveWhenExit;
        AlarmSoundLoopingToggle.IsOn = ViewModel.AlarmSoundLooping;
        ReminderNotificationPriorityCb.SelectedIndex = ViewModel.ReminderNotificationPriority == AppNotificationPriority.High ? 1 : 0;
        TrashRetentionCb.SelectedIndex = ViewModel.TrashRetentionDays switch
        {
            5 => 0,
            15 => 1,
            30 => 2,
            45 => 3,
            60 => 4,
            90 => 5,
            0 => 6,
            _ => 2
        };

        if (Helpers.RuntimeHelper.IsMSIX)
        {
            await GetTaskAsync();
        }
        else
        {
            StartupTogle.IsOn = AppUtilities.StartupEntryExists(AppConfig.StartupRegistryValueName);
        }

        AppDescription.Text = _localizer.GetLocalizedString("AppDescription");
        TimeFormatCb.SelectedIndex = (int)ViewModel.TimeFormat;
        LanguageCardCb.SelectedIndex = ViewModel.Language switch
        {
            "en-US" => 0,
            "tr-TR" => 1,
            //"de-DE" => 2,
            //"fr-FR" => 3,
            //"it-IT" => 4,
            //"nl-NL" => 5,
            //"uk-UA" => 6,
            _ => 0
        };

        var currentTheme = await App.Current.SettingService.GetSettingAsync(DataKeys.Theme, ElementTheme.Default);
        ThemeCb.SelectedIndex = currentTheme switch
        {
            ElementTheme.Light => 0,
            ElementTheme.Dark => 1,
            _ => 2
        };

        var currentBackdrop = await App.Current.SettingService.GetSettingAsync(DataKeys.Backdrop, DevWinUI.BackdropType.Mica);
        BackdropCb.SelectedIndex = currentBackdrop switch
        {
            DevWinUI.BackdropType.Mica => 0,
            DevWinUI.BackdropType.MicaAlt => 1,
            DevWinUI.BackdropType.Acrylic => 2,
            DevWinUI.BackdropType.AcrylicThin => 3,
            _ => 0
        };

        ViewModel.IsLoading = false;
    }

    #region startup

    private async Task GetTaskAsync()
    {
        _startupTask = await StartupTask.GetAsync(AppConfig.StartupTaskId);

        switch (_startupTask.State)
        {
            case StartupTaskState.Enabled:
                StartupTogle.IsOn = true;
                break;
            case StartupTaskState.Disabled:
                StartupTogle.IsOn = false;
                break;
            case StartupTaskState.DisabledByUser:
                await ShowStatusDialogAsync(_localizer.GetLocalizedString("Settings_DisabledByUser"));
                StartupTogle.IsOn = false;
                break;
            case StartupTaskState.DisabledByPolicy:
                StartupTogle.IsOn = false;
                StartupTogle.IsEnabled = false;
                break;
            case StartupTaskState.EnabledByPolicy:
                StartupTogle.IsOn = true;
                StartupTogle.IsEnabled = true;
                break;
        }
    }

    private async Task ShowStatusDialogAsync(string content)
    {
        var dialog = new ContentDialog
        {
            Title = "",
            Content = content,
            CloseButtonText = _localizer.GetLocalizedString("Ok")
        };

        await dialog.ShowAsync();
    }

    private async void EnableStartup()
    {
        if (Helpers.RuntimeHelper.IsMSIX)
        {
            if (_startupTask is null)
            {
                return;
            }

            var newState = await _startupTask.RequestEnableAsync();
            Debug.WriteLine("Request to enable startup, result = {0}", newState);
        }
        else
        {
            if (AppUtilities.GetProcessPath() is string processPath)
            {
                AppUtilities.SetStartupEntry(AppConfig.StartupRegistryValueName, processPath);
            }
        }
    }

    private void DisableStartup()
    {
        if (Helpers.RuntimeHelper.IsMSIX)
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

    #endregion
    private void StartupTogle_Toggled(object sender, RoutedEventArgs e)
    {
        if (StartupTogle.IsOn)
        {
            EnableStartup();
        }
        else
        {
            DisableStartup();
        }
    }

    private async void SaveTogle_Toggled(object sender, RoutedEventArgs e)
    {
        await ViewModel.SetSaveWhenExitAsync(SaveTogle.IsOn);
    }

    private async void AlarmSoundLoopingToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (ViewModel.IsLoading)
        {
            return;
        }

        await ViewModel.SetAlarmSoundLoopingAsync(AlarmSoundLoopingToggle.IsOn);
    }

    private async void ReminderNotificationPriorityCb_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ViewModel.IsLoading || ReminderNotificationPriorityCb.SelectedItem is not ComboBoxItem item)
        {
            return;
        }

        var priority = item.Tag?.ToString() == "High"
            ? AppNotificationPriority.High
            : AppNotificationPriority.Default;
        await ViewModel.SetReminderNotificationPriorityAsync(priority);
    }

    private async void TrashRetentionCb_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ViewModel.IsLoading ||
            TrashRetentionCb.SelectedItem is not ComboBoxItem item ||
            !int.TryParse(item.Tag?.ToString(), out var retentionDays))
        {
            return;
        }

        await ViewModel.SetTrashRetentionDaysAsync(retentionDays);
    }


    private void ExitButton_Click(object sender, RoutedEventArgs e)
    {
        App.HandleClosedEvents = false;
        App.Current.Exit();
    }

    private async void TimeFormatCb_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        TimeFormat timeFormat = (TimeFormat)TimeFormatCb.SelectedIndex;
        await ViewModel.SetTimeFormatAsync(timeFormat);
        ViewModel.TimeFormat = timeFormat;
    }

    private async void LanguageCardCb_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ViewModel.IsLoading)
        {
            return;
        }

        if (LanguageCardCb.SelectedItem is not ComboBoxItem selectedLanguage)
        {
            return;
        }

        var lang = selectedLanguage.Tag?.ToString() ?? "en-US";
        if (lang == ViewModel.Language)
        {
            return;
        }

        await ViewModel.SetLanguageAsync(lang);
        NavigationHelper.NavigateTo<SettingsPage>();
    }

    private async void ThemeCb_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ThemeCb.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            var theme = tag switch
            {
                "Light" => ElementTheme.Light,
                "Dark" => ElementTheme.Dark,
                _ => ElementTheme.Default
            };

            var currentTheme = await App.Current.SettingService.GetSettingAsync(DataKeys.Theme, ElementTheme.Default);
            if (currentTheme != theme)
            {
                await App.Current.ThemeService.SetElementThemeAsync(theme);
            }
        }
    }

    private async void BackdropCb_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (BackdropCb.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            var backdrop = tag switch
            {
                "Mica" => DevWinUI.BackdropType.Mica,
                "MicaAlt" => DevWinUI.BackdropType.MicaAlt,
                "Acrylic" => DevWinUI.BackdropType.Acrylic,
                "AcrylicThin" => DevWinUI.BackdropType.AcrylicThin,
                _ => DevWinUI.BackdropType.Mica
            };

            var currentBackdrop = await App.Current.SettingService.GetSettingAsync(DataKeys.Backdrop, DevWinUI.BackdropType.Mica);
            if (currentBackdrop != backdrop)
            {
                await App.Current.ThemeService.SetBackdropTypeAsync(backdrop);
            }
        }
    }

    private async void CheckUpdatesNowButton_Click(object sender, RoutedEventArgs e)
    {
        CheckUpdatesNowButton.IsEnabled = false;
        try
        {
            await App.Current.CheckForUpdatesAsync(isManual: true);
        }
        finally
        {
            CheckUpdatesNowButton.IsEnabled = true;
        }
    }
}
