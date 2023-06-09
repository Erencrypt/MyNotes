using Microsoft.UI.Xaml.Controls;
using MyNotes.Helpers;
using MyNotes.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Windows.Storage;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.ObjectModel;

namespace MyNotes.Views;

public sealed partial class RemindersPage : Page
{
    private readonly StorageFolder storageFolder = ApplicationData.Current.LocalFolder;
    private ObservableCollection<Reminder> items = new();
    public RemindersViewModel ViewModel
    {
        get;
    }
    public static bool IsNewNote
    {
        get
        {
            return isNewNote;
        }
        set
        {
            isNewNote = value;
        }
    }
    private static bool isNewNote = false;
    public RemindersPage()
    {
        ViewModel = App.GetService<RemindersViewModel>();
        InitializeComponent();
        deleteFlyout.Text = "DeleteFlyout".GetLocalized();
        deleteReminderFly.Content = "DeleteReminder_Button".GetLocalized();
        ToolTipService.SetToolTip(deleteReminder, "DeleteReminder".GetLocalized());
        ToolTipService.SetToolTip(newReminder, "AddReminder".GetLocalized());
        LstReminders.ItemsSource = items;
        ListFiles();
    }
    public class Reminder
    {
        public string? ReminderHeader { get; set; }
        public string? ReminderText { get; set; }
        public string? DateTime { get; set; }
        public string? Repeat { get; set; }
    }
    private async void AddReminder()
    {
        IsNewNote = true;
        CreateReminderDialog AddReminderDialog = new()
        {
            XamlRoot = XamlRoot
        };
        await AddReminderDialog.ShowAsync();
        if (AddReminderDialog.Result==ReminderCreateResult.ReminderCreationOK)
        {
            items.Insert(0,AddReminderDialog.rmnd);
            
        }
    }
    private async void EditReminder()
    {
        IsNewNote = false;
        CreateReminderDialog EditReminderDialog = new()
        {
            XamlRoot = XamlRoot,
            Title = "Edit Reminder",
            PrimaryButtonText = "Save Reminder"
        };
        await EditReminderDialog.ShowAsync();
    }
    private void ListFiles()
    {
        DirectoryInfo dinfo = new(storageFolder.Path.ToString() + "\\Reminders");
        FileInfo[] Files = dinfo.GetFiles("*.txt");
        List<FileInfo> orderedList = Files.OrderByDescending(x => x.CreationTime).ToList();
        string fullPath;
        items.Clear();
        foreach (FileInfo file in orderedList)
        {
            fullPath = dinfo.ToString() +"\\"+ file.Name;
            string readText = File.ReadAllText(fullPath, Encoding.UTF8);
            string [] lines = readText.Split("\r\n");
            Regex regex = MyRegex();
            string[] s;
            DateTime t;
            string [] tt;
            if (lines.Length == 3)
            {
                t= Convert.ToDateTime(lines[2]);
                s = regex.Split(t.ToString());
                items.Add(new Reminder() { ReminderHeader = file.Name[..^4], ReminderText = lines[1], DateTime = s[1][..^3] + " " + s[2], Repeat = lines[0] });
            }
            else if (lines.Length == 4)
            {
                t = Convert.ToDateTime(lines[3] +" "+ lines[2]);
                tt = regex.Split(t.ToString());
                items.Add(new Reminder() { ReminderHeader = file.Name[..^4], ReminderText = lines[1], DateTime = tt[0] + " " + tt[1][..^3] + " " + tt[2], Repeat = lines[0] });
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
    }

    private void LstReminders_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!deleteReminder.IsEnabled)
        {
            deleteReminder.IsEnabled = true;
        }
    }

    [GeneratedRegex("\\s")]
    private static partial Regex MyRegex();
}
