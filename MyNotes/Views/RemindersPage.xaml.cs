using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using MyNotes.Helpers;
using MyNotes.Models;
using MyNotes.ViewModels;
using System.Collections.ObjectModel;
using System.Text;
using Windows.Storage;

namespace MyNotes.Views;

public sealed partial class RemindersPage : Page
{
    private readonly StorageFolder storageFolder = App.StorageFolder;
    private readonly ObservableCollection<Reminder> items = new();
    readonly ReminderCleanup reminderCleanup = new();
    public RemindersViewModel ViewModel
    {
        get;
    }
    public static bool IsNewReminder
    {
        get
        {
            return isNewReminder;
        }
        set
        {
            isNewReminder = value;
        }
    }
    public static string ReminderName
    {
        get
        {
            return reminderName;
        }
        set
        {
            reminderName = value;
        }
    }
    private static bool isNewReminder = false;
    private static string reminderName = string.Empty;
    public RemindersPage()
    {
        //TODO:Add ReminderDetails page and add function to app notification for opening reminder details
        ViewModel = App.GetService<RemindersViewModel>();
        InitializeComponent();
        deleteFlyout.Text = "DeleteFlyout".GetLocalized();
        deleteReminderFly.Content = "DeleteConfirm".GetLocalized();
        EmptyText.Text = "Reminders_EmptyText".GetLocalized();
        ToolTipService.SetToolTip(deleteReminder, "Delete".GetLocalized());
        ToolTipService.SetToolTip(newReminder, "Add".GetLocalized());
        LstReminders.ItemsSource = items;
        ListReminders();
        if (LstReminders.Items.Count < 1)
        {
            EmptyText.Visibility = Visibility.Visible;
        }
        else
        {
            EmptyText.Visibility = Visibility.Collapsed;
        }
    }
    private async void AddReminder()
    {
        IsNewReminder = true;
        CreateReminderDialog AddReminderDialog = new()
        {
            XamlRoot = XamlRoot
        };
        await AddReminderDialog.ShowAsync();
        if (AddReminderDialog.Result == ReminderCreateResult.ReminderCreationOK)
        {
            items.Insert(0, AddReminderDialog.rmnd);
            if (Convert.ToDateTime(AddReminderDialog.rmnd.DateTime) > DateTime.Now)
            {
                reminderCleanup.Clean(false);
            }
            if (EmptyText.Visibility == Visibility.Visible)
            {
                EmptyText.Visibility = Visibility.Collapsed;
            }
        }
    }
    private async void EditReminder()
    {
        if (LstReminders.SelectedItem is Reminder selectedItem)
        {
            int index = LstReminders.SelectedIndex;
            IsNewReminder = false;
            reminderName = selectedItem.ReminderHeader!;
            CreateReminderDialog EditReminderDialog = new()
            {
                XamlRoot = XamlRoot,
                Title = "Reminders_EditReminder".GetLocalized(),
                PrimaryButtonText = "Reminders_SaveReminder".GetLocalized()
            };
            await EditReminderDialog.ShowAsync();
            if (EditReminderDialog.Result == ReminderCreateResult.ReminderCreationOK)
            {
                Reminder rm = EditReminderDialog.rmnd;
                items.Remove(selectedItem);
                items.Insert(index, rm);
                DateTime rmndDate = Convert.ToDateTime(EditReminderDialog.rmnd.DateTime);
                if (rmndDate > DateTime.Now && rmndDate < DateTime.Now.AddDays(1))
                {
                    reminderCleanup.Clean(false);
                }
            }
        }
        else
        {
            ContentDialog noWifiDialog = new()
            { XamlRoot = XamlRoot, Title = "Info".GetLocalized(), Content = "NoSelection".GetLocalized(), CloseButtonText = "Ok".GetLocalized() };
            await noWifiDialog.ShowAsync();
        }
    }
    private void ListReminders()
    {
        DirectoryInfo dinfo = new(storageFolder.Path + "\\Reminders");
        FileInfo[] Files = dinfo.GetFiles("*.txt");
        List<FileInfo> orderedList = Files.OrderByDescending(x => x.CreationTime).ToList();
        string fullPath;
        items.Clear();
        foreach (FileInfo file in orderedList)
        {
            fullPath = dinfo.ToString() + "\\" + file.Name;
            string readText = File.ReadAllText(fullPath, Encoding.UTF8);
            string[] lines = readText.Split("\r\n");
            DateTime t;
            if (lines.Length == 3)
            {
                t = Convert.ToDateTime(lines[2]);
                items.Add(new Reminder() { ReminderHeader = file.Name[..^4], ReminderText = lines[1], DateTime = t.ToString("hh:mm tt"), Repeat = lines[0] });
            }
            else if (lines.Length == 4)
            {
                t = Convert.ToDateTime(lines[3] + " " + lines[2]);
                items.Add(new Reminder() { ReminderHeader = file.Name[..^4], ReminderText = lines[1], DateTime = t.ToString("dd/MM/yyyy hh:mm tt"), Repeat = lines[0] });
            }
        }
        LstReminders.ItemsSource = items;
        LstReminders.UpdateLayout();
    }
    private void LstReminders_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        EditReminder();
    }
    private void NewReminder_Click(object sender, RoutedEventArgs e)
    {
        AddReminder();
    }
    private void DeleteReminder_Click(object sender, RoutedEventArgs e)
    {
        deleteReminder.Flyout.Hide();
        if (LstReminders.SelectedItem != null)
        {
            Reminder? rm = LstReminders.SelectedItem as Reminder;
            MoveFile moveFile = new();
            moveFile.Move("Reminders", "Trash", LstReminders, XamlRoot, rm!, items);
            if (App.Reminders.Contains(rm!))
            {
                App.Reminders.Remove(rm!);
            }
        }
    }
    private void LstReminders_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!deleteReminder.IsEnabled)
        {
            deleteReminder.IsEnabled = true;
        }
    }
}
