using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MyNotes.Helpers;
using MyNotes.Models;
using System.Text;
using Windows.Storage;

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
    private readonly StorageFolder storageFolder = App.StorageFolder;
    public ReminderCreateResult Result
    {
        get; private set;
    }
    readonly private bool isNewNote;
    private readonly string noteName;
    private bool isRepeated = false;
    TimeSpan tmsp;
    DateTime time, selectedDate, ofsetDate;
    public CreateReminderDialog()
    {
        //TODO: localize strings
        //TODO: simplify and clean code blocks
        //TODO: change reminder tex texbox to rich edit box
        InitializeComponent();
        reminderNameTextBox.Header = "CreateReminder_NameBoxHeader".GetLocalized();
        reminderTextTextBox.Header = "CreateReminder_TextBoxHeader".GetLocalized();
        ReminderRepeatCheck.Content = "CreateReminder_Repeated".GetLocalized();
        datePicker.Header = "CreateReminder_DateHeader".GetLocalized();
        timePicker.Header = "CreateReminder_TimeHeader".GetLocalized();
        Title = "CreateReminder_Title".GetLocalized();
        PrimaryButtonText = "CreateReminder_ButtonText".GetLocalized();
        CloseButtonText = "Cancel".GetLocalized();
        isNewNote = RemindersPage.IsNewNote;
        noteName = RemindersPage.NoteName;
        if (!isNewNote)
        {
            reminderNameTextBox.IsEnabled = false;
            DirectoryInfo dinfo = new(storageFolder.Path + "\\Reminders");
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
            var directory = storageFolder.Path + @"\Reminders\";
            var filelocation = directory + reminderNameTextBox.Text + ".txt";
            if (File.Exists(filelocation))
            {
                Error("CreateReminder_ErrorExistingFile", args);
            }
            else
            {
                using StreamWriter writer = new(filelocation);
                selectedDate = datePicker.SelectedDate!.Value.DateTime;
                writer.WriteLine(isRepeated.ToString());
                writer.WriteLine(reminderTextTextBox.Text);
                DateTime t;
                if (!isRepeated)
                {
                    t = Convert.ToDateTime(timePicker.SelectedTime.ToString());
                    writer.WriteLine(timePicker.SelectedTime);
                    writer.Write(selectedDate.Day + "/" + selectedDate.Month + "/" + selectedDate.Year);
                    rmnd = new Reminder { ReminderHeader = reminderNameTextBox.Text, ReminderText = reminderTextTextBox.Text, DateTime = t.ToString("dd/MM/yyyy hh:mm tt"), Repeat = isRepeated.ToString() };
                }
                else
                {
                    t = Convert.ToDateTime(timePicker.SelectedTime.ToString());
                    writer.Write(timePicker.SelectedTime);
                    rmnd = new Reminder { ReminderHeader = reminderNameTextBox.Text, ReminderText = reminderTextTextBox.Text, DateTime = t.ToString("hh:mm tt"), Repeat = isRepeated.ToString() };
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
            var directory = storageFolder.Path + @"\Reminders\";
            var filelocation = directory + reminderNameTextBox.Text + ".txt";
            using StreamWriter writer = new(filelocation);
            selectedDate = datePicker.SelectedDate!.Value.DateTime;
            writer.WriteLine(isRepeated.ToString());
            writer.WriteLine(reminderTextTextBox.Text);
            DateTime t;
            if (!isRepeated)
            {
                t = Convert.ToDateTime(timePicker.SelectedTime.ToString());
                rmnd = new Reminder { ReminderHeader = reminderNameTextBox.Text, ReminderText = reminderTextTextBox.Text, DateTime = t.ToString("dd/MM/yyyy hh:mm tt"), Repeat = isRepeated.ToString() };
                writer.WriteLine(timePicker.SelectedTime);
                writer.Write(selectedDate.Day + "/" + selectedDate.Month + "/" + selectedDate.Year);
            }
            else
            {
                t = Convert.ToDateTime(timePicker.SelectedTime.ToString());
                rmnd = new Reminder { ReminderHeader = reminderNameTextBox.Text, ReminderText = reminderTextTextBox.Text, DateTime = t.ToString("hh:mm tt"), Repeat = isRepeated.ToString() };
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
                    Error("CreateReminder_ErrorRequired", args);
                }
                else if (!string.IsNullOrEmpty(reminderNameTextBox.Text) && !string.IsNullOrEmpty(reminderTextTextBox.Text))
                {
                    tmsp = (TimeSpan)timePicker.SelectedTime!;
                    time = Convert.ToDateTime(tmsp.ToString());
                    ofsetDate = DateTime.Now.AddMinutes(5);
                    if (isRepeated)
                    {
                        CreateReminder(args);
                    }
                    else if (datePicker.SelectedDate!.Value.Date > DateTime.Now.Date)
                    {
                        CreateReminder(args);
                    }
                    else if (datePicker.SelectedDate!.Value.Date < DateTime.Now.Date)
                    {
                        Error("CreateReminder_ErrorLaterDate", args);
                    }
                    else if (time.Hour == 23 && ofsetDate.Hour == 0)
                    {
                        Error("CreateReminder_ErrorLaterTime", args);
                    }
                    else if (time.Hour < ofsetDate.Hour)
                    {
                        Error("CreateReminder_ErrorLaterTime", args);
                    }
                    else if (time.Minute < ofsetDate.Minute)
                    {
                        Error("CreateReminder_ErrorLaterTime", args);
                    }
                    else if (time.Minute >= ofsetDate.Minute)
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
                    Error("CreateReminder_ErrorRequired", args);
                }
                else if (!string.IsNullOrEmpty(reminderNameTextBox.Text) && !string.IsNullOrEmpty(reminderTextTextBox.Text))
                {
                    tmsp = (TimeSpan)timePicker.SelectedTime!;
                    time = Convert.ToDateTime(tmsp.ToString());
                    ofsetDate = DateTime.Now.AddMinutes(5);
                    if (isRepeated)
                    {
                        EditReminder(args);
                    }
                    else if (datePicker.SelectedDate!.Value.Date > DateTime.Now.Date)
                    {
                        EditReminder(args);
                    }
                    else if (datePicker.SelectedDate!.Value.Date < DateTime.Now.Date)
                    {
                        Error("CreateReminder_ErrorLaterDate", args);
                    }
                    else if (time.Hour == 23 && ofsetDate.Hour == 0)
                    {
                        Error("CreateReminder_ErrorLaterTime", args);
                    }
                    else if (time.Hour < ofsetDate.Hour)
                    {
                        Error("CreateReminder_ErrorLaterTime", args);
                    }
                    else if (time.Minute < ofsetDate.Minute)
                    {
                        Error("CreateReminder_ErrorLaterTime", args);
                    }
                    else if (time.Minute >= ofsetDate.Minute)
                    {
                        EditReminder(args);
                    }
                    else
                    {
                        args.Cancel = true;
                        throw new Exception();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            errorTextBlock.Visibility = Visibility.Visible;
            errorTextBlock.Text = "Error_Meesage".GetLocalized() + ex.Message;
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
        datePicker.SelectedDate = DateTime.Now;
    }
    private void Error(string errorText, ContentDialogButtonClickEventArgs args)
    {
        args.Cancel = true;
        errorTextBlock.Visibility = Visibility.Visible;
        errorTextBlock.Text = errorText.GetLocalized();
        return;
    }
}
