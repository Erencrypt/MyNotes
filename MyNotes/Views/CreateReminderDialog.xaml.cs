using Microsoft.UI.Xaml.Controls;
using Windows.Storage;
using MyNotes.Helpers;
using Microsoft.UI.Xaml;
using System.Text.RegularExpressions;
using MyNotes.Models;
using System.Text;

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
    readonly private bool isNewNote;
    private string noteName;
    private bool isRepeated = false;
    TimeSpan tmsp;
    DateTime time, selectedDate, ofsetDate;
    public CreateReminderDialog()
    {
        //TODO: localize strings
        //TODO simplify and clean code blocks
        this.InitializeComponent();
        ReminderRepeatCheck.Content = "Repeated".GetLocalized();
        isNewNote = RemindersPage.IsNewNote;
        noteName = RemindersPage.NoteName;
        if (!isNewNote)
        {
            reminderNameTextBox.IsEnabled = false;
            DirectoryInfo dinfo = new(storageFolder.Path.ToString() + "\\Reminders");
            FileInfo fileInfo = new(noteName + ".txt");
            string fullPath = dinfo.ToString() + "\\" + fileInfo;
            string readText = File.ReadAllText(fullPath, Encoding.UTF8);
            string[] lines = readText.Split("\r\n");
            DateTime t;
            if (lines.Length == 3)
            {
                t = Convert.ToDateTime(lines[2]);
                reminderNameTextBox.Text = fileInfo.Name[..^4];
                reminderTextTextBox.Text = lines[1];
                ReminderRepeatCheck.IsChecked = Convert.ToBoolean(lines[0]);
                timePicker.SelectedTime = t.TimeOfDay;
            }
            else if (lines.Length == 4)
            {
                t = Convert.ToDateTime(lines[3] + " " + lines[2]);
                reminderNameTextBox.Text = fileInfo.Name[..^4];
                reminderTextTextBox.Text = lines[1];
                ReminderRepeatCheck.IsChecked = Convert.ToBoolean(lines[0]);
                timePicker.SelectedTime = t.TimeOfDay;
                datePicker.SelectedDate = t.Date;
            }
        }
        else if (isNewNote)
        {
            ReminderRepeatCheck.IsChecked = false;
            timePicker.SelectedTime = new TimeSpan(DateTime.Now.Hour, DateTime.Now.Minute, 0);
        }
        datePicker.SelectedDate = DateTime.Now;
        datePicker.MinYear = DateTimeOffset.Now;
        datePicker.MaxYear = DateTimeOffset.Now.AddYears(3);
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
                Regex regex = MyRegex();
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
    private void EditReminder(ContentDialogButtonClickEventArgs args)
    {
        try
        {
            var directory = storageFolder.Path.ToString() + @"\Reminders\";
            var filelocation = directory + reminderNameTextBox.Text + ".txt";
            using StreamWriter writer = new(filelocation);
            selectedDate = datePicker.SelectedDate!.Value.DateTime;
            writer.WriteLine(isRepeated.ToString());
            writer.WriteLine(reminderTextTextBox.Text);
            Regex regex = MyRegex();
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
                    ofsetDate = DateTime.Now.AddHours(1);
                    if (isRepeated)
                    {
                        CreateReminder(args);
                    }
                    else if (datePicker.SelectedDate!.Value.Date < DateTime.Now.Date)
                    {
                        args.Cancel = true;
                        errorTextBlock.Visibility = Visibility.Visible;
                        errorTextBlock.Text = "Please select a date that later \nthan (or equal to) the current date.";
                    }
                    else if (datePicker.SelectedDate!.Value.Date == DateTime.Now.Date && time.Hour < ofsetDate.Hour)
                    {
                        args.Cancel = true;
                        errorTextBlock.Visibility = Visibility.Visible;
                        errorTextBlock.Text = "Please select a time that at least 1 hour later \nthan the current time.";
                    }
                    else if ((datePicker.SelectedDate.Value.Date == DateTime.Now.Date && time.Hour >= ofsetDate.Hour) || (datePicker.SelectedDate.Value.Date > DateTime.Now.Date))
                    {
                        CreateReminder(args);
                    }
                    else
                    {
                        args.Cancel = true;
                        throw new Exception();
                    }
                }
            }
            else
            {
                if (string.IsNullOrEmpty(reminderNameTextBox.Text) || string.IsNullOrEmpty(reminderTextTextBox.Text))
                {
                    args.Cancel = true;
                    errorTextBlock.Visibility = Visibility.Visible;
                    errorTextBlock.Text = "Reminder name and text is required.";
                }
                else if (!string.IsNullOrEmpty(reminderNameTextBox.Text) && !string.IsNullOrEmpty(reminderTextTextBox.Text))
                {
                    EditReminder(args);
                }
            }
        }
        catch (Exception ex)
        {
            errorTextBlock.Visibility = Visibility.Visible;
            errorTextBlock.Text = "An error occured. Error message:" + ex.Message;
        }
    }

    private void ContentDialog_CloseButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        Result = ReminderCreateResult.ReminderCreationCancel;
    }

    private void ReminderRepeatCheck_Checked(object sender, RoutedEventArgs e)
    {
        datePicker.Visibility = Visibility.Collapsed;
        isRepeated = true;
    }

    private void ReminderRepeatCheck_Unchecked(object sender, RoutedEventArgs e)
    {
        datePicker.Visibility = Visibility.Visible;
        isRepeated = false;
        if (datePicker.SelectedDate==null)
        {
            datePicker.SelectedDate = DateTime.Now;
        }
    }

    [GeneratedRegex("\\s")]
    private static partial Regex MyRegex();
}
