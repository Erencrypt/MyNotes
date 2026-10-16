using CommunityToolkit.Mvvm.Input;
using MyNotes.Data.Models;
using MyNotes.Data.Services.Interfaces;
using MyNotes.Helpers;
using MyNotes.Views;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace MyNotes.ViewModels
{
    /// <summary>
    /// ViewModel for the notes list page
    /// </summary>
    public partial class NotesListViewModel : BaseViewModel
    {
        private readonly INoteService _noteService;
        private ObservableCollection<Note> _notes = [];
        private string _searchTerm = string.Empty;
        private CancellationTokenSource? _searchCts;

        public NotesListViewModel(INoteService noteService)
        {
            _noteService = noteService;
        }

        /// <summary>
        /// Collection of notes to display
        /// </summary>
        public ObservableCollection<Note> Notes
        {
            get => _notes;
            set => SetProperty(ref _notes, value);
        }

        /// <summary>
        /// Whether notes have reminders (for UI binding)
        /// </summary>
        public bool HasReminders => Notes.Any(n => n.HasReminders);

        /// <summary>
        /// Search term for filtering notes
        /// </summary>
        public string SearchTerm
        {
            get => _searchTerm;
            set
            {
                SetProperty(ref _searchTerm, value);
                _ = SearchNotesAsync();
            }
        }

        /// <summary>
        /// Command to refresh the notes list
        /// </summary>
        [RelayCommand]
        private async Task RefreshAsync()
        {
            await LoadNotesAsync();
        }

        /// <summary>
        /// Command to search and filter notes, preserving order and minimising UI churn.
        /// Cancels any previous in-flight search so rapid keystrokes don't race.
        /// </summary>
        [RelayCommand]
        private async Task SearchNotesAsync()
        {
            // Cancel the previous search if it's still running
            _searchCts?.Cancel();
            _searchCts = new CancellationTokenSource();
            var ct = _searchCts.Token;

            try
            {
                IsLoading = true;

                var filteredNotes = await _noteService.SearchNotesAsync(SearchTerm, ct);

                ct.ThrowIfCancellationRequested();

                // O(1) lookups by Note.Id rather than O(n) list scans
                var filteredById = filteredNotes.ToDictionary(n => n.Id);
                var currentById = Notes.ToDictionary(n => n.Id);

                // Remove notes that are no longer in the results
                for (int i = Notes.Count - 1; i >= 0; i--)
                {
                    if (!filteredById.ContainsKey(Notes[i].Id))
                        Notes.RemoveAt(i);
                }

                // Insert or move each note so the collection matches filteredNotes exactly
                for (int i = 0; i < filteredNotes.Count; i++)
                {
                    var note = filteredNotes[i];
                    if (!currentById.ContainsKey(note.Id))
                    {
                        // Brand-new result — insert at the correct position
                        Notes.Insert(i, note);
                    }
                    else if (Notes.IndexOf(Notes.FirstOrDefault(n => n.Id == note.Id)!) != i)
                    {
                        // Already in the collection but in the wrong position — move it
                        var current = Notes.First(n => n.Id == note.Id);
                        Notes.Move(Notes.IndexOf(current), i);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // A newer search superseded this one — discard silently
            }
            catch (Exception ex)
            {
                AppInfoBar.Show("Error", $"An error occurred while searching notes: {ex.Message}", AlertInfoBarSeverity.Error);
                Debug.WriteLine($"Error searching notes: {ex.Message}");
            }
            finally
            {
                // Only clear the loading flag if this search wasn't cancelled
                if (!ct.IsCancellationRequested)
                    IsLoading = false;
            }
        }

        /// <summary>
        /// Command to create a new note
        /// </summary>
        [RelayCommand]
        private async Task CreateNoteAsync()
        {
            NavigationHelper.NavigateTo<NoteEditorPage>();
            await Task.CompletedTask;
        }

        /// <summary>
        /// Command to edit a note
        /// </summary>
        [RelayCommand]
        private async Task EditNoteAsync(object note)
        {
            if (note != null)
            {
                NavigationHelper.NavigateTo<NoteEditorPage>(note);
            }
            await Task.CompletedTask;
        }

        /// <summary>
        /// Command to delete a note
        /// </summary>
        [RelayCommand]
        private async Task DeleteNoteAsync(Note note)
        {
            if (note != null)
            {
                await _noteService.SoftDeleteNoteAsync(note.Id);
                Notes.Remove(note);
            }
        }

        /// <summary>
        /// Load notes from the service (full reload, used on init/refresh/delete)
        /// </summary>
        private async Task LoadNotesAsync()
        {
            IsLoading = true;
            try
            {
                var notes = string.IsNullOrWhiteSpace(SearchTerm)
                    ? await _noteService.GetActiveNotesAsync()
                    : await _noteService.SearchNotesAsync(SearchTerm);
                Notes.Clear();
                foreach (var note in notes)
                {
                    Notes.Add(note);
                }
            }
            catch (Exception ex)
            {
                AppInfoBar.Show("Error", $"An error occurred while loading the notes: {ex.Message}", AlertInfoBarSeverity.Error);
                Debug.WriteLine($"Error loading notes: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        public override async Task Initialize()
        {
            await LoadNotesAsync();
        }
    }
}