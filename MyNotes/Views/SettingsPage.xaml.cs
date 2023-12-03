using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MyNotes.ViewModels;
using System.Diagnostics;
using Windows.ApplicationModel;
using Windows.UI.Popups;
using Microsoft.Win32;
using MyNotes.Helpers;
using MyNotes.Contracts.Services;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml.Media;

namespace MyNotes.Views;

public sealed partial class SettingsPage : Page
{
    readonly RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true)!;
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
        localSettingsService = App.GetService<ILocalSettingsService>();
        BackDropState();
        SaveWhenExitState();
        if (RuntimeHelper.IsMSIX)
        {
            GetTask();
        }
        if (key.GetValue("MyNotes") != null)
        {
            StartupCheck.IsChecked = true;
        }
        else
        {
            StartupCheck.IsChecked = false;
        }
    }
    private async void BackDropState()
    {
        isAcrylic = await localSettingsService.ReadSettingAsync<bool>(AcrylicKey);
        var backdrop = await localSettingsService.ReadSettingAsync<MicaKind>(BackDropKey);
        if (isAcrylic)
        {
            Settings_BackDrop_Acrylic.IsChecked = true;
        }
        else
        {
            isAcrylic =false;
            if (backdrop.ToString() != null)
            {
                if (backdrop == MicaKind.Base)
                {
                    Settings_BackDrop_Base.IsChecked = true;
                }
                else if (backdrop == MicaKind.BaseAlt)
                {
                    Settings_BackDrop_BaseAlt.IsChecked = true;
                }
            }
            else if (backdrop.ToString() == null)
            {
                await localSettingsService.SaveSettingAsync(BackDropKey, MicaKind.Base);
                Settings_BackDrop_Base.IsChecked = true;
            }
        }
    }
    private async void SaveWhenExitState()
    {
        var save = await localSettingsService.ReadSettingAsync<bool>(SaveWhenExitKey);
        if (save.ToString() != null)
        {
            if (save)
            {
                SaveCheck.IsChecked = true;
            }
            else if (!save)
            {
                SaveCheck.IsChecked = false;
            }
        }
        else if (save.ToString() == null)
        {
            await localSettingsService.SaveSettingAsync(SaveWhenExitKey, true);
            SaveCheck.IsChecked = true;
        }
    }
    private async void GetTask()
    {
        startupTask = await StartupTask.GetAsync("8163264128256");
        switch (startupTask.State)
        {
            case StartupTaskState.Enabled:
                StartupCheck.IsChecked = true;
                break;
            case StartupTaskState.Disabled:
                StartupCheck.IsChecked = false;
                break;
            case StartupTaskState.DisabledByUser:
                // Task is disabled and user must enable it manually.
                MessageDialog dialog = new("Settings_DisabledByUser".GetLocalized());
                await dialog.ShowAsync();
                StartupCheck.IsChecked = false;
                break;
            case StartupTaskState.DisabledByPolicy:
                StartupCheck.IsChecked = false;
                StartupCheck.IsEnabled = false;
                break;
            case StartupTaskState.EnabledByPolicy:
                StartupCheck.IsChecked = true;
                StartupCheck.IsEnabled = true;
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
    private void StartupCheck_Checked(object sender, RoutedEventArgs e)
    {
        EnableStartup();
    }

    private void StartupCheck_Unchecked(object sender, RoutedEventArgs e)
    {
        DisableStartup();
    }

    private void HandleCheck(object sender, RoutedEventArgs e)
    {
        MicaKind knd = (MicaKind)App.MainWindow.SystemBackdrop.GetValue(MicaBackdrop.KindProperty);
        MicaBackdrop micaBackdrop = new();
        RadioButton? rb = sender as RadioButton;
        
        if (rb.Name == "Settings_BackDrop_Base")
        {
            localSettingsService.SaveSettingAsync(BackDropKey, MicaKind.Base);
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
        else if (rb.Name == "Settings_BackDrop_BaseAlt")
        {
            localSettingsService.SaveSettingAsync(BackDropKey, MicaKind.BaseAlt);
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
        else if (rb.Name == "Settings_BackDrop_Acrylic")
        {
            if (!isAcrylic)
            {
                localSettingsService.SaveSettingAsync(AcrylicKey, true);
                DesktopAcrylicBackdrop desktopAcrylicBackdrop = new();
                App.MainWindow.SystemBackdrop = desktopAcrylicBackdrop;
                isAcrylic = true;
            }
        }
        localSettingsService.SaveSettingAsync(AcrylicKey, isAcrylic);
    }

    private void SaveCheck_Checked(object sender, RoutedEventArgs e)
    {
        localSettingsService.SaveSettingAsync(SaveWhenExitKey, true);
        SaveCheck.IsChecked = true;
    }

    private void SaveCheck_Unchecked(object sender, RoutedEventArgs e)
    {
        localSettingsService.SaveSettingAsync(SaveWhenExitKey, false);
        SaveCheck.IsChecked = false;
    }
}
