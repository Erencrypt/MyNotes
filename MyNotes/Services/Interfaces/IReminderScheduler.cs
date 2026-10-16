using MyNotes.Data.Models;

namespace MyNotes.Services.Interfaces
{
    public interface IReminderScheduler
    {
        Task InitializeAsync();
        void Schedule(Reminder reminder);
        void Unschedule(Guid reminderId);
        void Reschedule(Reminder reminder);
        Task SnoozeAsync(Guid reminderId, TimeSpan snoozeDuration);
        Task DismissAsync(Guid reminderId);

        event Func<Reminder, Task>? ReminderInvoked;
        event Action<Reminder>? ReminderSnoozed;
        event Action<Reminder>? ReminderDismissed;
    }
}
