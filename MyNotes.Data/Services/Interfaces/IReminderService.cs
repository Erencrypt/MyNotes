using MyNotes.Data.Models;

namespace MyNotes.Data.Services.Interfaces
{
    /// <summary>
    /// Service interface for reminder operations
    /// </summary>
    public interface IReminderService
    {
        Task<List<Reminder>> GetAllRemindersAsync();
        Task<List<Reminder>> GetActiveRemindersAsync();
        Task<List<Reminder>> GetDeletedRemindersAsync();
        Task<List<Reminder>> GetUpcomingRemindersAsync();
        Task<List<Reminder>> GetRepeatingRemindersAsync();
        Task<List<Reminder>> GetExpiredRemindersAsync();
        Task<Reminder?> GetReminderByIdAsync(Guid id);
        Task<Reminder> CreateReminderAsync(Reminder reminder);
        Task<Reminder> UpdateReminderAsync(Reminder reminder);
        Task SoftDeleteReminderAsync(Guid id);
        Task RestoreReminderAsync(Guid id);
        Task PermanentlyDeleteReminderAsync(Guid id);
        Task<List<Reminder>> GetRemindersForNoteAsync(Guid noteId);
        Task<List<Reminder>> SearchRemindersAsync(string searchTerm, CancellationToken ct = default);
        Task<List<Reminder>> SearchDeletedRemindersAsync(string searchTerm, CancellationToken ct = default);

    }
}
