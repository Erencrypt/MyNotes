using Microsoft.UI.Xaml.Controls;
using MyNotes.Helpers;
using MyNotes.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;

namespace MyNotes.Views;

public sealed partial class RemindersPage : Page
{
    public RemindersViewModel ViewModel
    {
        get;
    }
    public static bool isNewNote = false;
    public RemindersPage()
    {
        ViewModel = App.GetService<RemindersViewModel>();
        InitializeComponent();
        deleteFlyout.Text = "DeleteFlyout".GetLocalized();
        deleteReminderFly.Content = "DeleteReminder_Button".GetLocalized();
        ToolTipService.SetToolTip(deleteReminder, "DeleteReminder".GetLocalized());
        ToolTipService.SetToolTip(newReminder, "AddReminder".GetLocalized());
        List<Reminder> items = new();
        for (int i = 0; i < 4; i++)
        {
            items.Add(new Reminder() { ReminderHeader = "test Reminder 1", ReminderText = "this is the thing i want to remember", DateTime = DateTime.Now, Repeat="true" });
            items.Add(new Reminder() { ReminderHeader = "reminder test", ReminderText = "this is another thing to remember", DateTime = DateTime.Now, Repeat = "false" });
            items.Add(new Reminder() { ReminderHeader = "test 123", ReminderText = "this one... im not sure", DateTime = DateTime.Now, Repeat = "false" });
        }
        LstReminders.ItemsSource = items;
    }
    public class Reminder
    {
        public string ?ReminderHeader { get; set; }
        public string ?ReminderText { get; set; }
        public DateTime? DateTime { get; set; }
        public string? Repeat { get; set; }
    }
    private async void AddReminder()
    {
        isNewNote = true;
        CreateReminderDialog AddReminderDialog = new()
        {
            XamlRoot = XamlRoot
        };
        await AddReminderDialog.ShowAsync();
    }
    private async void EditReminder()
    {
        isNewNote = false;
        CreateReminderDialog EditReminderDialog = new()
        {
            XamlRoot = XamlRoot,
            Title = "Edit Reminder",
            PrimaryButtonText = "Save Reminder"
        };
        await EditReminderDialog.ShowAsync();
    }
    private void LstReminders_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        EditReminder();
    }
    private void NewReminder_Click(object sender,RoutedEventArgs e)
    {
        AddReminder();
    }
    private void DeleteReminder_Click(object sender, RoutedEventArgs e)
    {

    }

    private void LstReminders_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        deleteReminder.IsEnabled = true;
    }
}
