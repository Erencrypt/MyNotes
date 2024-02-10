using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using Microsoft.Win32;
using Microsoft.Windows.AppLifecycle;
using MyNotes.Activation;
using MyNotes.Contracts.Services;
using MyNotes.Core.Contracts.Services;
using MyNotes.Core.Services;
using MyNotes.Helpers;
using MyNotes.Models;
using MyNotes.Notifications;
using MyNotes.Services;
using MyNotes.ViewModels;
using MyNotes.Views;
using System.Diagnostics;
using Windows.Storage;

namespace MyNotes;

public partial class App : Application
{
    public IHost Host
    {
        get;
    }
    public static T GetService<T>()
        where T : class
    {
        if ((App.Current as App)!.Host.Services.GetService(typeof(T)) is not T service)
        {
            throw new ArgumentException($"{typeof(T)} needs to be registered in ConfigureServices within App.xaml.cs.");
        }
        return service;
    }
    public static WindowEx MainWindow { get; } = new MainWindow();

    public static List<Reminder> InvokedReminders { get; set; } = new();
    public static List<Reminder> Reminders { get; set; } = new();
    public static StorageFolder StorageFolder
    {
        get
        {
            return sFolder!;
        }
        set
        {
            sFolder = value;
        }
    }

    public static Windows.ApplicationModel.StartupTask? StartupTask { get; }

    private static readonly RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true)!;
    private static readonly string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\MyNotes";
    private static StorageFolder? sFolder;
    private readonly DispatcherTimer timer = new();

    public App()
    {
        InitializeComponent();
        Host = Microsoft.Extensions.Hosting.Host.
        CreateDefaultBuilder().
        UseContentRoot(AppContext.BaseDirectory).
        ConfigureServices((context, services) =>
        {
            // Default Activation Handler
            services.AddTransient<ActivationHandler<LaunchActivatedEventArgs>, DefaultActivationHandler>();

            // Other Activation Handlers
            services.AddTransient<IActivationHandler, AppNotificationActivationHandler>();

            // Services
            services.AddSingleton<IAppNotificationService, AppNotificationService>();
            services.AddSingleton<ILocalSettingsService, LocalSettingsService>();
            services.AddSingleton<IThemeSelectorService, ThemeSelectorService>();
            services.AddTransient<INavigationViewService, NavigationViewService>();

            services.AddSingleton<IActivationService, ActivationService>();
            services.AddSingleton<IPageService, PageService>();
            services.AddSingleton<INavigationService, NavigationService>();

            // Core Services
            services.AddSingleton<IFileService, FileService>();

            // Views and ViewModels
            services.AddTransient<TrashViewModel>();
            services.AddTransient<TrashPage>();
            services.AddTransient<SettingsViewModel>();
            services.AddTransient<RemindersViewModel>();
            services.AddTransient<RemindersPage>();
            services.AddTransient<NoteDetailsViewModel>();
            services.AddTransient<NoteDetailsPage>();
            services.AddTransient<NotesViewModel>();
            services.AddTransient<NotesPage>();
            services.AddTransient<ShellPage>();
            services.AddTransient<ShellViewModel>();

            // Configuration
            services.Configure<LocalSettingsOptions>(context.Configuration.GetSection(nameof(LocalSettingsOptions)));

        }).
        Build();

        GetService<IAppNotificationService>().Initialize();

        UnhandledException += App_UnhandledException;
    }
    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        if (e.Exception != null)
        {
            LogWriter.Log("Unhandled Exception :" + e.Exception.Message, LogWriter.LogLevel.Error);
        }
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        var mainInstance = AppInstance.FindOrRegisterForKey("MyNotes");
        MainWindow.Closed += MainWindow_Closed;
        mainInstance.Activated += MainInstance_Activated;
        if (mainInstance.IsCurrent)
        {
            StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
            await folder.CreateFolderAsync("MyNotes", CreationCollisionOption.OpenIfExists);
            sFolder = await StorageFolder.GetFolderFromPathAsync(folderPath);
            CreateFolders();
            CreateSaveFile();

            LogWriter.CheckLogFile();
            LogWriter.Log("App Started", LogWriter.LogLevel.Info);

            ReminderCleanup reminderCleanup = new();
            reminderCleanup.Clean(true);

            base.OnLaunched(args);
            await GetService<IActivationService>().ActivateAsync(args);
            timer.Interval = TimeSpan.FromSeconds(15);
            timer.Tick += Timer_Tick;
            timer.Start();
        }
        if (!mainInstance.IsCurrent)
        {
            var activatedEventArgs = AppInstance.GetCurrent().GetActivatedEventArgs();
            await mainInstance.RedirectActivationToAsync(activatedEventArgs);
            Process.GetCurrentProcess().Kill();
            return;
        }
    }

    private void MainInstance_Activated(object? sender, AppActivationArguments e)
    {
        MainWindow.Show();
        MainWindow.BringToFront();
    }
    private void MainWindow_Closed(object sender, WindowEventArgs args)
    {
        if (SettingsPage.IsClose == false)
        {
            args.Handled = true;
            MainWindow.Hide();
        }
    }
    public static void ReminderSnoozed()
    {
        Reminder reminder = InvokedReminders[0];
        Reminders.Add(reminder);
        InvokedReminders.Remove(reminder);
    }
    public static void ReminderDismissed()
    {
        Reminder reminder = InvokedReminders[0];
        bool repeat = reminder.Repeat;
        if (repeat)
        {
            InvokedReminders.Remove(reminder);
        }
        else
        {
            MoveFile moveFile = new();
            moveFile.Move("Reminders", "Trash", reminder.ReminderHeader!, root: null!);
            InvokedReminders.Remove(reminder);
        }
    }
    private void Timer_Tick(object? sender, object e)
    {
        for (int i = 0; i < Reminders.Count; i++)
        {
            Reminder reminder = Reminders[i];
            bool rep = reminder.Repeat;
            DateTime tm = Convert.ToDateTime(reminder.DateTime);
            DateTime now = DateTime.Now;
            if (rep == true)
            {
                if (tm.Hour == now.Hour && tm.Minute == now.Minute)
                {
                    GetService<IAppNotificationService>().ShowReminder(reminder.ReminderHeader!, reminder.ReminderText!, tm.ToString("h:mm tt  - d/MM/yyyy"), reminder.Alarm);
                    InvokedReminders.Add(reminder);
                    Reminders.Remove(reminder);
                }
            }
            else if (rep == false)
            {
                if (tm.Date == now.Date)
                {
                    if (tm.Hour == now.Hour && tm.Minute == now.Minute)
                    {
                        GetService<IAppNotificationService>().ShowReminder(reminder.ReminderHeader!, reminder.ReminderText!, tm.ToString("d/MM/yyyy\nh:mm tt"), reminder.Alarm);
                        InvokedReminders.Add(reminder);
                        Reminders.Remove(reminder);
                    }
                }
            }
        }
    }
    private static async void CreateFolders()
    {
        List<string> folders = new() { "Notes", "Reminders", "Trash", "ApplicationData" };
        foreach (string folder in folders)
        {
            await StorageFolder.CreateFolderAsync(folder, CreationCollisionOption.OpenIfExists);
        }
    }
    private static async void CreateSaveFile()
    {
        await StorageFolder.CreateFolderAsync("ApplicationData", CreationCollisionOption.OpenIfExists);
        StorageFolder SettingsStorage = await StorageFolder.GetFolderFromPathAsync(StorageFolder.Path + "\\ApplicationData");
        if (await SettingsStorage.TryGetItemAsync("LocalSettings.json") == null)
        {
            await GetService<ILocalSettingsService>().SaveSettingAsync("SaveWhenExit", true);
            await GetService<ILocalSettingsService>().SaveSettingAsync("SpellCheck", false);

            if (RuntimeHelper.IsMSIX)
            {
                switch (StartupTask.State)
                {
                    case Windows.ApplicationModel.StartupTaskState.Disabled:
                        Windows.ApplicationModel.StartupTaskState newState = await StartupTask.RequestEnableAsync();
                        Debug.WriteLine("Request to enable startup, result = {0}", newState);
                        break;
                    case Windows.ApplicationModel.StartupTaskState.DisabledByUser:
                        break;
                    case Windows.ApplicationModel.StartupTaskState.DisabledByPolicy:
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
    }
}
