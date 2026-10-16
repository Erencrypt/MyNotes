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
    /// Reminders list page
    /// </summary>
    public sealed partial class RemindersListPage : Page
    {
        private readonly ILocalizer _localizer = Localizer.Get();
        public RemindersListViewModel ViewModel { get; set; }
        public RemindersListPage()
        {
            InitializeComponent();
            ViewModel = App.GetService<RemindersListViewModel>();
            DataContext = ViewModel;

            RemindersSearch.PlaceholderText = _localizer.GetLocalizedString("Search");
            ToolTipService.SetToolTip(deleteReminder, _localizer.GetLocalizedString("Delete"));
            ToolTipService.SetToolTip(newReminder, _localizer.GetLocalizedString("Add"));
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            await LoadReminders();
        }

        private async Task LoadReminders()
        {
            try
            {
                await ViewModel.Initialize();
                UpdateEmptyState();
            }
            catch (Exception ex)
            {
                AppInfoBar.Show("Error", $"An error occurred while loading the reminders: {ex.Message}", AlertInfoBarSeverity.Error);
                Debug.WriteLine($"Error loading reminders: {ex.Message}");
            }
        }
        private void UpdateEmptyState()
        {
            EmptyText.Text = _localizer.GetLocalizedString("Reminders_EmptyText");
            EmptyText.Visibility = ViewModel.Reminders.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        private async void RemindersSearch_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                // Filter reminders based on search text
                var searchText = sender.Text;
                ViewModel.SearchTerm = searchText;
                await ViewModel.SearchRemindersCommand.ExecuteAsync(null);
            }
        }

        private void LstReminders_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Enable/disable delete button based on selection
            deleteReminder.IsEnabled = LstReminders.SelectedItem != null;
        }

        private async void LstReminders_DoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
        {
            if (LstReminders.SelectedItem != null)
            {
                // Navigate to reminder editor with selected reminder
                await ViewModel.EditReminderCommand.ExecuteAsync(LstReminders.SelectedItem);
            }
        }

        private async void NewReminder_Click(object sender, RoutedEventArgs e)
        {
            // Navigate to reminder editor for new reminder
            await ViewModel.CreateReminderCommand.ExecuteAsync(null);
        }

        private async void DeleteReminder_Click(object sender, RoutedEventArgs e)
        {
            if (LstReminders.SelectedItem is Data.Models.Reminder selectedReminder)
            {
                try
                {
                    await ViewModel.DeleteReminderCommand.ExecuteAsync(selectedReminder);
                    UpdateEmptyState();
                }
                catch (Exception ex)
                {
                    AppInfoBar.Show("Error", $"An error occurred while deleting the reminder: {ex.Message}", AlertInfoBarSeverity.Error);
                    Debug.WriteLine($"Error deleting reminder: {ex.Message}");
                }
            }
            DeleteFlyout.Hide();
        }
    }
}
