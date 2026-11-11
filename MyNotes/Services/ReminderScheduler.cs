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
        // Original time of day for repeating reminders. Snoozing changes the in-memory
        // ReminderDateTime, so the next occurrence is always calculated from this instead.
        private readonly ConcurrentDictionary<Guid, TimeSpan> _repeatTimes = new();
        // How late a reminder can be and still count as "on time" (the loop ticks every 6 seconds).
        private static readonly TimeSpan OnTimeWindow = TimeSpan.FromMinutes(1);
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
                // Daily recurring: only the time of day matters, so schedule the next occurrence of it.
                Schedule(r);
                r.ReminderDateTime = NextOccurrence(_repeatTimes[r.Id], DateTime.Now);
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
            if (reminder.Repeat)
            {
                _repeatTimes[reminder.Id] = reminder.ReminderDateTime.TimeOfDay;
            }
            else
            {
                _repeatTimes.TryRemove(reminder.Id, out _);
            }

            _scheduled[reminder.Id] = reminder;
        }

        public void Unschedule(Guid reminderId)
        {
            _scheduled.TryRemove(reminderId, out _);
            _repeatTimes.TryRemove(reminderId, out _);
        }

        /// <summary>
        /// Next time a daily reminder at <paramref name="timeOfDay"/> should fire:
        /// today if that time is still ahead, otherwise tomorrow.
        /// </summary>
        private static DateTime NextOccurrence(TimeSpan timeOfDay, DateTime now)
        {
            var today = now.Date.Add(timeOfDay);
            return today > now ? today : today.AddDays(1);
        }

        private TimeSpan RepeatTimeOf(Reminder reminder) =>
            _repeatTimes.TryGetValue(reminder.Id, out var time) ? time : reminder.ReminderDateTime.TimeOfDay;
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
                    // Use the original time of day (not the snoozed one) and move to its next occurrence.
                    reminder.ReminderDateTime = NextOccurrence(RepeatTimeOf(reminder), DateTime.Now);
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
                    var due = _scheduled.Values.Where(r => r.ReminderDateTime <= now && r.ReminderDateTime >= now - OnTimeWindow).ToList();
                    await HandleMissedRemindersAsync(now);

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
                                // Repeating: next occurrence of the original time (in-memory only),
                                // so a snooze doesn't shift the daily time.
                                r.ReminderDateTime = NextOccurrence(RepeatTimeOf(r), now);
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

        /// <summary>
        /// Handles reminders whose time passed while they couldn't fire (e.g. the PC was asleep).
        /// Repeating ones skip to their next occurrence. One-time ones are moved to the trash and
        /// reported in a single summary notification, the same way expired reminders are at startup.
        /// </summary>
        private async Task HandleMissedRemindersAsync(DateTime now)
        {
            var missed = _scheduled.Values.Where(r => r.ReminderDateTime < now - OnTimeWindow).ToList();
            if (missed.Count == 0)
            {
                return;
            }

            int expiredCount = 0;
            foreach (var r in missed)
            {
                try
                {
                    if (r.Repeat)
                    {
                        r.ReminderDateTime = NextOccurrence(RepeatTimeOf(r), now);
                        LogWriter.Log($"Repeating reminder {r.Id} was missed; moved to {r.ReminderDateTime:g}.", LogWriter.LogLevel.Info);
                    }
                    else
                    {
                        await _reminderService.SoftDeleteReminderAsync(r.Id);
                        Unschedule(r.Id);
                        ReminderDismissed?.Invoke(r);
                        expiredCount++;
                    }
                }
                catch (Exception ex)
                {
                    LogWriter.Log($"Failed to handle missed reminder {r.Id}: {ex}", LogWriter.LogLevel.Error);
                }
            }

            if (expiredCount > 0)
            {
                await ShowNotificationsAsync(0, expiredCount);
            }
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
