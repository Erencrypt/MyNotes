using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using MyNotes.Helpers;
using WinUI3Localizer;

namespace MyNotes.Views
{
    public sealed partial class TrayIconView : UserControl
    {
        private readonly ILocalizer _Localizer = Localizer.Get();
        public TrayIconView()
        {
            InitializeComponent();
            TrayIcon.ToolTipText = _Localizer.GetLocalizedString("AppDisplayName");
        }
        [RelayCommand]
        public static void ShowHideWindow()
        {
            if (App._mainWindow!.AppWindow == null)
            {
                return;
            }
            if (App._mainWindow.AppWindow.IsVisible)
            {
                App._mainWindow.AppWindow.Hide();
            }
            else
            {
                App._mainWindow.AppWindow.Show();
            }
        }

        [RelayCommand]
        public static void NavigateToNotes()
        {
            if (!App._mainWindow!.AppWindow.IsVisible)
            {
                App._mainWindow.AppWindow.Show();
            }
            NavigationHelper.NavigateTo<NotesListPage>();
            MainWindow._navigationView.SelectedItem = MainWindow._navigationView.MenuItems[1];
        }
        [RelayCommand]
        public static void NavigateToReminders()
        {
            if (!App._mainWindow!.AppWindow.IsVisible)
            {
                App._mainWindow.AppWindow.Show();
            }
            NavigationHelper.NavigateTo<RemindersListPage>();
            MainWindow._navigationView.SelectedItem = MainWindow._navigationView.MenuItems[2];
        }
        [RelayCommand]
        public static void NavigateToPlanner()
        {
            if (!App._mainWindow!.AppWindow.IsVisible)
            {
                App._mainWindow.AppWindow.Show();
            }
            NavigationHelper.NavigateTo<PlannerPage>();
            MainWindow._navigationView.SelectedItem = MainWindow._navigationView.MenuItems[3];
        }
        [RelayCommand]
        public static void NavigateToSettings()
        {
            if (!App._mainWindow!.AppWindow.IsVisible)
            {
                App._mainWindow.AppWindow.Show();
            }
            NavigationHelper.NavigateTo<SettingsPage>();
            MainWindow._navigationView.SelectedItem = MainWindow._navigationView.SettingsItem;
        }
        [RelayCommand]
        public void ExitApplication()
        {
            App.HandleClosedEvents = false;
            TrayIcon.Dispose();
            App._mainWindow?.Close();
        }
    }
}
