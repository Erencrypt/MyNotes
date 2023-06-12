using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using MyNotes.Helpers;
using MyNotes.ViewModels;
using MyNotes.Models;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Text;
using Windows.Storage;
using static MyNotes.Views.RemindersPage;

namespace MyNotes.Views;

public sealed partial class TrashPage : Page
{
    private readonly StorageFolder storageFolder = ApplicationData.Current.LocalFolder;
    private ObservableCollection<Reminder> items = new();
    public TrashViewModel ViewModel
    {
        get;
    }
    public TrashPage()
    {
        ViewModel = App.GetService<TrashViewModel>();
        InitializeComponent();
        //Localizatinos
        deleteFlyoutNoteText.Text = "DeleteFlyout2".GetLocalized();
        deleteNoteFly.Content = "DeleteConfirm".GetLocalized();
        ToolTipService.SetToolTip(deleteNote, "Delete".GetLocalized());
        ToolTipService.SetToolTip(restoreNote, "Restore".GetLocalized());
        deleteFlyoutReminderText.Text = "DeleteFlyout2".GetLocalized();
        deleteReminderFly.Content = "DeleteConfirm".GetLocalized();
        ToolTipService.SetToolTip(deleteReminder, "Delete".GetLocalized());
        ToolTipService.SetToolTip(restoreReminder, "Restore".GetLocalized());

        LstReminders.ItemsSource = items;
        ListNotes();
        ListReminders();
        if (LstNotes.Items.Count < 1)
        {
            EmptyText.Visibility = Visibility.Visible;
        }
        else
        {
            EmptyText.Visibility = Visibility.Collapsed;
        }
        if (LstReminders.Items.Count < 1)
        {
            EmptyText.Visibility = Visibility.Visible;
        }
        else
        {
            EmptyText.Visibility = Visibility.Collapsed;
        }
    }
    private void ListNotes()
    {
        DirectoryInfo dinfo = new(storageFolder.Path.ToString() + "\\Trash");
        FileInfo[] Files = dinfo.GetFiles("*.rtf");
        List<FileInfo> orderedList = Files.OrderByDescending(x => x.CreationTime).ToList();
        LstNotes.Items.Clear();
        foreach (FileInfo file in orderedList)
        {
            LstNotes.Items.Add(file.Name[..^4]);
        }
    }
    private void ListReminders()
    {
        DirectoryInfo dinfo = new(storageFolder.Path.ToString() + "\\Trash");
        FileInfo[] Files = dinfo.GetFiles("*.txt");
        List<FileInfo> orderedList = Files.OrderByDescending(x => x.CreationTime).ToList();
        string fullPath;
        items.Clear();
        foreach (FileInfo file in orderedList)
        {
            fullPath = dinfo.ToString() + "\\" + file.Name;
            string readText = File.ReadAllText(fullPath, Encoding.UTF8);
            string[] lines = readText.Split("\r\n");
            Regex regex = MyRegex();
            string[] s;
            DateTime t;
            string[] tt;
            if (lines.Length == 3)
            {
                t = Convert.ToDateTime(lines[2]);
                s = regex.Split(t.ToString());
                items.Add(new Reminder() { ReminderHeader = file.Name[..^4], ReminderText = lines[1], DateTime = s[1][..^3] + " " + s[2], Repeat = lines[0] });
            }
            else if (lines.Length == 4)
            {
                t = Convert.ToDateTime(lines[3] + " " + lines[2]);
                tt = regex.Split(t.ToString());
                items.Add(new Reminder() { ReminderHeader = file.Name[..^4], ReminderText = lines[1], DateTime = tt[0] + " " + tt[1][..^3] + " " + tt[2], Repeat = lines[0] });
            }
        }
        LstReminders.ItemsSource = items;
        LstReminders.UpdateLayout();
    }
    private async void DeleteNote()
    {
        try
        {
            var selectedItem = LstNotes.SelectedItem;
            if (selectedItem != null)
            {
                var directory = storageFolder.Path.ToString() + @"\Trash\" + LstNotes.SelectedItem.ToString() + ".rtf";
                var file = await StorageFile.GetFileFromPathAsync(directory);
                await file.DeleteAsync();
                LstNotes.Items.Remove(selectedItem);
            }
            else
            {
                ContentDialog noWifiDialog = new()
                { XamlRoot = XamlRoot, Title = "Info".GetLocalized(), Content = "NoSelection".GetLocalized(), CloseButtonText = "Ok".GetLocalized() };
                await noWifiDialog.ShowAsync();
            }
        }
        catch (Exception ex)
        {
            ContentDialog noWifiDialog = new()
            {
                XamlRoot = XamlRoot,
                Title = "Error".GetLocalized(),
                Content = "Error_Meesage2".GetLocalized() + ex.Message,
                CloseButtonText = "Ok".GetLocalized()
            };
            await noWifiDialog.ShowAsync();
        }
    }
    private async void DeleteReminder()
    {
        try
        {
            Reminder? selectedItem = LstReminders.SelectedItem as Reminder;
            if (selectedItem != null)
            {
                var directory = storageFolder.Path.ToString() + @"\Trash\" + selectedItem.ReminderHeader + ".txt";
                var file = await StorageFile.GetFileFromPathAsync(directory);
                await file.DeleteAsync();
                items.Remove(selectedItem);
            }
            else
            {
                ContentDialog noWifiDialog = new()
                { XamlRoot = XamlRoot, Title = "Info".GetLocalized(), Content = "NoSelection".GetLocalized(), CloseButtonText = "Ok".GetLocalized() };
                await noWifiDialog.ShowAsync();
            }
        }
        catch (Exception ex)
        {
            ContentDialog noWifiDialog = new()
            {
                XamlRoot = XamlRoot,
                Title = "Error".GetLocalized(),
                Content = "Error_Meesage2".GetLocalized() + ex.Message,
                CloseButtonText = "Ok".GetLocalized()
            };
            await noWifiDialog.ShowAsync();
        }
    }
    private void LstNotes_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!deleteNote.IsEnabled)
        {
            deleteNote.IsEnabled = true;
        }
    }
    private void RestoreNote_Click(object sender, RoutedEventArgs e)
    {
        if (LstNotes.Items.Count <= 1)
        {
            EmptyText.Visibility = Visibility.Visible;
        }
        MoveFile moveFile = new();
        moveFile.Move("Trash", "Notes", LstNotes, XamlRoot);
    }
    private void DeleteNote_Click(object sender, RoutedEventArgs e)
    {
        if (LstNotes.Items.Count <= 1)
        {
            EmptyText.Visibility = Visibility.Visible;
        }
        DeleteNote();
        deleteNote.Flyout.Hide();
    }
    private void LstReminders_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!deleteNote.IsEnabled)
        {
            deleteReminder.IsEnabled = true;
        }
    }
    private void RestoreReminder_Click(object sender, RoutedEventArgs e)
    {
        if (LstReminders.Items.Count <= 1)
        {
            EmptyText2.Visibility = Visibility.Visible;
        }
        if (LstReminders.SelectedItem != null)
        {
            Reminder? rm = LstReminders.SelectedItem as Reminder;
            MoveFile moveFile = new();
            moveFile.Move("Trash", "Reminders", LstReminders, XamlRoot, rm!, items);
        }
        
    }
    private void DeleteReminder_Click(object sender, RoutedEventArgs e)
    {
        if (LstReminders.Items.Count <= 1)
        {
            EmptyText2.Visibility = Visibility.Visible;
        }
        DeleteReminder();
        deleteReminder.Flyout.Hide();
    }
    [GeneratedRegex("\\s")]
    private static partial Regex MyRegex();
}
