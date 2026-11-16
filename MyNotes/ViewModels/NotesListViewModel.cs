using CommunityToolkit.Mvvm.Input;
using MyNotes.Data.Models;
using MyNotes.Data.Services.Interfaces;
using MyNotes.Helpers;
using MyNotes.Views;
using System.Collections.ObjectModel;
using System.Diagnostics;
using WinUI3Localizer;

namespace MyNotes.ViewModels
{
    /// <summary>
    /// ViewModel for the notes list page
    /// </summary>
    public partial class NotesListViewModel : BaseViewModel
    {
        private readonly ILocalizer _localizer = Localizer.Get();
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
            set => SetProperty(ref _searchTerm, value);
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

                Notes.SyncWith(filteredNotes, n => n.Id);
            }
            catch (OperationCanceledException)
            {
                // A newer search superseded this one — discard silently
            }
            catch (Exception ex)
            {
                AppInfoBar.Show(
                    _localizer.GetLocalizedString("Error"),
                    string.Format(_localizer.GetLocalizedString("NotesList_SearchErrorMessage"), ex.Message),
                    AlertInfoBarSeverity.Error);
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
                AppInfoBar.Show(
                    _localizer.GetLocalizedString("Error"),
                    string.Format(_localizer.GetLocalizedString("NotesList_LoadErrorMessage"), ex.Message),
                    AlertInfoBarSeverity.Error);
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