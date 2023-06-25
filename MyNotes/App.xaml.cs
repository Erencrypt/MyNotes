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
using System.Text.RegularExpressions;
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
    private readonly StorageFolder storageFolder = ApplicationData.Current.LocalFolder;
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

        App.GetService<IAppNotificationService>().Initialize();

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
            ReminderCleanup();
            base.OnLaunched(args);
            await App.GetService<IActivationService>().ActivateAsync(args);
        }

        //TODO: notification example
        //notificationService.ShowReminder("header text", "this is reminder text.", "time section");
        //App.GetService<IAppNotificationService>().Show(string.Format("AppNotificationSamplePayload".GetLocalized(), AppContext.BaseDirectory));
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
    private void ReminderCleanup()
    {
        //TODO: Send app notification about expired reminders
        int count = 0;
        DirectoryInfo dinfo = new(storageFolder.Path.ToString() + "\\Reminders");
        FileInfo[] Files = dinfo.GetFiles("*.txt");
        List<FileInfo> orderedList = Files.OrderByDescending(x => x.CreationTime).ToList();
        string fullPath;
        foreach (FileInfo file in orderedList)
        {
            fullPath = dinfo.ToString() + "\\" + file.Name;
            string readText = File.ReadAllText(fullPath, Encoding.UTF8);
            string[] lines = readText.Split("\r\n");
            DateTime t;
            if (lines.Length == 4)
            {
                t = Convert.ToDateTime(lines[3] + " " + lines[2]);
                if (t.Date < DateTime.Now.Date)
                {
                    MoveFile moveFile = new();
                    moveFile.Move("Reminders", "Trash", file.Name[..^4].ToString(), root: null!);
                    count++;
                }
            }
        }
        if (count > 0)
        {
            App.GetService<IAppNotificationService>().ShowInfoMessage("Info",count.ToString()+" Reminder(s) moved to trash due to expiration, you can check out trash page to see them.");
        }
    }
}
