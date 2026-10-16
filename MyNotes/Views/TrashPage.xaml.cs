using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using MyNotes.ViewModels;
using System.Diagnostics;
using WinUI3Localizer;

namespace MyNotes.Views
{
    /// <summary>
    /// Trash page
    /// </summary>
    public sealed partial class TrashPage : Page
    {
        public TrashViewModel ViewModel { get; set; }
        private readonly ILocalizer _localizer = Localizer.Get();
        public static bool? NtfInvoke
        {
            get; set;
        }
        public TrashPage()
        {
            InitializeComponent();
            ViewModel = App.GetService<TrashViewModel>();
            DataContext = ViewModel;
            ViewModel.DeletedTaskLists.CollectionChanged += (_, _) => UpdateEmptyStates();

            NotesSearch.PlaceholderText = _localizer.GetLocalizedString("Search");
            RemindersSearch.PlaceholderText = _localizer.GetLocalizedString("Search");
            TaskListSearch.PlaceholderText = _localizer.GetLocalizedString("Search");
            ToolTipService.SetToolTip(DeleteNote, _localizer.GetLocalizedString("Delete"));
            ToolTipService.SetToolTip(RestoreNote, _localizer.GetLocalizedString("Restore"));
            ToolTipService.SetToolTip(DeleteReminder, _localizer.GetLocalizedString("Delete"));
            ToolTipService.SetToolTip(RestoreReminder, _localizer.GetLocalizedString("Restore"));
            ToolTipService.SetToolTip(DeleteTaskList, _localizer.GetLocalizedString("Delete"));
            ToolTipService.SetToolTip(RestoreTaskList, _localizer.GetLocalizedString("Restore"));

            EmptyText.Visibility = LstNotes.Items.Count < 1 ? Visibility.Visible : Visibility.Collapsed;
            EmptyText2.Visibility = LstReminders.Items.Count < 1 ? Visibility.Visible : Visibility.Collapsed;
            EmptyText3.Visibility = LstTaskLists.Items.Count < 1 ? Visibility.Visible : Visibility.Collapsed;

            if (NtfInvoke == true)
            {
                PivotChange();
            }
        }
        private void PivotChange()
        {
            TrashPivot.SelectedItem = TrashPivot.Items[1];
            TrashPivot.UpdateLayout();
        }
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            await ViewModel.Initialize();
            UpdateEmptyStates();
        }

        private void UpdateEmptyStates()
        {
            // Update notes empty state
            EmptyText.Text = _localizer.GetLocalizedString("Trash_TrashEmpty");
            EmptyText.Visibility = ViewModel.DeletedNotes.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            // Update reminders empty state
            EmptyText2.Text = _localizer.GetLocalizedString("Trash_TrashEmpty");
            EmptyText2.Visibility = ViewModel.DeletedReminders.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            // Update task list empty state
            EmptyText3.Text = _localizer.GetLocalizedString("Trash_TrashEmpty");
            EmptyText3.Visibility = ViewModel.DeletedTaskLists.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private async void NotesSearch_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                // Filter notes based on search text
                var searchText = sender.Text;
                ViewModel.NotesSearchTerm = searchText;
                await ViewModel.SearchDeletedNotesCommand.ExecuteAsync(null);
            }
        }

        private async void RemindersSearch_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                // Filter deleted reminders based on search text
                var searchText = sender.Text;
                ViewModel.RemindersSearchTerm = searchText;
                await ViewModel.SearchDeletedRemindersCommand.ExecuteAsync(null);
            }
        }

        private void LstNotes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Enable/disable buttons based on selection
            RestoreNote.IsEnabled = LstNotes.SelectedItem != null;
            DeleteNote.IsEnabled = LstNotes.SelectedItem != null;
        }

        private void LstReminders_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Enable/disable buttons based on selection
            RestoreReminder.IsEnabled = LstReminders.SelectedItem != null;
            DeleteReminder.IsEnabled = LstReminders.SelectedItem != null;
        }

        private async void RestoreNote_Click(object sender, RoutedEventArgs e)
        {
            if (LstNotes.SelectedItem is Data.Models.Note selectedNote)
            {
                try
                {
                    await ViewModel.RestoreNoteCommand.ExecuteAsync(selectedNote);
                    UpdateEmptyStates();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error restoring note: {ex.Message}");
                }
            }
        }

        private async void DeleteNote_Click(object sender, RoutedEventArgs e)
        {
            if (LstNotes.SelectedItem is Data.Models.Note selectedNote)
            {
                try
                {
                    await ViewModel.PermanentlyDeleteNoteCommand.ExecuteAsync(selectedNote);
                    UpdateEmptyStates();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error permanently deleting note: {ex.Message}");
                }
            }
        }

        private async void RestoreReminder_Click(object sender, RoutedEventArgs e)
        {
            if (LstReminders.SelectedItem is Data.Models.Reminder selectedReminder)
            {
                try
                {
                    await ViewModel.RestoreReminderCommand.ExecuteAsync(selectedReminder);
                    UpdateEmptyStates();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error restoring reminder: {ex.Message}");
                }
            }
        }

        private async void DeleteReminder_Click(object sender, RoutedEventArgs e)
        {
            if (LstReminders.SelectedItem is Data.Models.Reminder selectedReminder)
            {
                try
                {
                    await ViewModel.PermanentlyDeleteReminderCommand.ExecuteAsync(selectedReminder);
                    UpdateEmptyStates();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error permanently deleting reminder: {ex.Message}");
                }
            }
        }

        private void TaskListSearch_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                ViewModel.TaskListsSearchTerm = sender.Text;
            }
        }

        private void LstTaskLists_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RestoreTaskList.IsEnabled = LstTaskLists.SelectedItem != null;
            DeleteTaskList.IsEnabled = LstTaskLists.SelectedItem != null;
        }

        private async void RestoreTaskList_Click(object sender, RoutedEventArgs e)
        {
            if (LstTaskLists.SelectedItem is Data.Models.TaskList selectedTaskList)
            {
                try
                {
                    await ViewModel.RestoreTaskListCommand.ExecuteAsync(selectedTaskList);
                    UpdateEmptyStates();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error restoring task list: {ex.Message}");
                }
            }
        }

        private async void DeleteTaskList_Click(object sender, RoutedEventArgs e)
        {
            if (LstTaskLists.SelectedItem is Data.Models.TaskList selectedTaskList)
            {
                try
                {
                    await ViewModel.PermanentlyDeleteTaskListCommand.ExecuteAsync(selectedTaskList);
                    UpdateEmptyStates();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error permanently deleting task list: {ex.Message}");
                }
            }
        }
    }
}
