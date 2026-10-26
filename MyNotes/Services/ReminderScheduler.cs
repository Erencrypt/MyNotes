using MyNotes.Data.Models;
using MyNotes.Data.Services.Interfaces;
using MyNotes.Helpers;
using MyNotes.Services.Interfaces;
using System.Collections.Concurrent;
using WinUI3Localizer;

namespace MyNotes.Services
{
    public partial class ReminderScheduler : IReminderScheduler, IDisposable
    {
        private readonly ILocalizer _localizer = Localizer.Get();
        private readonly IReminderService _reminderService;
        private readonly IAppNotificationService _notificationService;
        private readonly ConcurrentDictionary<Guid, Reminder> _scheduled = new();
        private readonly CancellationTokenSource _cts = new();
        private Task? _workerTask;

        public event Func<Reminder, Task>? ReminderInvoked;
        public event Action<Reminder>? ReminderSnoozed;
        public event Action<Reminder>? ReminderDismissed;

        public ReminderScheduler(IReminderService reminderService, IAppNotificationService notificationService)
        {
            _reminderService = reminderService;
            _notificationService = notificationService;
        }

        public async Task InitializeAsync()
        {
            int DeletedCount = 0;
            int AddedCount = 0;
            // Get upcoming non-repeating reminders (not yet fired)
            var upcomingNonRepeating = await _reminderService.GetUpcomingRemindersAsync();
            foreach (var r in upcomingNonRepeating)
            {
                _scheduled[r.Id] = r;
                AddedCount += 1;
            }

            // Get all active repeating reminders
            var repeating = await _reminderService.GetRepeatingRemindersAsync();

            foreach (var r in repeating)
            {
                // Calculate next occurrence based on time only (daily recurring)
                var reminderTime = r.ReminderDateTime.TimeOfDay;
                var todayOccurrence = DateTime.Now.Date.Add(reminderTime);

                if (todayOccurrence <= DateTime.Now)
                {
                    // Time has passed today, schedule for tomorrow
                    r.ReminderDateTime = DateTime.Now.Date.AddDays(1).Add(reminderTime);
                }
                else
                {
                    // Time hasn't passed, schedule for today
                    r.ReminderDateTime = todayOccurrence;
                }
                _scheduled[r.Id] = r;
                AddedCount += 1;
            }

            var expired = await _reminderService.GetExpiredRemindersAsync();
            foreach (var r in expired)
            {
                DeletedCount += 1;
                await _reminderService.SoftDeleteReminderAsync(r.Id);
            }

            _workerTask = Task.Run(() => WorkerLoopAsync(_cts.Token));
            await ShowNotificationsAsync(AddedCount, DeletedCount);
        }

        public void Schedule(Reminder reminder)
        {
            _scheduled[reminder.Id] = reminder;
        }

        public void Unschedule(Guid reminderId)
        {
            _scheduled.TryRemove(reminderId, out _);
        }
        public void Reschedule(Reminder reminder)
        {
            // Always upsert: remove existing entry (if any) and re-add with the new data.
            // Previously this only ran when the reminder was already in _scheduled,
            // which meant editing a repeating reminder that wasn't in the dict was silently dropped.
            Unschedule(reminder.Id);
            Schedule(reminder);
        }

        public async Task SnoozeAsync(Guid reminderId, TimeSpan snoozeDuration)
        {
            if (_scheduled.TryGetValue(reminderId, out var reminder))
            {
                // Update in-memory scheduled time only, do NOT update database
                reminder.ReminderDateTime = DateTime.Now.Add(snoozeDuration);
                ReminderSnoozed?.Invoke(reminder);
            }
            await Task.CompletedTask;
        }

        public async Task DismissAsync(Guid reminderId)
        {
            if (_scheduled.TryGetValue(reminderId, out var reminder))
            {
                if (reminder.Repeat)
                {
                    // Gets reminders from db to ensure correct time is used for rescheduling, in case it was snoozed before.
                    var reminderForTime = await _reminderService.GetReminderByIdAsync(reminderId) ?? reminder;
                    // For repeating reminders, reschedule for tomorrow
                    reminder.ReminderDateTime = reminderForTime.ReminderDateTime.AddDays(1);
                    await _reminderService.UpdateReminderAsync(reminder);
                    // Keep it in scheduled with updated time
                }
                else
                {
                    // For non-repeating, delete from DB
                    await _reminderService.SoftDeleteReminderAsync(reminder.Id);
                    _scheduled.TryRemove(reminderId, out _);
                }
                ReminderDismissed?.Invoke(reminder);
            }
        }

        private async Task WorkerLoopAsync(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    var now = DateTime.Now;
                    var due = _scheduled.Values.Where(r => r.ReminderDateTime <= now && r.ReminderDateTime >= now.AddMinutes(-1)).ToList();
                    foreach (var r in due)
                    {
                        try
                        {
                            var notificationShown = await _notificationService.ShowReminderAsync(
                                r.Id,
                                r.ReminderHeader ?? string.Empty,
                                r.ReminderText ?? string.Empty,
                                r.ReminderDateTime.ToString("g"),
                                r.Alarm,
                                r.NoteId);
                            if (!notificationShown)
                            {
                                LogWriter.Log($"Windows did not accept the notification for reminder {r.Id}.",
                                    LogWriter.LogLevel.Warning);
                            }

                            // raise event for subscribers
                            if (ReminderInvoked != null)
                            {
                                await ReminderInvoked.Invoke(r);
                            }

                            if (!r.Repeat)
                            {
                                // Non-repeating: remove from schedule
                                _scheduled.TryRemove(r.Id, out _);
                            }
                            else
                            {
                                // Repeating: reschedule for tomorrow (daily) in-memory only
                                r.ReminderDateTime = r.ReminderDateTime.AddDays(1);
                                // Keep in scheduled dict with updated time
                            }
                        }
                        catch (Exception ex)
                        {
                            LogWriter.Log($"Failed to show reminder notification {r.Id}: {ex}", LogWriter.LogLevel.Error);
                        }
                    }

                    await Task.Delay(TimeSpan.FromSeconds(6), ct);
                }
            }
            catch (TaskCanceledException) { }
        }

        private async Task ShowNotificationsAsync(int AddedCount, int DeletedCount)
        {
            try
            {
                bool notificationShown;
                if (DeletedCount > 0 && AddedCount > 0)
                {
                    notificationShown = await _notificationService.ShowDeletedMessage(
                        _localizer.GetLocalizedString("Info"),
                        string.Format(_localizer.GetLocalizedString("AppNotification_ActiveReminder"), AddedCount) + "\n" +
                        string.Format(_localizer.GetLocalizedString("AppNotification_Expired"), DeletedCount));
                }
                else if (DeletedCount > 0)
                {
                    notificationShown = await _notificationService.ShowDeletedMessage(
                        _localizer.GetLocalizedString("Info"),
                        string.Format(_localizer.GetLocalizedString("AppNotification_Expired"), DeletedCount));
                }
                else if (AddedCount > 0)
                {
                    notificationShown = await _notificationService.ShowInfoMessage(
                        _localizer.GetLocalizedString("Info"),
                        string.Format(_localizer.GetLocalizedString("AppNotification_ActiveReminder"), AddedCount));
                }
                else
                { return; }

                if (!notificationShown)
                {
                    LogWriter.Log("The startup reminder summary notification was not accepted by Windows.", LogWriter.LogLevel.Warning);
                }
            }
            catch (Exception ex)
            {
                LogWriter.Log($"Failed to show the startup reminder summary notification: {ex}", LogWriter.LogLevel.Error);
            }
        }
        public void Dispose()
        {
            _cts.Cancel();
            try { _workerTask?.Wait(1000); } catch { }
            _cts.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
