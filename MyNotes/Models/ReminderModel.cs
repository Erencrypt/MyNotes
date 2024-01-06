namespace MyNotes.Models;
public class Reminder
{
    public string? ReminderIcon { get; set; }
    public string? ReminderHeader { get; set; }
    public string? ReminderText { get; set; }
    public string? DateTime { get; set; }
    public bool Repeat { get; set; }
    public bool Alarm { get; set; }
}