using CommunityToolkit.Mvvm.Input;
using MyNotes.Data.Models;
using MyNotes.Data.Services.Interfaces;
using MyNotes.Helpers;
using MyNotes.Views;
using System.Diagnostics;

namespace MyNotes.ViewModels
{
    /// <summary>
    /// ViewModel for the note editor page
    /// </summary>
    public partial class NoteEditorViewModel : BaseViewModel
    {
        private readonly INoteService _noteService;
        private readonly ISettingService _settingService;


        public NoteEditorViewModel(INoteService noteService, ISettingService settingService)
        {
            _noteService = noteService;
            _settingService = settingService;
        }

        // Note properties
        private Guid _noteId = Guid.Empty;
        private string _title = string.Empty;
        private string _content = string.Empty;
        private bool _hasReminders;

        public Guid NoteId
        {
            get => _noteId;
            set => SetProperty(ref _noteId, value);
        }

        // Note properties
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public string Content
        {
            get => _content;
            set => SetProperty(ref _content, value);
        }

        public bool HasReminders
        {
            get => _hasReminders;
            set => SetProperty(ref _hasReminders, value);
        }

        /// <summary>
        /// Command to save the note
        /// </summary>
        [RelayCommand]
        private async Task SaveNoteAsync()
        {
            try
            {
                IsLoading = true;

                Note note;
                if (NoteId == Guid.Empty) // New note
                {
                    note = new Note
                    {
                        Title = Title.Trim(),
                        Content = Content ?? string.Empty, // Store RTF content as-is
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };
                }
                else // Existing note
                {
                    note = (await _noteService.GetNoteByIdAsync(NoteId))!;
                    note.Title = Title.Trim();
                    note.Content = Content ?? string.Empty; // Store RTF content as-is
                    note.UpdatedAt = DateTime.Now;
                }

                // Save the note
                if (NoteId == Guid.Empty)
                {
                    await _noteService.CreateNoteAsync(note);
                }
                else
                {
                    await _noteService.UpdateNoteAsync(note);
                }
            }
            catch (Exception ex)
            {
                AppInfoBar.Show("Error", $"An error occurred while saving the note: {ex.Message}", AlertInfoBarSeverity.Error);
                System.Diagnostics.Debug.WriteLine($"Error saving note: {ex.Message}");
                throw; // Re-throw to let the UI handle it
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Load an existing note for editing
        /// </summary>
        public async Task LoadNoteAsync(Guid noteId)
        {
            try
            {
                IsLoading = true;
                var note = await _noteService.GetNoteByIdAsync(noteId);

                if (note != null)
                {
                    NoteId = note.Id;
                    Title = note.Title;
                    // Store the content as-is (could be RTF or plain text)
                    Content = note.Content ?? string.Empty;

                    if (note.HasReminders)
                    {
                        HasReminders = true;
                    }
                }
            }
            catch (Exception ex)
            {
                AppInfoBar.Show("Error", $"An error occurred while loading the note: {ex.Message}", AlertInfoBarSeverity.Error);
                Debug.WriteLine($"Error loading note: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }
        public async Task SaveSpellCheckChange(bool value) { await _settingService.SetSettingAsync(DataKeys.SpellCheckEnabled, value); }

        public async Task DeleteNote(Guid id)
        {
            await _noteService.SoftDeleteNoteAsync(id);
            NavigationHelper.NavigateTo<NotesListPage>();
        }
    }
}
