using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using MyNotes.Helpers;
using MyNotes.ViewModels;
using System.Diagnostics;
using WinUI3Localizer;

namespace MyNotes.Views
{
    /// <summary>
    /// Notes list page
    /// </summary>
    public sealed partial class NotesListPage : Page
    {
        private ILocalizer _localizer = Localizer.Get();
        public NotesListViewModel ViewModel { get; set; }

        public NotesListPage()
        {
            InitializeComponent();
            ViewModel = App.GetService<NotesListViewModel>();
            DataContext = ViewModel;

            EmptyText.Visibility = LstNotes.Items.Count < 1 ? Visibility.Visible : Visibility.Collapsed;
            NotesSearch.PlaceholderText = _localizer.GetLocalizedString("Search");
            ToolTipService.SetToolTip(deleteNote, _localizer.GetLocalizedString("Delete"));
            ToolTipService.SetToolTip(newNote, _localizer.GetLocalizedString("Add"));
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            await LoadNotes();
        }

        private async Task LoadNotes()
        {
            try
            {
                await ViewModel.Initialize();
                UpdateEmptyState();
            }
            catch (Exception ex)
            {
                AppInfoBar.Show("Error", $"An error occurred while loading the notes: {ex.Message}", AlertInfoBarSeverity.Error);
                Debug.WriteLine($"Error loading notes: {ex.Message}");
            }
        }
        private void UpdateEmptyState()
        {
            EmptyText.Text = _localizer.GetLocalizedString("Notes_EmptyText");
            EmptyText.Visibility = ViewModel.Notes.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private async void NotesSearch_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                // Filter notes based on search text
                var searchText = sender.Text;
                ViewModel.SearchTerm = searchText;
                await ViewModel.SearchNotesCommand.ExecuteAsync(null);
            }
        }

        private void LstNotes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Enable/disable delete button based on selection
            deleteNote.IsEnabled = LstNotes.SelectedItem != null;
        }

        private async void LstNotes_DoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
        {
            if (LstNotes.SelectedItem != null)
            {
                // Navigate to note editor with selected note
                await ViewModel.EditNoteCommand.ExecuteAsync(LstNotes.SelectedItem);
            }
        }

        private async void NewNote_Click(object sender, RoutedEventArgs e)
        {
            // Navigate to note editor for new note
            await ViewModel.CreateNoteCommand.ExecuteAsync(null);
        }

        private async void DeleteNote_Click(object sender, RoutedEventArgs e)
        {
            if (LstNotes.SelectedItem is Data.Models.Note selectedNote)
            {
                try
                {
                    await ViewModel.DeleteNoteCommand.ExecuteAsync(selectedNote);
                    UpdateEmptyState();
                }
                catch (Exception ex)
                {
                    AppInfoBar.Show("Error", $"An error occurred while deleting the note: {ex.Message}", AlertInfoBarSeverity.Error);
                    Debug.WriteLine($"Error deleting note: {ex.Message}");
                }
            }
            DeleteFlyout.Hide();
        }
    }
}
