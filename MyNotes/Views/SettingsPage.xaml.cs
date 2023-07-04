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
        if (RuntimeHelper.IsMSIX)
        {
            GetTask();
        }
        if (key.GetValue("MyNotes")!=null)
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
        var backdrop = await localSettingsService.ReadSettingAsync<MicaKind>(BackDropKey);
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
        else if (backdrop.ToString()==null)
        {
            _ = localSettingsService.SaveSettingAsync(BackDropKey, MicaKind.Base);
            Settings_BackDrop_Base.IsChecked= true;
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
            if (Environment.ProcessPath!=null)
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
        MicaBackdrop micaBackdrop = new();
        RadioButton? rb = sender as RadioButton;
        if (rb.Name == "Settings_BackDrop_Base")
        {
            _ = localSettingsService.SaveSettingAsync(BackDropKey, MicaKind.Base);
            micaBackdrop.Kind = MicaKind.Base;
        }
        else if (rb.Name == "Settings_BackDrop_BaseAlt")
        {
            _ = localSettingsService.SaveSettingAsync(BackDropKey, MicaKind.BaseAlt);
            micaBackdrop.Kind = MicaKind.BaseAlt;
        }
        MicaKind knd = (MicaKind)App.MainWindow.SystemBackdrop.GetValue(MicaBackdrop.KindProperty);
        if (knd != micaBackdrop.Kind)
        {
            App.MainWindow.SystemBackdrop = micaBackdrop;
        }
    }
}
