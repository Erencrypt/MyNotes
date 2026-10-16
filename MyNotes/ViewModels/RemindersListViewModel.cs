using CommunityToolkit.Mvvm.Input;
using MyNotes.Data.Models;
using MyNotes.Data.Services.Interfaces;
using MyNotes.Helpers;
using MyNotes.Services.Interfaces;
using MyNotes.Views;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace MyNotes.ViewModels
{
    /// <summary>
    /// ViewModel for the reminders list page
    /// </summary>
    public partial class RemindersListViewModel : BaseViewModel
    {
        private readonly IReminderService _reminderService;
        private readonly IReminderScheduler _scheduler;
        private ObservableCollection<Reminder> _reminders = [];
        private string _searchTerm = string.Empty;
        private CancellationTokenSource? _searchCts;

        public RemindersListViewModel(IReminderService reminderService, IReminderScheduler scheduler)
        {
            _reminderService = reminderService;
            _scheduler = scheduler;

            // Subscribe to scheduler events to keep UI in sync
            _scheduler.ReminderSnoozed += (r) =>
            {
                // Update list with snoozed reminder to show snoozed indicator
                if (App._mainWindow == null)
                { return; }
                App._mainWindow.DispatcherQueue.TryEnqueue(() =>
                {
                    var existing = Reminders.FirstOrDefault(x => x.Id == r.Id);
                    if (existing != null)
                    {
                        var idx = Reminders.IndexOf(existing);
                        Reminders[idx] = r;
                    }
                });
            };
            _scheduler.ReminderDismissed += (r) =>
            {
                // Only remove non-repeating reminders from list on dismiss
                if (!r.Repeat)
                {
                    if (App._mainWindow == null)
                    { return; }
                    App._mainWindow.DispatcherQueue.TryEnqueue(() =>
                    {
                        var existing = Reminders.FirstOrDefault(x => x.Id == r.Id);
                        if (existing != null)
                        {
                            Reminders.Remove(existing);
                        }
                    });
                }
            };
        }

        /// <summary>
        /// Collection of reminders to display
        /// </summary>
        public ObservableCollection<Reminder> Reminders
        {
            get => _reminders;
            set => SetProperty(ref _reminders, value);
        }
        public string SearchTerm
        {
            get => _searchTerm;
            set
            {
                SetProperty(ref _searchTerm, value);
                _ = SearchRemindersAsync();
            }
        }

        /// <summary>
        /// Command to refresh the reminders list
        /// </summary>
        [RelayCommand]
        private async Task RefreshAsync()
        {
            await LoadRemindersAsync();
        }

        /// <summary>
        /// Command to search and filter reminders, preserving order and minimising UI churn.
        /// Cancels any previous in-flight search so rapid keystrokes don't race.
        /// </summary>
        [RelayCommand]
        private async Task SearchRemindersAsync()
        {
            // Cancel the previous search if it's still running
            _searchCts?.Cancel();
            _searchCts = new CancellationTokenSource();
            var ct = _searchCts.Token;

            try
            {
                IsLoading = true;
                var filteredReminders = await _reminderService.SearchRemindersAsync(SearchTerm, ct);
                ct.ThrowIfCancellationRequested();

                // O(1) lookups by Reminder.Id rather than O(n) list scans
                var filteredById = filteredReminders.ToDictionary(n => n.Id);
                var currentById = Reminders.ToDictionary(n => n.Id);

                // Remove reminders that are no longer in the results
                for (int i = Reminders.Count - 1; i >= 0; i--)
                {
                    if (!filteredById.ContainsKey(Reminders[i].Id))
                        Reminders.RemoveAt(i);
                }

                // Insert or move each reminder so the collection matches filteredReminders exactly
                for (int i = 0; i < filteredReminders.Count; i++)
                {
                    var reminder = filteredReminders[i];
                    if (!currentById.ContainsKey(reminder.Id))
                    {
                        // Brand-new result — insert at the correct position
                        Reminders.Insert(i, reminder);
                    }
                    else if (Reminders.IndexOf(Reminders.FirstOrDefault(n => n.Id == reminder.Id)!) != i)
                    {
                        // Already in the collection but in the wrong position — move it
                        var current = Reminders.First(n => n.Id == reminder.Id);
                        Reminders.Move(Reminders.IndexOf(current), i);
                    }
                }

            }
            catch (OperationCanceledException)
            {
                // A newer search superseded this one — discard silently
            }
            catch (Exception ex)
            {
                AppInfoBar.Show("Error", $"An error occurred while searching reminders: {ex.Message}", AlertInfoBarSeverity.Error);
                Debug.WriteLine($"Error searching reminders: {ex.Message}");
            }
            finally
            {
                // Only clear the loading flag if this search wasn't cancelled
                if (!ct.IsCancellationRequested)
                    IsLoading = false;
            }
        }

        /// <summary>
        /// Command to create a new reminder
        /// </summary>
        [RelayCommand]
        private async Task CreateReminderAsync()
        {
            NavigationHelper.NavigateTo<ReminderEditorPage>();
            await Task.CompletedTask;
        }

        /// <summary>
        /// Command to edit a reminder
        /// </summary>
        [RelayCommand]
        private async Task EditReminderAsync(object reminder)
        {
            // Navigate to ReminderEditorPage using helper
            if (reminder != null)
            {
                NavigationHelper.NavigateTo<ReminderEditorPage>(reminder);
            }
            await Task.CompletedTask;
        }

        /// <summary>
        /// Command to delete a reminder
        /// </summary>
        [RelayCommand]
        private async Task DeleteReminderAsync(Reminder reminder)
        {
            if (reminder != null)
            {
                await _reminderService.SoftDeleteReminderAsync(reminder.Id);
                Reminders.Remove(reminder);
                _scheduler.Unschedule(reminder.Id);
            }
        }

        /// <summary>
        /// Command to toggle reminder active state
        /// </summary>
        [RelayCommand]
        private async Task ToggleReminderAsync(Reminder reminder)
        {
            if (reminder != null)
            {
                reminder.IsActive = !reminder.IsActive;
                await _reminderService.UpdateReminderAsync(reminder);
                await LoadRemindersAsync();
            }
        }

        /// <summary>
        /// Load reminders from the service (full reload, used on init/refresh/delete)
        /// </summary>
        private async Task LoadRemindersAsync()
        {
            IsLoading = true;
            try
            {
                var reminders = string.IsNullOrWhiteSpace(SearchTerm)
                    ? await _reminderService.GetActiveRemindersAsync()
                    : await _reminderService.SearchRemindersAsync(SearchTerm);
                Reminders.Clear();
                foreach (var reminder in reminders)
                {
                    Reminders.Add(reminder);
                }
            }
            catch (Exception ex)
            {
                AppInfoBar.Show("Error", $"An error occurred while loading the reminders: {ex.Message}", AlertInfoBarSeverity.Error);
                Debug.WriteLine($"Error loading reminders: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async override Task Initialize()
        {
            await LoadRemindersAsync();
        }
    }
}
