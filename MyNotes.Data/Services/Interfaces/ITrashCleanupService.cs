namespace MyNotes.Data.Services.Interfaces
{
    /// <summary>
    /// Removes expired soft-deleted records from the database.
    /// </summary>
    public interface ITrashCleanupService
    {
        Task PermanentlyDeleteExpiredItemsAsync(int retentionDays, CancellationToken cancellationToken = default);
    }
}
