using DevWinUI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.Windows.AppLifecycle;
using MyNotes.Data.Context;
using MyNotes.Data.Services;
using MyNotes.Data.Services.Interfaces;
using MyNotes.Helpers;
using MyNotes.Services;
using MyNotes.Services.Interfaces;
using MyNotes.ViewModels;
using MyNotes.Views;
using System.Diagnostics;
using Microsoft.Windows.Globalization;
using Windows.Storage;
using WinUI3Localizer;

namespace MyNotes
{
    public partial class App : Application
    {
        public new static App Current => (App)Application.Current;
        public static Window? _mainWindow { get; set; }
        private IHost? _host;
        public IThemeService ThemeService => GetService<IThemeService>();
        public ISettingService SettingService => GetService<ISettingService>();

        //global variables
        public static bool HandleClosedEvents { get; set; } = true;

        public App()
        {
            AppConfig.EnsureMyNotesDirectoryExists();
            ConfigureServices();
            InitializeDbAsync().Wait();
            InitializeComponent();
        }

        /// <summary>
        /// Configure dependency injection services
        /// </summary>
        private void ConfigureServices()
        {
            var hostBuilder = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    // Database - use a factory instead of a scoped context.
                    // Desktop apps don't have a natural "scope" like web requests,
                    // so each service creates short-lived contexts per operation.
                    services.AddDbContextFactory<AppDbContext>();

                    // Services - Singleton now, since they no longer hold a
                    // long-lived DbContext directly; they hold the factory instead.
                    services.AddSingleton<INoteService, NoteService>();
                    services.AddSingleton<IReminderService, ReminderService>();
                    services.AddSingleton<ISettingService, SettingService>();
                    services.AddSingleton<ITaskListService, TaskListService>();
                    services.AddSingleton<ITaskItemService, TaskItemService>();
                    services.AddSingleton<ITrashCleanupService, TrashCleanupService>();

                    // Reminder scheduler (in-memory, singleton)
                    services.AddSingleton<IReminderScheduler, ReminderScheduler>();

                    // Theme service as singleton since it manages app-wide state and resources
                    services.AddSingleton<IAppNotificationService, AppNotificationService>();
                    services.AddSingleton<IThemeService, ThemeService>();

                    // ViewModels
                    services.AddTransient<NotesListViewModel>();
                    services.AddTransient<RemindersListViewModel>();
                    services.AddTransient<SettingsViewModel>();
                    services.AddTransient<TrashViewModel>();
                    services.AddTransient<NoteEditorViewModel>();
                    services.AddTransient<ReminderEditorViewModel>();
                    services.AddTransient<PlannerViewModel>();
                    services.AddTransient<TaskListEditorViewModel>();
                });

            _host = hostBuilder.Build();
        }

        /// <summary>
        /// Get service from DI container.
        /// Safe now: everything resolved through this is Singleton, so
        /// resolving from the root provider is valid (no scope needed).
        /// </summary>
        public static T GetService<T>() where T : class
        {
            if (Current is App app && app._host != null)
            {
                return app._host.Services.GetRequiredService<T>();
            }
            throw new InvalidOperationException("Service provider not available");
        }

        protected async override void OnLaunched(LaunchActivatedEventArgs args)
        {


            var mainInstance = AppInstance.FindOrRegisterForKey("MyNotes");
            mainInstance.Activated += MainInstance_Activated;

            if (mainInstance.IsCurrent)
            {

                await InitializeLocalizer();

                _mainWindow = new MainWindow();
                _mainWindow!.Closed += Window_Closed;
                if (App._mainWindow.Content == null)
                {
                    App._mainWindow.Content = MainWindow._rootFrame;
                }

                await InitializeTheme();

                try
                {
                    var retentionDays = await SettingService.GetSettingAsync<int>(DataKeys.TrashRetentionDays, 30);
                    if (retentionDays != 0)
                    {
                        await GetService<ITrashCleanupService>().PermanentlyDeleteExpiredItemsAsync(retentionDays);
                    }
                }
                catch (Exception ex)
                {
                    LogWriter.Log($"Failed to clean up expired trash items: {ex.Message}", LogWriter.LogLevel.Error);
                }

                try
                {
                    await GetService<IReminderScheduler>().InitializeAsync();
                    await GetService<IAppNotificationService>().InitializeAsync();
                }
                catch (Exception ex)
                {
                    LogWriter.Log("Failed to initialize ReminderScheduler: " + ex.Message, LogWriter.LogLevel.Error);
                }

                base.OnLaunched(args);
                App._mainWindow.Activate();
            }
            if (!mainInstance.IsCurrent)
            {
                var activatedEventArgs = AppInstance.GetCurrent().GetActivatedEventArgs();
                await mainInstance.RedirectActivationToAsync(activatedEventArgs);
                Process.GetCurrentProcess().Kill();
                return;
            }
        }

        #region Window activation and closing
        private void MainInstance_Activated(object? sender, AppActivationArguments e)
        {
            if (_mainWindow==null)
            {return;}
            _mainWindow.AppWindow.Show();
        }
        private async void Window_Closed(object sender, WindowEventArgs args)
        {
            if (_mainWindow == null)
            { return; }
            if (HandleClosedEvents == true)
            {
                args.Handled = true;
                _mainWindow.AppWindow.Hide();
            }
            else
            {
                if (_host != null)
                {
                    await _host.StopAsync();
                    _host.Dispose();
                }
            }
        }
        #endregion

        #region Localization
        private static async Task InitializeLocalizer()
        {
            string StringsFolderPath = Path.Combine(AppContext.BaseDirectory, "Strings");
            StorageFolder stringsFolder = await StorageFolder.GetFolderFromPathAsync(StringsFolderPath);

            ILocalizer localizer = await new LocalizerBuilder()
                .AddStringResourcesFolderForLanguageDictionaries(StringsFolderPath)
                .SetOptions(options =>
                {
                    options.DefaultLanguage = "en-US";
                })
                .Build();

            string language = await GetService<ISettingService>().GetSettingAsync(DataKeys.Language, "en-US") ?? "en-US";
            ApplicationLanguages.PrimaryLanguageOverride = language;
            await localizer.SetLanguage(language);
        }
        #endregion

        #region Theme
        private async Task InitializeTheme()
        {
            var theme = await SettingService.GetSettingAsync(DataKeys.Theme, ElementTheme.Default);
            var backdrop = await SettingService.GetSettingAsync(DataKeys.Backdrop, BackdropType.Mica);

            ThemeService.ConfigureAutoSave(false);
            ThemeService.Initialize(_mainWindow);
            await ThemeService.SetElementThemeWithoutSaveAsync(theme);
            await ThemeService.SetBackdropTypeWithoutSaveAsync(backdrop);

            ThemeService.BackdropChanged += ThemeService_BackdropChanged;
            ThemeService.ThemeChanged += ThemeService_ThemeChanged;
        }

        private async void ThemeService_ThemeChanged(object? sender, ElementTheme e)
        {
            await SettingService.SetSettingAsync(DataKeys.Theme, e);
        }

        private async void ThemeService_BackdropChanged(object? sender, BackdropType e)
        {
            await SettingService.SetSettingAsync(DataKeys.Backdrop, e);
        }
        #endregion

        #region Database initialization
        private async Task InitializeDbAsync()
        {
            if (_host != null)
            {
                // Start host so hosted services run
                await _host.StartAsync();

                // Create and initialize the database.
                // Use the factory to create a short-lived context just for migration.
                var contextFactory = _host.Services.GetRequiredService<IDbContextFactory<AppDbContext>>();
                await using var context = await contextFactory.CreateDbContextAsync();
                await context.Database.MigrateAsync();
            }
        }
        #endregion
    }
}