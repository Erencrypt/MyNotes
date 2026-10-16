using Microsoft.EntityFrameworkCore;
using MyNotes.Data.Context;
using MyNotes.Data.Models;
using MyNotes.Data.Services.Interfaces;

namespace MyNotes.Data.Services
{
    /// <summary>
    /// Service implementation for reminder operations
    /// </summary>
    public class ReminderService : IReminderService
    {
        private readonly IDbContextFactory<AppDbContext> _contextFactory;

        public ReminderService(IDbContextFactory<AppDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<List<Reminder>> GetAllRemindersAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Reminders
                .Include(r => r.Note)
                .OrderByDescending(r => r.UpdatedAt)
                .ToListAsync();
        }

        public async Task<List<Reminder>> GetActiveRemindersAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Reminders
                .Where(r => r.DeletedAt == null)
                .Include(r => r.Note)
                .OrderByDescending(r => r.UpdatedAt)
                .ToListAsync();
        }

        public async Task<List<Reminder>> GetDeletedRemindersAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Reminders
                .IgnoreQueryFilters()
                .Where(r => r.DeletedAt != null)
                .Include(r => r.Note)
                .OrderByDescending(r => r.UpdatedAt)
                .ToListAsync();
        }

        public async Task<List<Reminder>> GetUpcomingRemindersAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Reminders
                .Where(r => r.DeletedAt == null)
                .Where(r => r.ReminderDateTime > DateTime.Now && r.IsActive && !r.Repeat)
                .Include(r => r.Note)
                .OrderBy(r => r.ReminderDateTime)
                .ToListAsync();
        }

        public async Task<List<Reminder>> GetRepeatingRemindersAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Reminders
                .Where(r => r.DeletedAt == null)
                .Where(r => r.Repeat && r.IsActive)
                .Include(r => r.Note)
                .OrderBy(r => r.ReminderDateTime)
                .ToListAsync();
        }

        public async Task<List<Reminder>> GetExpiredRemindersAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Reminders
                .Where(r => r.ReminderDateTime <= DateTime.Now && r.IsActive && !r.Repeat)
                .Include(r => r.Note)
                .OrderByDescending(r => r.ReminderDateTime)
                .ToListAsync();
        }

        public async Task<Reminder?> GetReminderByIdAsync(Guid id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Reminders
                .Include(r => r.Note)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<Reminder> CreateReminderAsync(Reminder reminder)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            context.Reminders.Add(reminder);
            await context.SaveChangesAsync();
            return reminder;
        }

        public async Task<Reminder> UpdateReminderAsync(Reminder reminder)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            reminder.UpdatedAt = DateTime.UtcNow;
            context.Reminders.Update(reminder);
            await context.SaveChangesAsync();
            return reminder;
        }

        public async Task SoftDeleteReminderAsync(Guid id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var reminder = await context.Reminders.FindAsync(id);
            if (reminder != null)
            {
                reminder.DeletedAt = DateTime.Now;
                reminder.UpdatedAt = DateTime.Now;
                await context.SaveChangesAsync();
            }
        }

        public async Task RestoreReminderAsync(Guid id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var reminder = await context.Reminders
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(r => r.Id == id && r.DeletedAt != null);

            if (reminder != null)
            {
                reminder.DeletedAt = null;
                reminder.UpdatedAt = DateTime.Now;
                await context.SaveChangesAsync();
            }
        }

        public async Task PermanentlyDeleteReminderAsync(Guid id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var reminder = await context.Reminders
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(r => r.Id == id && r.DeletedAt != null);

            if (reminder != null)
            {
                context.Reminders.Remove(reminder);
                await context.SaveChangesAsync();
            }
        }

        public async Task<List<Reminder>> GetRemindersForNoteAsync(Guid noteId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Reminders
                .Where(r => r.DeletedAt == null)
                .Where(r => r.NoteId == noteId)
                .Include(r => r.Note)
                .OrderByDescending(r => r.UpdatedAt)
                .ToListAsync();
        }

        public async Task<List<Reminder>> SearchRemindersAsync(string searchTerm, CancellationToken ct = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Reminders
                .Where(r => r.DeletedAt == null)
                .Where(r => r.ReminderHeader.Contains(searchTerm) || r.ReminderText.Contains(searchTerm))
                .Include(r => r.Note)
                .OrderByDescending(r => r.UpdatedAt)
                .ToListAsync(ct);
        }

        public async Task<List<Reminder>> SearchDeletedRemindersAsync(string searchTerm, CancellationToken ct = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Reminders
                .Where(r => r.DeletedAt != null)
                .Where(r => r.ReminderHeader.Contains(searchTerm) || r.ReminderText.Contains(searchTerm))
                .Include(r => r.Note)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync(ct);
        }
    }
}
