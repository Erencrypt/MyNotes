using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.Win32;
using MyNotes.Contracts.Services;
using MyNotes.Helpers;
using MyNotes.ViewModels;
using System.Diagnostics;
using Windows.ApplicationModel;
using Windows.UI.Popups;

namespace MyNotes.Views;

public sealed partial class SettingsPage : Page
{
    private readonly RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true)!;
    private readonly ILocalSettingsService localSettingsService;
    StartupTask? startupTask;
    private readonly string BackDropKey = "BackDrop";
    private readonly string AcrylicKey = "IsAcrylic";
    private readonly string SaveWhenExitKey = "SaveWhenExit";
    private bool isAcrylic;
    public SettingsViewModel ViewModel
    {
        get;
    }
    public SettingsPage()
    {
        ViewModel = App.GetService<SettingsViewModel>();
        InitializeComponent();

        ThemeCard.Header = "Settings_Theme".GetLocalized();
        BackdropCard.Header = "Settings_BackDrop".GetLocalized();
        SaveCard.Header = "Settings_Save".GetLocalized();
        SaveCard.Description = "Settings_SaveDescription".GetLocalized();
        StartupCard.Header = "Settings_Startup".GetLocalized();
        StartupCard.Description = "Settings_StartupDescription".GetLocalized();
        AboutSection.Header = "AppDescription".GetLocalized();
        AboutSection.Description = "Settings_About".GetLocalized();
        localSettingsService = App.GetService<ILocalSettingsService>();
        ThemeState();
        BackDropState();
        SaveWhenExitState();
        if (RuntimeHelper.IsMSIX)
        {
            GetTask();
        }
        if (key.GetValue("MyNotes") != null)
        {
            StartupTogle.IsOn = true;
        }
        else
        {
            StartupTogle.IsOn = false;
        }
    }
    private async void BackDropState()
    {
        isAcrylic = await localSettingsService.ReadSettingAsync<bool>(AcrylicKey);
        var backdrop = await localSettingsService.ReadSettingAsync<MicaKind>(BackDropKey);
        if (isAcrylic)
        {
            BackdropComboBox.SelectedItem = Settings_BackDrop_Acrylic;
        }
        else
        {
            isAcrylic = false;
            if (backdrop.ToString() != null)
            {
                if (backdrop == MicaKind.Base)
                {
                    BackdropComboBox.SelectedItem = Settings_BackDrop_Base;
                }
                else if (backdrop == MicaKind.BaseAlt)
                {
                    BackdropComboBox.SelectedItem = Settings_BackDrop_BaseAlt;
                }
            }
            else if (backdrop.ToString() == null)
            {
                await localSettingsService.SaveSettingAsync(BackDropKey, MicaKind.Base);
                BackdropComboBox.SelectedItem = Settings_BackDrop_Base;
            }
        }
    }

    private void ThemeState()
    {
        switch (ViewModel.ElementTheme)
        {
            case ElementTheme.Default:
                ThemeComboBox.SelectedIndex = 2;
                break;
            case ElementTheme.Light:
                ThemeComboBox.SelectedIndex = 0;
                break;
            case ElementTheme.Dark:
                ThemeComboBox.SelectedIndex = 1;
                break;
        }
    }
    private async void SaveWhenExitState()
    {
        var save = await localSettingsService.ReadSettingAsync<bool>(SaveWhenExitKey);
        if (save.ToString() != null)
        {
            if (save)
            {
                SaveTogle.IsOn = true;
            }
            else if (!save)
            {
                SaveTogle.IsOn = false;
            }
        }
        else if (save.ToString() == null)
        {
            await localSettingsService.SaveSettingAsync(SaveWhenExitKey, true);
            SaveTogle.IsOn = true;
        }
    }
    private async void GetTask()
    {
        startupTask = await StartupTask.GetAsync("8163264128256");
        switch (startupTask.State)
        {
            case StartupTaskState.Enabled:
                StartupTogle.IsOn = true;
                break;
            case StartupTaskState.Disabled:
                StartupTogle.IsOn = false;
                break;
            case StartupTaskState.DisabledByUser:
                // Task is disabled and user must enable it manually.
                MessageDialog dialog = new("Settings_DisabledByUser".GetLocalized());
                await dialog.ShowAsync();
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
    private async void EnableStartup()
    {
        if (RuntimeHelper.IsMSIX)
        {
            switch (startupTask.State)
            {
                case StartupTaskState.Disabled:
                    StartupTaskState newState = await startupTask.RequestEnableAsync();
                    Debug.WriteLine("Request to enable startup, result = {0}", newState);
                    break;
                case StartupTaskState.DisabledByUser:
                    // Task is disabled and user must enable it manually.
                    MessageDialog dialog = new("Settings_DisabledByUser".GetLocalized());
                    await dialog.ShowAsync();
                    break;
                case StartupTaskState.DisabledByPolicy:
                    MessageDialog dialog2 = new("Settings_DisabledByPolicy".GetLocalized());
                    await dialog2.ShowAsync();
                    Debug.WriteLine("Settings_DisabledByPolicy".GetLocalized());
                    break;
            }
        }
        else
        {
            if (Environment.ProcessPath != null)
            {
                key.SetValue("MyNotes", Environment.ProcessPath!);
            }
        }
    }
    private void DisableStartup()
    {
        if (RuntimeHelper.IsMSIX)
        {
            switch (startupTask.State)
            {
                case StartupTaskState.Enabled:
                    startupTask.Disable();
                    Debug.WriteLine("Request to disable startup, result = {0}", startupTask.State);
                    break;
                case StartupTaskState.EnabledByPolicy:
                    Debug.WriteLine("Settings_EnabledByPolicy".GetLocalized());
                    break;
            }
        }
        else
        {
            key.DeleteValue("MyNotes", false);
        }
    }

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

    private void SaveTogle_Toggled(object sender, RoutedEventArgs e)
    {
        _= localSettingsService.SaveSettingAsync(SaveWhenExitKey, SaveTogle.IsOn);
    }



    private async void BackdropComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        MicaKind knd = (MicaKind)App.MainWindow.SystemBackdrop.GetValue(MicaBackdrop.KindProperty);
        MicaBackdrop micaBackdrop = new();
        ComboBoxItem? cbItem = BackdropComboBox.SelectedItem as ComboBoxItem;

        if (cbItem.Name == "Settings_BackDrop_Base")
        {
            await localSettingsService.SaveSettingAsync(BackDropKey, MicaKind.Base);
            micaBackdrop.Kind = MicaKind.Base;
            if (isAcrylic)
            {
                App.MainWindow.SystemBackdrop = micaBackdrop;
                isAcrylic = false;
            }
            else if (knd != micaBackdrop.Kind)
            {
                App.MainWindow.SystemBackdrop = micaBackdrop;
            }
        }
        else if (cbItem.Name == "Settings_BackDrop_BaseAlt")
        {
            await localSettingsService.SaveSettingAsync(BackDropKey, MicaKind.BaseAlt);
            micaBackdrop.Kind = MicaKind.BaseAlt;
            if (isAcrylic)
            {
                App.MainWindow.SystemBackdrop = micaBackdrop;
                isAcrylic = false;
            }
            else if (knd != micaBackdrop.Kind)
            {
                App.MainWindow.SystemBackdrop = micaBackdrop;
            }
        }
        else if (cbItem.Name == "Settings_BackDrop_Acrylic")
        {
            if (!isAcrylic)
            {
                await localSettingsService.SaveSettingAsync(AcrylicKey, true);
                DesktopAcrylicBackdrop desktopAcrylicBackdrop = new();
                App.MainWindow.SystemBackdrop = desktopAcrylicBackdrop;
                isAcrylic = true;
            }
        }
        await localSettingsService.SaveSettingAsync(AcrylicKey, isAcrylic);
    }

    private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        switch (ThemeComboBox.SelectedIndex)
        {
            case 0:
                ViewModel.SwitchThemeCommand.Execute(ElementTheme.Light);
                break;
            case 1:
                ViewModel.SwitchThemeCommand.Execute(ElementTheme.Dark);
                break;
            case 2:
                ViewModel.SwitchThemeCommand.Execute(ElementTheme.Default);
                break;
            default:
                ViewModel.SwitchThemeCommand.Execute(ElementTheme.Default);
                break;
        }
    }

}
