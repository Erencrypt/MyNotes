using Microsoft.UI.Xaml.Controls;

using MyNotes.ViewModels;
using Windows.System;

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
        List<Reminder> items = new List<Reminder>();
        for (int i = 0; i < 4; i++)
        {
            items.Add(new Reminder() { ReminderHeader = "test Reminder 1", ReminderText = "this is the thing i want to remember" });
            items.Add(new Reminder() { ReminderHeader = "reminder test", ReminderText = "this is another thing to remember" });
            items.Add(new Reminder() { ReminderHeader = "test 123", ReminderText = "this one... im not sure" });
        }
        LstReminders.ItemsSource = items;
    }
    public class Reminder
    {
        public string ReminderHeader { get; set; }

        public string ReminderText { get; set; }
    }
}
