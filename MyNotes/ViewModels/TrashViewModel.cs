using CommunityToolkit.Mvvm.Input;
using MyNotes.Data.Models;
using MyNotes.Data.Services.Interfaces;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace MyNotes.ViewModels
{
    /// <summary>
    /// ViewModel for the trash page
    /// </summary>
    public partial class TrashViewModel : BaseViewModel
    {
        private readonly INoteService _noteService;
        private readonly IReminderService _reminderService;
        private readonly ITaskListService _taskListService;
        private ObservableCollection<Note> _deletedNotes = [];
        private ObservableCollection<Reminder> _deletedReminders = [];
        private ObservableCollection<TaskList> _deletedTaskLists = [];
        private string _notesSearchTerm = string.Empty;
        private string _remindersSearchTerm = string.Empty;
        private string _taskListsSearchTerm = string.Empty;

        private CancellationTokenSource? _notesSearchCts;
        private CancellationTokenSource? _remindersSearchCts;
        private CancellationTokenSource? _taskListsSearchCts;



        public TrashViewModel(INoteService noteService, IReminderService reminderService, ITaskListService taskListService)
        {
            _noteService = noteService;
            _reminderService = reminderService;
            _taskListService = taskListService;
        }

        /// <summary>
        /// Collection of deleted notes
        /// </summary>
        public ObservableCollection<Note> DeletedNotes
        {
            get => _deletedNotes;
            set => SetProperty(ref _deletedNotes, value);
        }
        public string NotesSearchTerm
        {
            get => _notesSearchTerm;
            set
            {
                SetProperty(ref _notesSearchTerm, value);
                _ = SearchDeletedNotesAsync();
            }
        }
        public string RemindersSearchTerm
        {
            get => _remindersSearchTerm;
            set
            {
                SetProperty(ref _remindersSearchTerm, value);
                _ = SearchDeletedRemindersAsync();
            }
        }

        public string TaskListsSearchTerm
        {
            get => _taskListsSearchTerm;
            set
            {
                SetProperty(ref _taskListsSearchTerm, value);
                _ = SearchDeletedTaskListsAsync();
            }
        }

        /// <summary>
        /// Collection of deleted reminders
        /// </summary>
        public ObservableCollection<Reminder> DeletedReminders
        {
            get => _deletedReminders;
            set => SetProperty(ref _deletedReminders, value);
        }

        /// <summary>
        /// Collection of deleted task lists
        /// </summary>
        public ObservableCollection<TaskList> DeletedTaskLists
        {
            get => _deletedTaskLists;
            set => SetProperty(ref _deletedTaskLists, value);
        }
        /// <summary>
        /// Command to search and filter notes, preserving order and minimising UI churn.
        /// Cancels any previous in-flight search so rapid keystrokes don't race.
        /// </summary>
        [RelayCommand]
        private async Task SearchDeletedNotesAsync()
        {
            // Cancel the previous search if it's still running
            _notesSearchCts?.Cancel();
            _notesSearchCts = new CancellationTokenSource();
            var ctNote = _notesSearchCts.Token;

            try
            {
                IsLoading = true;

                var filteredNotes = await _noteService.SearchDeletedNotesAsync(NotesSearchTerm, ctNote);

                ctNote.ThrowIfCancellationRequested();

                // O(1) lookups by Note.Id rather than O(n) list scans
                var filteredById = filteredNotes.ToDictionary(n => n.Id);
                var currentById = DeletedNotes.ToDictionary(n => n.Id);

                // Remove notes that are no longer in the results
                for (int i = DeletedNotes.Count - 1; i >= 0; i--)
                {
                    if (!filteredById.ContainsKey(DeletedNotes[i].Id))
                        DeletedNotes.RemoveAt(i);
                }

                // Insert or move each note so the collection matches filteredNotes exactly
                for (int i = 0; i < filteredNotes.Count; i++)
                {
                    var note = filteredNotes[i];
                    if (!currentById.ContainsKey(note.Id))
                    {
                        // Brand-new result — insert at the correct position
                        DeletedNotes.Insert(i, note);
                    }
                    else if (DeletedNotes.IndexOf(DeletedNotes.FirstOrDefault(n => n.Id == note.Id)!) != i)
                    {
                        // Already in the collection but in the wrong position — move it
                        var current = DeletedNotes.First(n => n.Id == note.Id);
                        DeletedNotes.Move(DeletedNotes.IndexOf(current), i);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // A newer search superseded this one — discard silently
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error searching notes: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
            finally
            {
                // Only clear the loading flag if this search wasn't cancelled
                if (!ctNote.IsCancellationRequested)
                    IsLoading = false;
            }
        }

        /// <summary>
        /// Command to search and filter reminders, preserving order and minimising UI churn.
        /// Cancels any previous in-flight search so rapid keystrokes don't race.
        /// </summary>
        [RelayCommand]
        private async Task SearchDeletedRemindersAsync()
        {
            // Cancel the previous search if it's still running
            _remindersSearchCts?.Cancel();
            _remindersSearchCts = new CancellationTokenSource();
            var ctReminder = _remindersSearchCts.Token;

            try
            {
                IsLoading = true;
                var filteredReminders = await _reminderService.SearchDeletedRemindersAsync(RemindersSearchTerm, ctReminder);
                ctReminder.ThrowIfCancellationRequested();

                // O(1) lookups by Note.Id rather than O(n) list scans
                var filteredById = filteredReminders.ToDictionary(n => n.Id);
                var currentById = DeletedReminders.ToDictionary(n => n.Id);

                // Remove notes that are no longer in the results
                for (int i = DeletedReminders.Count - 1; i >= 0; i--)
                {
                    if (!filteredById.ContainsKey(DeletedReminders[i].Id))
                        DeletedReminders.RemoveAt(i);
                }

                // Insert or move each note so the collection matches filteredNotes exactly
                for (int i = 0; i < filteredReminders.Count; i++)
                {
                    var note = filteredReminders[i];
                    if (!currentById.ContainsKey(note.Id))
                    {
                        // Brand-new result — insert at the correct position
                        DeletedReminders.Insert(i, note);
                    }
                    else if (DeletedReminders.IndexOf(DeletedReminders.FirstOrDefault(n => n.Id == note.Id)!) != i)
                    {
                        // Already in the collection but in the wrong position — move it
                        var current = DeletedReminders.First(n => n.Id == note.Id);
                        DeletedReminders.Move(DeletedReminders.IndexOf(current), i);
                    }
                }

            }
            catch (OperationCanceledException)
            {
                // A newer search superseded this one — discard silently
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error searching notes: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
            finally
            {
                // Only clear the loading flag if this search wasn't cancelled
                if (!ctReminder.IsCancellationRequested)
                    IsLoading = false;
            }
        }

        /// <summary>
        /// Command to search and filter deleted task lists.
        /// </summary>
        [RelayCommand]
        private async Task SearchDeletedTaskListsAsync()
        {
            _taskListsSearchCts?.Cancel();
            _taskListsSearchCts = new CancellationTokenSource();
            var ctTaskList = _taskListsSearchCts.Token;

            try
            {
                IsLoading = true;
                var filteredTaskLists = await _taskListService.SearchDeletedTaskListsAsync(TaskListsSearchTerm, ctTaskList);
                ctTaskList.ThrowIfCancellationRequested();

                var filteredById = filteredTaskLists.ToDictionary(tl => tl.Id);
                var currentById = DeletedTaskLists.ToDictionary(tl => tl.Id);

                for (int i = DeletedTaskLists.Count - 1; i >= 0; i--)
                {
                    if (!filteredById.ContainsKey(DeletedTaskLists[i].Id))
                        DeletedTaskLists.RemoveAt(i);
                }

                for (int i = 0; i < filteredTaskLists.Count; i++)
                {
                    var taskList = filteredTaskLists[i];
                    if (!currentById.ContainsKey(taskList.Id))
                    {
                        DeletedTaskLists.Insert(i, taskList);
                    }
                    else if (DeletedTaskLists.IndexOf(DeletedTaskLists.FirstOrDefault(tl => tl.Id == taskList.Id)!) != i)
                    {
                        var current = DeletedTaskLists.First(tl => tl.Id == taskList.Id);
                        DeletedTaskLists.Move(DeletedTaskLists.IndexOf(current), i);
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error searching task lists: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
            finally
            {
                if (!ctTaskList.IsCancellationRequested)
                    IsLoading = false;
            }
        }

        /// <summary>
        /// Command to refresh the trash content
        /// </summary>
        [RelayCommand]
        private async Task RefreshNotesAsync()
        {
            await LoadDeletedNotes();
        }
        /// <summary>
        /// Command to refresh the trash content
        /// </summary>
        [RelayCommand]
        private async Task RefreshRemindersAsync()
        {
            await LoadDeletedReminders();
        }

        /// <summary>
        /// Command to refresh the task list trash content
        /// </summary>
        [RelayCommand]
        private async Task RefreshTaskListsAsync()
        {
            await LoadDeletedTaskLists();
        }

        /// <summary>
        /// Command to restore a deleted note
        /// </summary>
        [RelayCommand]
        private async Task RestoreNoteAsync(Note note)
        {
            if (note != null)
            {
                await _noteService.RestoreNoteAsync(note.Id);
                DeletedNotes.Remove(note);
            }
        }

        /// <summary>
        /// Command to restore a deleted reminder
        /// </summary>
        [RelayCommand]
        private async Task RestoreReminderAsync(Reminder reminder)
        {
            if (reminder != null)
            {
                await _reminderService.RestoreReminderAsync(reminder.Id);
                DeletedReminders.Remove(reminder);
            }
        }

        /// <summary>
        /// Command to permanently delete a note
        /// </summary>
        [RelayCommand]
        private async Task PermanentlyDeleteNoteAsync(Note note)
        {
            if (note != null)
            {
                await _noteService.PermanentlyDeleteNoteAsync(note.Id);
                DeletedNotes.Remove(note);
            }
        }

        /// <summary>
        /// Command to permanently delete a reminder
        /// </summary>
        [RelayCommand]
        private async Task PermanentlyDeleteReminderAsync(Reminder reminder)
        {
            if (reminder != null)
            {
                await _reminderService.PermanentlyDeleteReminderAsync(reminder.Id);
                DeletedReminders.Remove(reminder);
            }
        }

        /// <summary>
        /// Restore a deleted task list
        /// </summary>
        [RelayCommand]
        private async Task RestoreTaskListAsync(TaskList taskList)
        {
            if (taskList != null)
            {
                await _taskListService.RestoreTaskListAsync(taskList.Id);
                DeletedTaskLists.Remove(taskList);
            }
        }

        /// <summary>
        /// Permanently delete a task list
        /// </summary>
        [RelayCommand]
        private async Task PermanentlyDeleteTaskListAsync(TaskList taskList)
        {
            if (taskList != null)
            {
                await _taskListService.PermanentlyDeleteTaskListAsync(taskList.Id);
                DeletedTaskLists.Remove(taskList);
            }
        }

        /// <summary>
        /// Load deleted items from services
        /// </summary>
        private async Task LoadDeletedNotes()
        {
            IsLoading = true;
            try
            {
                var notes = string.IsNullOrWhiteSpace(NotesSearchTerm)
                    ? await _noteService.GetDeletedNotesAsync()
                    : await _noteService.SearchDeletedNotesAsync(NotesSearchTerm);

                DeletedNotes.Clear();
                foreach (var note in notes)
                {
                    DeletedNotes.Add(note);
                }
            }
            finally
            {
                IsLoading = false;
            }
        }
        /// <summary>
        /// Load deleted items from services
        /// </summary>
        private async Task LoadDeletedReminders()
        {
            IsLoading = true;
            try
            {
                var reminders = string.IsNullOrWhiteSpace(RemindersSearchTerm)
                     ? await _reminderService.GetDeletedRemindersAsync()
                     : await _reminderService.SearchDeletedRemindersAsync(RemindersSearchTerm);

                DeletedReminders.Clear();
                foreach (var reminder in reminders)
                {
                    DeletedReminders.Add(reminder);
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Load deleted task lists from the service
        /// </summary>
        private async Task LoadDeletedTaskLists()
        {
            IsLoading = true;
            try
            {
                var taskLists = string.IsNullOrWhiteSpace(TaskListsSearchTerm)
                    ? await _taskListService.GetDeletedTaskListsAsync()
                    : await _taskListService.SearchDeletedTaskListsAsync(TaskListsSearchTerm);

                DeletedTaskLists.Clear();
                foreach (var taskList in taskLists)
                {
                    DeletedTaskLists.Add(taskList);
                }
            }
            finally
            {
                IsLoading = false;
            }
        }
       
        public async override Task Initialize()
        {
            await LoadDeletedNotes();
            await LoadDeletedReminders();
            await LoadDeletedTaskLists();
        }
    }
}
