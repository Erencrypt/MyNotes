using Microsoft.EntityFrameworkCore;
using MyNotes.Data.Context;
using MyNotes.Data.Services.Interfaces;

namespace MyNotes.Data.Services
{
    public sealed class TrashCleanupService : ITrashCleanupService
    {
        private readonly IDbContextFactory<AppDbContext> _contextFactory;

        public TrashCleanupService(IDbContextFactory<AppDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task PermanentlyDeleteExpiredItemsAsync(int retentionDays, CancellationToken cancellationToken = default)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(retentionDays);
            var cutoff = DateTime.Now.AddDays(-retentionDays);

            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

            await context.TaskItems
                .IgnoreQueryFilters()
                .Where(item => item.DeletedAt != null && item.DeletedAt < cutoff)
                .ExecuteDeleteAsync(cancellationToken);

            await context.TaskLists
                .IgnoreQueryFilters()
                .Where(list => list.DeletedAt != null && list.DeletedAt < cutoff)
                .ExecuteDeleteAsync(cancellationToken);

            await context.Reminders
                .IgnoreQueryFilters()
                .Where(reminder => reminder.DeletedAt != null && reminder.DeletedAt < cutoff)
                .ExecuteDeleteAsync(cancellationToken);

            await context.Notes
                .IgnoreQueryFilters()
                .Where(note => note.DeletedAt != null && note.DeletedAt < cutoff)
                .ExecuteDeleteAsync(cancellationToken);

            //await context.Settings
            //    .IgnoreQueryFilters()
            //    .Where(setting => setting.DeletedAt != null && setting.DeletedAt < cutoff)
            //    .ExecuteDeleteAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
    }
}
