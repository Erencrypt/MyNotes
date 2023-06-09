using Microsoft.UI.Xaml.Controls;
using Windows.Storage;
using MyNotes.Helpers;
using Microsoft.UI.Xaml;
using System.Text.RegularExpressions;
using static MyNotes.Views.RemindersPage;

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
    public Reminder rmnd = new();
    private readonly StorageFolder storageFolder = ApplicationData.Current.LocalFolder;
    public ReminderCreateResult Result
    {
        get; private set;
    }
    readonly bool isNewNote;
    private bool isRepeated = false;
    TimeSpan tmsp;
    DateTime time, selectedDate,ofsetDate;
    public CreateReminderDialog()
    {
        //TODO: localize strings
        this.InitializeComponent();
        ReminderRepeatCheck.Content = "Repeated".GetLocalized();
        ReminderRepeatCheck.IsChecked = false;
        datePicker.SelectedDate = DateTime.Now;
        datePicker.MinYear = DateTimeOffset.Now;
        datePicker.MaxYear = DateTimeOffset.Now.AddYears(3);
        timePicker.SelectedTime = new TimeSpan(DateTime.Now.Hour, DateTime.Now.Minute, 0);
        isNewNote = RemindersPage.isNewNote;
    }
    private void CreateReminder(ContentDialogButtonClickEventArgs args)
    {
        try
        {
            var directory = storageFolder.Path.ToString() + @"\Reminders\";
            var filelocation = directory + reminderNameTextBox.Text + ".txt";
            if (File.Exists(filelocation))
            {
                args.Cancel = true;
                errorTextBlock.Visibility = Visibility.Visible;
                errorTextBlock.Text = "There is a reminder with the same name\nalready exist. Please use another name.";
            }
            else
            {
                using StreamWriter writer = new(filelocation);
                selectedDate = datePicker.SelectedDate!.Value.DateTime;
                writer.WriteLine(isRepeated.ToString());
                writer.WriteLine(reminderTextTextBox.Text);
                Regex regex = new(@"\s");
                string[] s;
                DateTime t;
                string[] tt;
                if (!isRepeated)
                {
                    t = Convert.ToDateTime(timePicker.SelectedTime.ToString());
                    tt = regex.Split(t.ToString());
                    rmnd = new Reminder { ReminderHeader = reminderNameTextBox.Text, ReminderText = reminderTextTextBox.Text, DateTime = tt[0] + " " + tt[1][..^3] + " " + tt[2], Repeat = isRepeated.ToString() };
                    writer.WriteLine(timePicker.SelectedTime);
                    writer.Write(selectedDate.Day + "/" + selectedDate.Month + "/" + selectedDate.Year);
                }
                else
                {
                    t = Convert.ToDateTime(timePicker.SelectedTime.ToString());
                    s = regex.Split(t.ToString());
                    rmnd = new Reminder { ReminderHeader = reminderNameTextBox.Text, ReminderText = reminderTextTextBox.Text, DateTime = s[1][..^3] + " " + s[2], Repeat = isRepeated.ToString() };
                    writer.Write(timePicker.SelectedTime);
                }
                writer.Close();
                Result = ReminderCreateResult.ReminderCreationOK;
            }
        }
        catch (Exception ex)
        {
            args.Cancel = true;
            errorTextBlock.Visibility = Visibility.Visible;
            errorTextBlock.Text = ex.Message;
            Result = ReminderCreateResult.ReminderCreationFail;
        }
    }
    private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        try
        {
            if (isNewNote)
            {
                if (string.IsNullOrEmpty(reminderNameTextBox.Text) || string.IsNullOrEmpty(reminderTextTextBox.Text))
                {
                    args.Cancel = true;
                    errorTextBlock.Visibility = Visibility.Visible;
                    errorTextBlock.Text = "Reminder name and text is required.";
                }
                else if (!string.IsNullOrEmpty(reminderNameTextBox.Text) && !string.IsNullOrEmpty(reminderTextTextBox.Text))
                {
                    tmsp = (TimeSpan)timePicker.SelectedTime!;
                    time = Convert.ToDateTime(tmsp.ToString());
                    ofsetDate=DateTime.Now.AddHours(1);
                    if (isRepeated)
                    {
                        CreateReminder(args);
                    }
                    else if (time.Hour < ofsetDate.Hour)
                    {
                        args.Cancel = true;
                        errorTextBlock.Visibility = Visibility.Visible;
                        errorTextBlock.Text = "Please select a time that at least 1 hour later \nthan the current time.";
                    }
                    else if (datePicker.SelectedDate!.Value.Date < DateTime.Now.Date)
                    {
                        args.Cancel = true;
                        errorTextBlock.Visibility = Visibility.Visible;
                        errorTextBlock.Text = "Please select a date that later \nthan (or equal to) the current date.";
                    }
                    else if (time.Hour >= ofsetDate.Hour && datePicker.SelectedDate.Value.Date >= DateTime.Now.Date)
                    {
                        CreateReminder(args);
                    }
                }
            }
            else
            {

            }
        }
        catch (Exception ex)
        {
            errorTextBlock.Visibility = Visibility.Visible;
            errorTextBlock.Text = "An error occured. Error message:"+ex.Message;
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
