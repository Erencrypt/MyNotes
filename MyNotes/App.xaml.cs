using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;

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
using System.Text;
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
    private readonly StorageFolder storageFolder = ApplicationData.Current.LocalFolder;
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
        MainWindow.Closed += MainWindow_Closed;
        SingleInstanceService singleInstanceService = new();

        if (singleInstanceService.IsFirstInstance())
        {
            singleInstanceService.OnArgumentsReceived += OnArgumentsReceived;
            CreateFolders();
            ReminderCleanup();
            
            base.OnLaunched(args);
            await App.GetService<IActivationService>().ActivateAsync(args);
            timer.Interval = TimeSpan.FromSeconds(15);
            timer.Tick += Timer_Tick;
            timer.Start();
        }
    }
    private void OnArgumentsReceived(string[] obj)
    {
        MainWindow.Show();
        MainWindow.BringToFront();
    }
    private void MainWindow_Closed(object sender, WindowEventArgs args)
    {
        args.Handled = true;
        App.MainWindow.Hide();
    }
    private void Timer_Tick(object? sender, object e)
    {
        for (int i = 0; i < reminders.Count; i++)
        {
            Reminder reminder= reminders[i];
            bool rep = Convert.ToBoolean(reminder.Repeat);
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
    public static void ReminderSnoozed()
    {
        Reminder reminder = InvokedReminders[0];
        reminders.Add(reminder);
        InvokedReminders.Remove(reminder);
    }
    public static void ReminderDismissed()
    {
        Reminder reminder = InvokedReminders[0];
        MoveFile moveFile = new();
        moveFile.Move("Reminders", "Trash", reminder.ReminderHeader!, root: null!);
        InvokedReminders.Remove(reminder);
    }
    private async void CreateFolders()
    {
        await storageFolder.CreateFolderAsync("Notes", CreationCollisionOption.OpenIfExists);
        await storageFolder.CreateFolderAsync("Reminders", CreationCollisionOption.OpenIfExists);
        await storageFolder.CreateFolderAsync("Trash", CreationCollisionOption.OpenIfExists);
    }
    private async void ReminderCleanup()
    {
        int DeletedCount = 0;
        int AddedCount = 0;
        await storageFolder.CreateFolderAsync("Reminders", CreationCollisionOption.OpenIfExists);
        DirectoryInfo dinfo = new(storageFolder.Path.ToString() + "\\Reminders");
        FileInfo[] Files = dinfo.GetFiles("*.txt");
        List<FileInfo> orderedList = Files.OrderByDescending(x => x.CreationTime).ToList();
        string fullPath;
        MoveFile moveFile = new();
        foreach (FileInfo file in orderedList)
        {
            fullPath = dinfo.ToString() + "\\" + file.Name;
            string readText = File.ReadAllText(fullPath, Encoding.UTF8);
            string[] lines = readText.Split("\r\n");
            DateTime t;
            if (lines.Length == 3)
            {
                t = Convert.ToDateTime(lines[2]);
                if (t.TimeOfDay > DateTime.Now.TimeOfDay)
                {
                    reminders.Add(new Reminder() { ReminderHeader = file.Name[..^4], ReminderText = lines[1], DateTime = t.ToString(), Repeat = lines[0] });
                    AddedCount++;
                }
            }
            if (lines.Length == 4)
            {
                t = Convert.ToDateTime(lines[3] + " " + lines[2]);
                if (t.Date < DateTime.Now.Date)
                {
                    moveFile.Move("Reminders", "Trash", file.Name[..^4].ToString(), root: null!);
                    DeletedCount++;
                }
                else if (t.Date == DateTime.Now.Date)
                {
                    if (t.TimeOfDay < DateTime.Now.TimeOfDay)
                    {
                        moveFile.Move("Reminders", "Trash", file.Name[..^4].ToString(), root: null!);
                        DeletedCount++;
                    }
                }
                else
                {
                    reminders.Add(new Reminder() { ReminderHeader = file.Name[..^4], ReminderText = lines[1], DateTime = t.ToString(), Repeat = lines[0] });
                    AddedCount++;
                }
            }
        }
        if (DeletedCount > 0 && AddedCount > 0)
        {
            GetService<IAppNotificationService>().ShowDeletedMessage("Info", DeletedCount.ToString() + " Reminder(s) moved to trash due to expiration and you have "+AddedCount.ToString()+ " active reminder(s).\nYou can check out trash page to see deleted reminders.");
        }
        else if (DeletedCount > 0 && AddedCount == 0)
        {
            GetService<IAppNotificationService>().ShowDeletedMessage("Info", DeletedCount.ToString() + " Reminder(s) moved to trash due to expiration.You can check out trash page to see deleted reminders.");
        }
        else if (DeletedCount == 0 && AddedCount > 0)
        {
            GetService<IAppNotificationService>().ShowInfoMessage("Info","You have " + AddedCount.ToString() + " active reminder(s).");
        }
    }
}
