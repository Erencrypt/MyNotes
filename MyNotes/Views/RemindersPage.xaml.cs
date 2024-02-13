using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using MyNotes.Helpers;
using MyNotes.Models;
using MyNotes.ViewModels;
using System.Collections.ObjectModel;
using System.Text;
using System.Text.Json;
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
    public static bool IsNewReminder { get; set; }
    public static string ReminderName { get; set; } = string.Empty;

    public RemindersPage()
    {
        ViewModel = App.GetService<RemindersViewModel>();
        InitializeComponent();
        deleteFlyout.Text = "DeleteFlyout".GetLocalized();
        deleteReminderFly.Content = "DeleteConfirm".GetLocalized();
        EmptyText.Text = "Reminders_EmptyText".GetLocalized();
        ToolTipService.SetToolTip(deleteReminder, "Delete".GetLocalized());
        ToolTipService.SetToolTip(newReminder, "Add".GetLocalized());
        LstReminders.ItemsSource = items;
        ListReminders();
        EmptyText.Visibility = LstReminders.Items.Count < 1 ? Visibility.Visible : Visibility.Collapsed;
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
            LstReminders.ItemsSource = items;
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
            ReminderName = selectedItem.ReminderHeader!;
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
    }
    private void ListReminders()
    {
        DirectoryInfo dinfo = new(storageFolder.Path + "\\Reminders");
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
    private void LstReminders_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (LstReminders.SelectedItem != null)
        {
            EditReminder();
        }
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

    private void RemindersSearch_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (RemindersSearch!=null)
        {
            VisualStateManager.GoToState(this, "Normal", false);
            VisualStateManager.GoToState(this, "FadeOut", false);
            LstReminders.ItemsSource = items.Where(x => x.ReminderHeader.ToLower().Contains(RemindersSearch.Text.ToLower()));
            LstReminders.UpdateLayout();
            VisualStateManager.GoToState(this, "FadeIn", false);
        }
    }
}
