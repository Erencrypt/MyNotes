using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MyNotes.Helpers;
using MyNotes.Models;
using MyNotes.ViewModels;
using System.Collections.ObjectModel;
using System.Text;
using System.Text.Json;
using Windows.Storage;

namespace MyNotes.Views;

public sealed partial class TrashPage : Page
{
    private readonly StorageFolder storageFolder = App.StorageFolder;
    private readonly ObservableCollection<Reminder> items = new();
    public TrashViewModel ViewModel
    {
        get;
    }
    public static bool? NtfInvoke { get; set; }

    public TrashPage()
    {
        ViewModel = App.GetService<TrashViewModel>();
        InitializeComponent();
        PivotItem pivotItem;
        //Localizatinos
        deleteFlyoutNoteText.Text = "Trash_DeleteFlyout".GetLocalized();
        deleteNoteFly.Content = "DeleteConfirm".GetLocalized();
        EmptyText.Text = "Trash_TrashEmpty".GetLocalized();
        pivotItem = (PivotItem)TrashPivot.Items[0];
        pivotItem.Header = "Trash_NotePivotHeader".GetLocalized();
        ToolTipService.SetToolTip(deleteNote, "Delete".GetLocalized());
        ToolTipService.SetToolTip(restoreNote, "Restore".GetLocalized());
        deleteFlyoutReminderText.Text = "Trash_DeleteFlyout".GetLocalized();
        deleteReminderFly.Content = "DeleteConfirm".GetLocalized();
        EmptyText2.Text = "Trash_TrashEmpty".GetLocalized();
        pivotItem = (PivotItem)TrashPivot.Items[1];
        pivotItem.Header = "Trash_ReminderPivotHeader".GetLocalized();
        ToolTipService.SetToolTip(deleteReminder, "Delete".GetLocalized());
        ToolTipService.SetToolTip(restoreReminder, "Restore".GetLocalized());

        LstReminders.ItemsSource = items;
        ListNotes(string.Empty);
        ListReminders();
        EmptyText.Visibility = LstNotes.Items.Count < 1 ? Visibility.Visible : Visibility.Collapsed;
        EmptyText2.Visibility = LstReminders.Items.Count < 1 ? Visibility.Visible : Visibility.Collapsed;
        if (NtfInvoke == true)
        {
            PivotChange();
        }
    }
    private void PivotChange()
    {
        TrashPivot.SelectedItem = TrashPivot.Items[1];
        TrashPivot.UpdateLayout();
    }
    private void ListNotes(string SearchText)
    {
        DirectoryInfo dinfo = new(storageFolder.Path + "\\Notes");
        FileInfo[] Files = dinfo.GetFiles("*.rtf");
        List<FileInfo> orderedList = SearchText == null ? orderedList = Files.OrderByDescending(x => x.LastAccessTime).ToList() :
        orderedList = Files.Where(x => x.Name[..^4].ToLower().Contains(SearchText.ToLower())).OrderByDescending(x => x.LastAccessTime).ToList();
        LstNotes.Items.Clear();
        foreach (FileInfo file in orderedList)
        {
            LstNotes.Items.Add(file.Name[..^4]);
        }
    }
    private void ListReminders()
    {
        DirectoryInfo dinfo = new(storageFolder.Path + "\\Trash");
        FileInfo[] Files = dinfo.GetFiles("*.json");
        List<FileInfo> orderedList = Files.OrderByDescending(x => x.CreationTime).ToList();
        string fullPath;
        items.Clear();
        foreach (FileInfo file in orderedList)
        {
            fullPath = dinfo.ToString() + "\\" + file.Name;
            string readText = File.ReadAllText(fullPath, Encoding.UTF8);
            Reminder readedReminder = JsonSerializer.Deserialize<Reminder>(readText)!;
            readedReminder.ReminderHeader = file.Name[..^5];
            items.Add(readedReminder);
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
                var directory = storageFolder.Path + @"\Trash\" + LstNotes.SelectedItem.ToString() + ".rtf";
                var file = await StorageFile.GetFileFromPathAsync(directory);
                await file.DeleteAsync();
                LstNotes.Items.Remove(selectedItem);
            }
            else
            {
                ContentDialog infoDialog = new()
                { XamlRoot = XamlRoot, Title = "Info".GetLocalized(), Content = "NoSelection".GetLocalized(), CloseButtonText = "Ok".GetLocalized() };
                await infoDialog.ShowAsync();
            }
        }
        catch (Exception ex)
        {
            ContentDialog infoDialog = new()
            {
                XamlRoot = XamlRoot,
                Title = "Error".GetLocalized(),
                Content = "Error_Meesage".GetLocalized() + ex.Message,
                CloseButtonText = "Ok".GetLocalized()
            };
            await infoDialog.ShowAsync();
            LogWriter.Log(ex.Message, LogWriter.LogLevel.Error);
        }
    }
    private async void DeleteReminder()
    {
        try
        {
            if (LstReminders.SelectedItem is Reminder selectedItem)
            {
                var directory = storageFolder.Path + @"\Trash\" + selectedItem.ReminderHeader + ".json";
                var file = await StorageFile.GetFileFromPathAsync(directory);
                await file.DeleteAsync();
                items.Remove(selectedItem);
            }
            else
            {
                ContentDialog infoDialog = new()
                { XamlRoot = XamlRoot, Title = "Info".GetLocalized(), Content = "NoSelection".GetLocalized(), CloseButtonText = "Ok".GetLocalized() };
                await infoDialog.ShowAsync();
            }
        }
        catch (Exception ex)
        {
            ContentDialog errorDialog = new()
            {
                XamlRoot = XamlRoot,
                Title = "Error".GetLocalized(),
                Content = "Error_Meesage".GetLocalized() + ex.Message,
                CloseButtonText = "Ok".GetLocalized()
            };
            await errorDialog.ShowAsync();
            LogWriter.Log(ex.Message, LogWriter.LogLevel.Error);
        }
    }
    private void LstNotes_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!deleteNote.IsEnabled)
        {
            deleteNote.IsEnabled = true;
        }
        if (!restoreNote.IsEnabled)
        {
            restoreNote.IsEnabled = true;
        }
    }
    private void RestoreNote_Click(object sender, RoutedEventArgs e)
    {
        if (LstNotes.SelectedItem != null)
        {
            if (LstNotes.Items.Count <= 1)
            {
                EmptyText.Visibility = Visibility.Visible;
            }
            MoveFile moveFile = new();
            moveFile.Move("Trash", "Notes", LstNotes, XamlRoot);
        }
    }
    private void DeleteNote_Click(object sender, RoutedEventArgs e)
    {
        if (LstNotes.SelectedItem != null)
        {
            if (LstNotes.Items.Count <= 1)
            {
                EmptyText.Visibility = Visibility.Visible;
            }
            DeleteNote();
        }
        deleteNote.Flyout.Hide();
    }
    private void LstReminders_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!deleteReminder.IsEnabled)
        {
            deleteReminder.IsEnabled = true;
        }
        if (!restoreReminder.IsEnabled)
        {
            restoreReminder.IsEnabled = true;
        }
    }
    private void RestoreReminder_Click(object sender, RoutedEventArgs e)
    {
        if (LstReminders.SelectedItem != null)
        {
            if (LstReminders.Items.Count <= 1)
            {
                EmptyText2.Visibility = Visibility.Visible;
            }
            Reminder? rm = LstReminders.SelectedItem as Reminder;
            MoveFile moveFile = new();
            moveFile.Move("Trash", "Reminders", LstReminders, XamlRoot, rm!, items);
        }
    }
    private void DeleteReminder_Click(object sender, RoutedEventArgs e)
    {
        if (LstReminders.SelectedItem != null)
        {
            if (LstReminders.Items.Count <= 1)
            {
                EmptyText2.Visibility = Visibility.Visible;
            }
            DeleteReminder();
        }
        deleteReminder.Flyout.Hide();
    }

    private void NotesSearch_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        ListNotes(NotesSearch.Text);
    }

    private void RemindersSearch_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (RemindersSearch != null)
        {
            LstReminders.ItemsSource = items.Where(x => x.ReminderHeader.ToLower().Contains(RemindersSearch.Text.ToLower()));
            LstReminders.UpdateLayout();
        }
    }
}
