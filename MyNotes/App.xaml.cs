using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
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
using Windows.Storage;

namespace MyNotes;

public partial class App : Application
{
    // The .NET Generic Host provides dependency injection, configuration, logging, and other services.
    // https://docs.microsoft.com/dotnet/core/extensions/generic-host
    // https://docs.microsoft.com/dotnet/core/extensions/dependency-injection
    // https://docs.microsoft.com/dotnet/core/extensions/configuration
    // https://docs.microsoft.com/dotnet/core/extensions/logging
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

    public static List<Reminder> InvokedReminders
    {
        get
        {
            return invokedReminders;
        }
        set
        {
            invokedReminders = value;
        }
    }
    public static List<Reminder> Reminders
    {
        get
        {
            return reminders;
        }
        set
        {
            reminders = value;
        }
    }
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
    private static readonly string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\MyNotes";
    private static StorageFolder? sFolder;
    private readonly DispatcherTimer timer = new();
    private static List<Reminder> reminders = new();
    private static List<Reminder> invokedReminders = new();

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
        // TODO: Log and handle exceptions as appropriate.
        // https://docs.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.application.unhandledexception.
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        var mainInstance = AppInstance.FindOrRegisterForKey("MyNotes");
        MainWindow.Closed += MainWindow_Closed;
        mainInstance.Activated += MainInstance_Activated;
        if (mainInstance.IsCurrent)
        {
            StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
            sFolder = await StorageFolder.GetFolderFromPathAsync(folderPath);
            CreateSaveFile();
            CreateFolders();
            ReminderCleanup reminderCleanup = new();
            await folder.CreateFolderAsync("MyNotes", CreationCollisionOption.OpenIfExists);
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
            System.Diagnostics.Process.GetCurrentProcess().Kill();
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
        args.Handled = true;
        MainWindow.Hide();
    }
    public static void ReminderSnoozed()
    {
        Reminder reminder = InvokedReminders[0];
        reminders.Add(reminder);
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
        for (int i = 0; i < reminders.Count; i++)
        {
            Reminder reminder = reminders[i];
            bool rep = reminder.Repeat;
            DateTime tm = Convert.ToDateTime(reminder.DateTime);
            DateTime now = DateTime.Now;
            if (rep == true)
            {
                if (tm.Hour == now.Hour && tm.Minute == now.Minute)
                {
                    GetService<IAppNotificationService>().ShowReminder(reminder.ReminderHeader!, reminder.ReminderText!, tm.ToString("d/MM/yyyy\nh:mm tt"));
                    InvokedReminders.Add(reminder);
                    reminders.Remove(reminder);
                }
            }
            else if (rep == false)
            {
                if (tm.Date == now.Date)
                {
                    if (tm.Hour == now.Hour && tm.Minute == now.Minute)
                    {
                        GetService<IAppNotificationService>().ShowReminder(reminder.ReminderHeader!, reminder.ReminderText!, tm.ToString("d/MM/yyyy\nh:mm tt"));
                        InvokedReminders.Add(reminder);
                        reminders.Remove(reminder);
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
        StorageFolder SettingsStorage = await StorageFolder.GetFolderFromPathAsync(StorageFolder.Path + "\\ApplicationData");
        if (await SettingsStorage.TryGetItemAsync("LocalSettings.json") == null)
        {
            await GetService<ILocalSettingsService>().SaveSettingAsync("SaveWhenExit", true);
        }
    }
}
