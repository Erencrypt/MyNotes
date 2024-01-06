using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MyNotes.Helpers;
using MyNotes.Models;
using System.Text;
using System.Text.Json;
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
    readonly private bool isNewReminder;
    private readonly string reminderName;
    private bool isRepeated = false;
    private bool isAlarm =false;
    TimeSpan tmsp;
    DateTime time, selectedDate, ofsetDate;
    public CreateReminderDialog()
    {
        //TODO: localize strings
        InitializeComponent();
        reminderNameTextBox.Header = "CreateReminder_NameBoxHeader".GetLocalized();
        reminderTextTextBox.Header = "CreateReminder_TextBoxHeader".GetLocalized();
        ReminderAlarmTogleText.Text = "CreateReminder_Alarm".GetLocalized();
        ReminderRepeatTogleText.Text = "CreateReminder_Repeated".GetLocalized();
        datePicker.Header = "CreateReminder_DateHeader".GetLocalized();
        timePicker.Header = "CreateReminder_TimeHeader".GetLocalized();
        Title = "CreateReminder_Title".GetLocalized();
        PrimaryButtonText = "CreateReminder_ButtonText".GetLocalized();
        CloseButtonText = "Cancel".GetLocalized();
        isNewReminder = RemindersPage.IsNewReminder;
        reminderName = RemindersPage.ReminderName;
        if (!isNewReminder)
        {

            reminderNameTextBox.IsEnabled = false;
            DirectoryInfo dinfo = new(storageFolder.Path + "\\Reminders");
            FileInfo fileInfo = new(reminderName + ".json");
            string fullPath = dinfo.ToString() + "\\" + fileInfo;
            string readText = File.ReadAllText(fullPath, Encoding.UTF8);
            Reminder readedReminder = JsonSerializer.Deserialize<Reminder>(readText)!;
            DateTime t;

            t = Convert.ToDateTime(readedReminder.DateTime);
            reminderNameTextBox.Text = readedReminder.ReminderHeader;
            reminderTextTextBox.Text = readedReminder.ReminderText;
            ReminderRepeatTogle.IsOn = readedReminder.Repeat;
            ReminderAlarmTogle.IsOn = readedReminder.Alarm;
            timePicker.SelectedTime = t.TimeOfDay;
            datePicker.SelectedDate = t.Date;
        }
        else if (isNewReminder)
        {
            ReminderRepeatTogle.IsOn = false;
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
            var filelocation = directory + reminderNameTextBox.Text + ".json";
            if (File.Exists(filelocation))
            {
                Error("CreateReminder_ErrorExistingFile", args);
            }
            else
            {
                SaveReminder(filelocation);
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
            var filelocation = directory + reminderNameTextBox.Text + ".json";

            SaveReminder(filelocation);
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
            if (isNewReminder)
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

    private void ReminderAlarmTogle_Toggled(object sender, RoutedEventArgs e)
    {
        if (ReminderAlarmTogle.IsOn)
        {
            isAlarm = true;
        }
        else
        {
            isAlarm = false;
        }
    }

    private void ReminderRepeatTogle_Toggled(object sender, RoutedEventArgs e)
    {
        if (ReminderRepeatTogle.IsOn)
        {
            datePicker.Visibility = Visibility.Collapsed;
            isRepeated = true;
        }
        else
        {
            datePicker.Visibility = Visibility.Visible;
            isRepeated = false;
            datePicker.SelectedDate = DateTime.Now;
        }
    }

    private void Error(string errorText, ContentDialogButtonClickEventArgs args)
    {
        args.Cancel = true;
        errorTextBlock.Visibility = Visibility.Visible;
        errorTextBlock.Text = errorText.GetLocalized();
        return;
    }

    private void SaveReminder(string filelocation)
    {
        DateTime t;
        string time;
        string icon;
        selectedDate = datePicker.SelectedDate!.Value.DateTime;
        if (!isRepeated)
        {
            t = selectedDate.Date.Add((TimeSpan)timePicker.SelectedTime!);
            time = t.ToString("dd/MM/yyyy hh:mm tt");
            icon = "\uEC92";
        }
        else
        {
            t = Convert.ToDateTime(timePicker.SelectedTime.ToString());
            time = t.ToString("hh:mm tt");
            icon = "\uE823";
        }
        Reminder reminder = new()
        {
            ReminderIcon = icon,
            ReminderHeader = reminderNameTextBox.Text,
            ReminderText = reminderTextTextBox.Text,
            DateTime = time,
            Repeat = isRepeated,
            Alarm = isAlarm
        };
        string jsonString = JsonSerializer.Serialize(reminder);
        File.WriteAllText(filelocation, jsonString);
        rmnd = reminder;
    }
}
