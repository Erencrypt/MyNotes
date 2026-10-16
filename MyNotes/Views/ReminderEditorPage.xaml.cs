using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using MyNotes.Helpers;
using MyNotes.ViewModels;
using WinUI3Localizer;

namespace MyNotes.Views
{
    /// <summary>
    /// Reminder editor page for adding/editing reminders
    /// </summary>
    public sealed partial class ReminderEditorPage : Page
    {
        private readonly ILocalizer _Localizer = Localizer.Get();
        private readonly string CurrentLanguage;
        public ReminderEditorViewModel ViewModel { get; }

        public ReminderEditorPage()
        {
            InitializeComponent();
            ViewModel = App.GetService<ReminderEditorViewModel>();
            DataContext = ViewModel;
            DatePicker.MinDate = DateTimeOffset.Now;
            _ = ViewModel.Initialize();
            TimePicker.ClockIdentifier = ViewModel.HourFormat;

            CurrentLanguage = _Localizer.GetCurrentLanguage();
            TimePicker.Language = CurrentLanguage;
            DatePicker.Language = CurrentLanguage;

            HeaderTextBox.PlaceholderText = _Localizer.GetLocalizedString("ReminderEditor_HeaderTextBoxPlaceholder");
            TextTextBox.PlaceholderText = _Localizer.GetLocalizedString("ReminderEditor_TextTextBoxPlaceholder");
            NoteComboBox.PlaceholderText = _Localizer.GetLocalizedString("ReminderEditor_NoteComboBoxPlaceholder");
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is Data.Models.Reminder reminder)
            {
                // Load existing reminder for editing
                await ViewModel.LoadReminderAsync(reminder.Id);
            }
            if (ViewModel.ReminderId == Guid.Empty)
            {
                DeleteButton.Visibility = Visibility.Collapsed;
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var header = HeaderTextBox.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(header))
            {
                AppInfoBar.Show("Validation Error", "Header is required.", AlertInfoBarSeverity.Warning);
                HeaderTextBox.Focus(FocusState.Programmatic);
                return;
            }
            var text = TextTextBox.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(text))
            {
                AppInfoBar.Show("Validation Error", "Text is required.", AlertInfoBarSeverity.Warning);
                TextTextBox.Focus(FocusState.Programmatic);
                return;
            }
            await ViewModel.SaveReminderAsync();
            // Navigate back to reminders list
            NavigationHelper.GoBack();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // Navigate back to reminders list
            NavigationHelper.GoBack();
        }

        private void ClearNoteSelection_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.SelectedNote = null;
        }
        private async void DeleteButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            DeleteFlyout.Hide();
            if (ViewModel.ReminderId != Guid.Empty)
            {
                await ViewModel.DeleteReminder(ViewModel.ReminderId);
            }
        }
    }
}
