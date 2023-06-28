using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MyNotes.ViewModels;
using System.Diagnostics;
using Windows.ApplicationModel;
using Windows.UI.Popups;
using Microsoft.Win32;
using MyNotes.Helpers;

namespace MyNotes.Views;

// TODO: Set the URL for your privacy policy by updating SettingsPage_PrivacyTermsLink.NavigateUri in Resources.resw.
public sealed partial class SettingsPage : Page
{
    readonly RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true)!;
    StartupTask? startupTask;
    public SettingsViewModel ViewModel
    {
        get;
    }
    public SettingsPage()
    {
        ViewModel = App.GetService<SettingsViewModel>();
        InitializeComponent();
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
                MessageDialog dialog = new(
                     "You have disabled this app's ability to run " +
                     "as soon as you sign in, but if you change your mind, " +
                     "you can enable this in the Startup tab in Task Manager.",
                     "My Notes");
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
                    MessageDialog dialog = new(
                        "You have disabled this app's ability to run " +
                        "as soon as you sign in, but if you change your mind, " +
                        "you can enable this in the Startup tab in Task Manager.",
                        "My Notes");
                    await dialog.ShowAsync();
                    break;
                case StartupTaskState.DisabledByPolicy:
                    Debug.WriteLine("Startup disabled by group policy, or not supported on this device");
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
                    Debug.WriteLine("Startup enabled by group policy");
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
}
