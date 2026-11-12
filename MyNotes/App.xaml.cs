using DevWinUI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.AppLifecycle;
using Microsoft.Windows.Globalization;
using MyNotes.Data.Context;
using MyNotes.Data.Models.Enums;
using MyNotes.Data.Services;
using MyNotes.Data.Services.Interfaces;
using MyNotes.Helpers;
using MyNotes.Services;
using MyNotes.Services.Interfaces;
using MyNotes.ViewModels;
using MyNotes.Views.Dialogs;
using System.Diagnostics;
using System.Globalization;
using Windows.Storage;
using WinUI3Localizer;

namespace MyNotes
{
    public partial class App : Application
    {
        public new static App Current => (App)Application.Current;
        public static Window? _mainWindow { get; set; }
        private IHost? _host;
        private readonly SemaphoreSlim _updateCheckSemaphore = new(1, 1);
        public IThemeService ThemeService => GetService<IThemeService>();
        public ISettingService SettingService => GetService<ISettingService>();

        //global variables
        public static bool HandleClosedEvents { get; set; } = true;
        /// <summary>Cached time format setting, read by the date converters (loaded at launch, updated from Settings).</summary>
        public static TimeFormat TimeFormat { get; set; } = TimeFormat.TwelveHour;

        public App()
        {
            AppConfig.EnsureMyNotesDirectoryExists();
            LogWriter.CheckLogFile();
            ConfigureServices();
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
                    services.AddSingleton<IAppUpdateService, AppUpdateService>();
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
                await InitializeDbAsync(); // must run first
                await InitializeLocalizer();
                TimeFormat = await SettingService.GetSettingAsync(DataKeys.HourFormat, TimeFormat.TwelveHour);
                // Must run before MainWindow is created: its constructor opens the notes list.
                await FillMissingNotePlainTextAsync();

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
                    await GetService<IAppNotificationService>().InitializeAsync();
                }
                catch (Exception ex)
                {
                    LogWriter.Log($"Failed to initialize app notifications: {ex}", LogWriter.LogLevel.Error);
                }

                try
                {
                    await GetService<IReminderScheduler>().InitializeAsync();
                }
                catch (Exception ex)
                {
                    LogWriter.Log($"Failed to initialize reminder scheduler: {ex}", LogWriter.LogLevel.Error);
                }

                base.OnLaunched(args);
                App._mainWindow.Activate();
                await CheckForUpdatesAsync(isManual: false);
            }
            if (!mainInstance.IsCurrent)
            {
                var activatedEventArgs = AppInstance.GetCurrent().GetActivatedEventArgs();
                await mainInstance.RedirectActivationToAsync(activatedEventArgs);
                Process.GetCurrentProcess().Kill();
                return;
            }
        }
        private async Task ShutdownServicesAsync()
        {
            var host = _host;
            if (host is null)
            {
                return;
            }

            GetService<IAppNotificationService>().Unregister();
            await host.StopAsync();
            host.Dispose();
            _host = null;
        }

        #region Window activation and closing
        private void MainInstance_Activated(object? sender, AppActivationArguments e)
        {
            if (_mainWindow == null)
            { return; }
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
                try
                {
                    await ShutdownServicesAsync();
                }
                catch (Exception ex)
                {
                    LogWriter.Log($"Failed to shut down application services: {ex}", LogWriter.LogLevel.Error);
                }
            }
        }
        #endregion

        #region Update checking and installation
        internal static async Task<XamlRoot> GetMainWindowXamlRootAsync()
        {
            var rootFrame = MainWindow._rootFrame;
            if (rootFrame.XamlRoot is { } xamlRoot)
            {
                return xamlRoot;
            }

            var loadedSource = new TaskCompletionSource<XamlRoot>(TaskCreationOptions.RunContinuationsAsynchronously);
            RoutedEventHandler loadedHandler = null!;
            loadedHandler = (_, _) =>
            {
                if (rootFrame.XamlRoot is not { } loadedXamlRoot)
                {
                    return;
                }

                rootFrame.Loaded -= loadedHandler;
                loadedSource.TrySetResult(loadedXamlRoot);
            };

            rootFrame.Loaded += loadedHandler;
            if (rootFrame.XamlRoot is { } currentXamlRoot)
            {
                rootFrame.Loaded -= loadedHandler;
                return currentXamlRoot;
            }

            return await loadedSource.Task;
        }

        public async Task CheckForUpdatesAsync(bool isManual)
        {
            await _updateCheckSemaphore.WaitAsync();
            try
            {
                var updateService = GetService<IAppUpdateService>();
                var localizer = Localizer.Get();

                if (!updateService.IsInstalled)
                {
                    if (isManual)
                    {
                        AppInfoBar.Show(
                            localizer.GetLocalizedString("Update_CheckTitle"),
                            localizer.GetLocalizedString("Update_InstalledReleaseRequired"),
                            AlertInfoBarSeverity.Informational);
                    }
                    return;
                }

                if (isManual)
                {
                    AppInfoBar.Show(localizer.GetLocalizedString("Update_CheckTitle"), localizer.GetLocalizedString("Update_Checking"));
                }

                var updateInfo = await updateService.CheckForUpdatesAsync();
                if (updateInfo is null)
                {
                    if (isManual)
                    {
                        AppInfoBar.Show(
                            localizer.GetLocalizedString("Update_CheckTitle"),
                            localizer.GetLocalizedString("Update_UpToDate"),
                            AlertInfoBarSeverity.Success);
                    }
                    return;
                }

                var version = updateInfo.TargetFullRelease.Version.ToString();
                var updateTitle = localizer.GetLocalizedString("Update_AvailableTitle");
                var updateMessage = localizer.GetLocalizedString("Update_VersionAvailable").Replace("{0}", version, StringComparison.Ordinal);

                try
                {
                    var notificationMessage = localizer.GetLocalizedString("Update_NotificationVersionAvailable").Replace("{0}", version, StringComparison.Ordinal);
                    var notificationShown = await GetService<IAppNotificationService>().ShowUpdateAvailableMessage(updateTitle, notificationMessage);
                    if (!notificationShown)
                    {
                        throw new InvalidOperationException("Windows did not accept the update notification.");
                    }
                }
                catch (Exception ex)
                {
                    LogWriter.Log($"Failed to show the update notification: {ex}", LogWriter.LogLevel.Error);
                    AppInfoBar.Show(updateTitle, localizer.GetLocalizedString("Update_NotificationFailed"), AlertInfoBarSeverity.Warning);
                }

                UpdateAvailableDialog updateDialog = new(updateTitle, updateMessage)
                {
                    XamlRoot = await GetMainWindowXamlRootAsync()
                };

                if (await updateDialog.ShowAsync() != ContentDialogResult.Primary)
                { return; }

                AppInfoBar.Show(localizer.GetLocalizedString("Update_DownloadingTitle"), localizer.GetLocalizedString("Update_Downloading"));

                await updateService.DownloadUpdatesAsync(updateInfo);
                await ShutdownServicesAsync();
                updateService.ApplyUpdatesAndRestart(updateInfo);
            }
            catch (Exception ex)
            {
                LogWriter.Log($"Update check or installation failed: {ex}", LogWriter.LogLevel.Error);
                var localizer = Localizer.Get();
                AppInfoBar.Show(localizer.GetLocalizedString("Update_FailedTitle"),
                    string.Format(localizer.GetLocalizedString("Update_ErrorMessage"), ex.Message),
                    AlertInfoBarSeverity.Error);
            }
            finally
            {
                _updateCheckSemaphore.Release();
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

            LocalizationFallback.ApplyFallback(localizer, "en-US");

            string language = await GetService<ISettingService>().GetSettingAsync(DataKeys.Language, "en-US") ?? "en-US";
            ApplicationLanguages.PrimaryLanguageOverride = language;
            AppDbContext.SearchCulture = CultureInfo.GetCultureInfo(language);
            await localizer.SetLanguage(language);
        }
        #endregion

        #region Theme
        private async Task InitializeTheme()
        {
            if (_mainWindow == null)
            { return; }

            var theme = await SettingService.GetSettingAsync(DataKeys.Theme, ElementTheme.Default);
            var backdrop = await SettingService.GetSettingAsync(DataKeys.Backdrop, BackdropType.Mica);

            ThemeService.ConfigureAutoSave(false);
            ThemeService.Initialize(_mainWindow);
            await ThemeService.SetElementThemeWithoutSaveAsync(theme);
            await ThemeService.SetBackdropTypeWithoutSaveAsync(backdrop);

            var useSystemAccent = await SettingService.GetSettingAsync(DataKeys.UseSystemAccentColor, true);
            var customAccent = await SettingService.GetSettingAsync<string>(DataKeys.CustomAccentColor, null);
            AccentColorHelper.Apply(
                useSystemAccent || string.IsNullOrEmpty(customAccent) ? null : HexConverter.HexToColor(customAccent),
                _mainWindow.Content as FrameworkElement);

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
        /// <summary>
        /// One-time fill of Note.PlainText for notes saved before the column existed (or imported
        /// by the legacy migration tool). Runs on the UI thread because RTF decoding uses a RichEditBox.
        /// After the first run there is nothing left to fill, so this is just one cheap query.
        /// </summary>
        /// Note: Remove after few versions
        private async Task FillMissingNotePlainTextAsync()
        {
            try
            {
                var noteService = GetService<INoteService>();
                var notes = await noteService.GetNotesMissingPlainTextAsync();
                if (notes.Count == 0)
                {
                    return;
                }

                var plainTexts = new Dictionary<Guid, string>();
                foreach (var note in notes)
                {
                    try
                    {
                        var plainText = RtfHelper.ToPlainText(note.Content);
                        if (plainText.Length > 0)
                        {
                            plainTexts[note.Id] = plainText;
                        }
                    }
                    catch (Exception ex)
                    {
                        LogWriter.Log($"Failed to convert note {note.Id} to plain text: {ex.Message}", LogWriter.LogLevel.Warning);
                    }
                }

                await noteService.SetPlainTextsAsync(plainTexts);
            }
            catch (Exception ex)
            {
                LogWriter.Log($"Failed to fill note plain text: {ex}", LogWriter.LogLevel.Error);
            }
        }

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