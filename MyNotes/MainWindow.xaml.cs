using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using MyNotes.Helpers;
using MyNotes.Views;
using MyNotes.Views.Controls;
using System.Drawing.Imaging;
using WinUI3Localizer;

namespace MyNotes
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private readonly ILocalizer _localizer = Localizer.Get();
        public static Frame _rootFrame { get; private set; } = new();
        public static NavigationView _navigationView { get; set; } = new();
        public static AlertInfoBar? AppAlertInfoBar { get; private set; }
        public MainWindow()
        {
            InitializeComponent();

            _rootFrame = rootFrame;
            _navigationView = NavView;
            AppAlertInfoBar = AlertInfoBarControl;

            // Hide the default system title bar.
            ExtendsContentIntoTitleBar = true; // Extend the content into the title bar and hide the default titlebar
            SetTitleBar(AppTitleBar); // Set the custom title bar

            var presenter = AppWindow.Presenter as OverlappedPresenter;
            presenter!.PreferredMinimumWidth = 504;
            presenter.PreferredMinimumHeight = 492;

            AppWindow.Resize(new Windows.Graphics.SizeInt32(768, 512));
            AppWindow.Move(new Windows.Graphics.PointInt32(100, 100));

            AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets/WindowIcon.ico"));

            NavView.SelectedItem = NavView.MenuItems[1];

            NavigationHelper.NavigateTo<NotesListPage>(new SuppressNavigationTransitionInfo { });

            Activated += MainWindow_Activated;
        }

        private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            MainWindowLocalization();
        }

        private void MainWindowLocalization()
        {
            Title = _localizer.GetLocalizedString("AppDisplayName");
            AppTitleBar.Title= _localizer.GetLocalizedString("AppDisplayName");
            MenuHeader.Content = _localizer.GetLocalizedString("MainWindow_MenuHeader");
            NotesItem.Content = _localizer.GetLocalizedString("MainWindow_NotesItem");
            RemindersItem.Content = _localizer.GetLocalizedString("MainWindow_RemindersItem");
            PlannerItem.Content = _localizer.GetLocalizedString("MainWindow_PlannerItem");
            TrashItem.Content = _localizer.GetLocalizedString("MainWindow_TrashItem");
            var settingsItem = NavView.SettingsItem as NavigationViewItem;
            if (settingsItem != null)
            {
                settingsItem.Content = _localizer.GetLocalizedString("MainWindow_SettingsItem");
            }
        }
        private void AppTitleBar_PaneToggleRequested(TitleBar sender, object args)
        {
            NavView.IsPaneOpen = !NavView.IsPaneOpen;
        }

        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected == true)
            {
                rootFrame.Navigate(typeof(SettingsPage));
            }
            else if (args.SelectedItemContainer != null)
            {
                var selectedItemTag = args.SelectedItemContainer.Tag.ToString();

                // Navigate to the corresponding page based on the selected item tag
                switch (selectedItemTag)
                {
                    case "notes":
                        rootFrame.Navigate(typeof(NotesListPage));
                        break;
                    case "reminders":
                        rootFrame.Navigate(typeof(RemindersListPage));
                        break;
                    case "planner":
                        rootFrame.Navigate(typeof(PlannerPage));
                        break;
                    case "trash":
                        rootFrame.Navigate(typeof(TrashPage));
                        break;
                    default:
                        break;
                }
            }

        }
    }
}
