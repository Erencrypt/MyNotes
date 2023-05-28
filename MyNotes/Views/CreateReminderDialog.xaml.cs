using Microsoft.UI.Xaml.Controls;
using Windows.Storage;
using MyNotes.Helpers;
using Windows.Foundation;
using CommunityToolkit.WinUI.UI;

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
    public CreateReminderDialog()
    {
        this.InitializeComponent();
        datePicker.SelectedDate = DateTime.Now;
        datePicker.MinYear=DateTimeOffset.Now;
        datePicker.MaxYear=DateTimeOffset.Now.AddYears(3);
    }

    private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {

    }

    private void ContentDialog_CloseButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        Result = ReminderCreateResult.ReminderCreationCancel;
    }
}
