using Microsoft.UI.Xaml.Controls;
using Windows.Storage;
using MyNotes.Helpers;
using Windows.Foundation;
using CommunityToolkit.WinUI.UI;
using MyNotes.Views;
using Microsoft.UI.Xaml;

namespace MyNotes.Views;
public enum ReminderCreateResult
{
    ReminderCreationOK,
    ReminderCreationFail,
    ReminderCreationCancel,
    Nothing
}
public sealed partial class CreateReminderDialog : ContentDialog
{
    public ReminderCreateResult Result
    {
        get; private set;
    }
    readonly bool isNewNote;
    private bool isRepeated = false;
    public CreateReminderDialog()
    {
        this.InitializeComponent();
        ReminderRepeatCheck.Content = "Repeated".GetLocalized();
        ReminderRepeatCheck.IsChecked = false;
        datePicker.SelectedDate = DateTime.Now;
        datePicker.MinYear = DateTimeOffset.Now;
        datePicker.MaxYear = DateTimeOffset.Now.AddYears(3);
        timePicker.SelectedTime = new TimeSpan(DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);
        isNewNote = RemindersPage.isNewNote;
    }

    private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        if (isNewNote)
        {
            if (reminderNameTextBox != null && reminderTextTextBox != null)
            {

            }
        }
    }

    private void ContentDialog_CloseButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        Result = ReminderCreateResult.ReminderCreationCancel;
    }

    private void ReminderRepeatCheck_Checked(object sender, RoutedEventArgs e)
    {
        datePicker.Visibility= Visibility.Collapsed;
        isRepeated = true;
    }

    private void ReminderRepeatCheck_Unchecked(object sender, RoutedEventArgs e)
    {
        datePicker.Visibility = Visibility.Visible;
        isRepeated = false;
    }
}
