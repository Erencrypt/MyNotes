using Microsoft.UI.Xaml.Controls;
using MyNotes.Helpers;
using MyNotes.ViewModels;

namespace MyNotes.Views;

public sealed partial class RemindersPage : Page
{
    public RemindersViewModel ViewModel
    {
        get;
    }

    public RemindersPage()
    {
        ViewModel = App.GetService<RemindersViewModel>();
        InitializeComponent();
        deleteFlyout.Text = "DeleteFlyout".GetLocalized();
        deleteReminderFly.Content = "DeleteReminder_Button".GetLocalized();
        ToolTipService.SetToolTip(deleteReminder, "DeleteReminder".GetLocalized());
        ToolTipService.SetToolTip(newReminder, "AddReminder".GetLocalized());

        List<Reminder> items = new List<Reminder>();
        for (int i = 0; i < 4; i++)
        {
            items.Add(new Reminder() { ReminderHeader = "test Reminder 1", ReminderText = "this is the thing i want to remember", Date=DateTime.Now, Repeat="true" });
            items.Add(new Reminder() { ReminderHeader = "reminder test", ReminderText = "this is another thing to remember", Date = DateTime.Now, Repeat = "false" });
            items.Add(new Reminder() { ReminderHeader = "test 123", ReminderText = "this one... im not sure", Date = DateTime.Now, Repeat = "false" });
        }
        LstReminders.ItemsSource = items;
    }
    public class Reminder
    {
        public string ?ReminderHeader { get; set; }
        public string ?ReminderText { get; set; }
        public DateTime? Date { get; set; }
        public string? Repeat { get; set; }
    }
    private async void AddReminder()
    {
        CreateReminderDialog AddReminderDialog = new CreateReminderDialog();
        AddReminderDialog.XamlRoot = XamlRoot;
        AddReminderDialog.Title = "New Reminder";
        await AddReminderDialog.ShowAsync();
    }
    private void LstReminders_DoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
    {
        
    }
    private void newReminder_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        AddReminder();
    }
    private void deleteReminder_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {

    }

    private void LstReminders_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        deleteReminder.IsEnabled = true;
    }
}
