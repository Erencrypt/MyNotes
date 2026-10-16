using Microsoft.EntityFrameworkCore;
using MyNotes.Data.Context;
using MyNotes.Data.Models;
using MyNotes.Data.Services.Interfaces;


namespace MyNotes.Data.Services
{
    public class TaskListService : ITaskListService
    {
        private readonly IDbContextFactory<AppDbContext> _contextFactory;

        public TaskListService(IDbContextFactory<AppDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<List<TaskList>> GetActiveTaskListsAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.TaskLists
                .Where(tl => tl.DeletedAt == null)
                .OrderBy(tl => tl.Order)
                .ThenByDescending(tl => tl.UpdatedAt)
                .ToListAsync();
        }

        public async Task<List<TaskList>> GetAllTaskListsAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.TaskLists
                 .Include(tl => tl.TaskItems)
                 .OrderBy(tl => tl.Order)
                 .ThenByDescending(tl => tl.UpdatedAt)
                 .ToListAsync();
        }

        public async Task<List<TaskList>> GetDeletedTaskListsAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.TaskLists
                .IgnoreQueryFilters()
                .Where(tl => tl.DeletedAt != null)
                .Include(tl => tl.TaskItems)
                .OrderBy(tl => tl.Order)
                .ThenByDescending(tl => tl.UpdatedAt)
                .ToListAsync();
        }

        public async Task<TaskList?> GetTaskListByIdAsync(Guid id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.TaskLists
                .Include(tl => tl.TaskItems)
                .FirstOrDefaultAsync(tl => tl.Id == id);
        }

        public async Task<TaskList> CreateTaskListAsync(TaskList taskList)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            context.TaskLists.Add(taskList);
            await context.SaveChangesAsync();
            return taskList;
        }

        public async Task<TaskList> UpdateTaskListAsync(TaskList taskList)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            taskList.UpdatedAt = DateTime.UtcNow;
            context.TaskLists.Update(taskList);
            await context.SaveChangesAsync();
            return taskList;
        }

        public async Task UpdateTaskListOrdersAsync(IEnumerable<TaskList> taskLists)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var now = DateTime.UtcNow;
            foreach (var item in taskLists)
            {
                var existing = await context.TaskLists.FindAsync(item.Id);
                if (existing != null)
                {
                    existing.Order = item.Order;
                    existing.UpdatedAt = now;
                }
            }
            await context.SaveChangesAsync();
        }

        public async Task SoftDeleteTaskListAsync(Guid id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var taskList = await context.TaskLists
                .Include(tl => tl.TaskItems)
                .FirstOrDefaultAsync(tl => tl.Id == id);
            if (taskList != null)
            {
                var deletedAt = DateTime.Now;
                taskList.DeletedAt = deletedAt;
                taskList.UpdatedAt = deletedAt;

                taskList.TaskItems.ToList().ForEach(ti =>
                {
                    ti.DeletedAt = deletedAt;
                    ti.UpdatedAt = deletedAt;
                });
                await context.SaveChangesAsync();
            }
        }

        public async Task RestoreTaskListAsync(Guid id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var taskList = await context.TaskLists
                .IgnoreQueryFilters()
                .Include(tl => tl.TaskItems)
                .FirstOrDefaultAsync(tl => tl.Id == id && tl.DeletedAt != null);

            if (taskList != null)
            {
                var deletedAt = taskList.DeletedAt!.Value;
                var restoredAt = DateTime.Now;
                taskList.DeletedAt = null;
                taskList.UpdatedAt = restoredAt;

                taskList.TaskItems
                    .Where(ti => ti.DeletedAt >= deletedAt)
                    .ToList()
                    .ForEach(ti =>
                {
                    ti.DeletedAt = null;
                    ti.UpdatedAt = restoredAt;
                });
                await context.SaveChangesAsync();
            }
        }

        public async Task PermanentlyDeleteTaskListAsync(Guid id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var taskList = await context.TaskLists
                .IgnoreQueryFilters()
                .Include(tl => tl.TaskItems)
                .FirstOrDefaultAsync(tl => tl.Id == id && tl.DeletedAt != null);

            if (taskList != null)
            {
                context.TaskLists.Remove(taskList);
                await context.SaveChangesAsync();
            }
        }

        public async Task<List<TaskList>> SearchDeletedTaskListsAsync(string searchTerm, CancellationToken ct = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.TaskLists
                .IgnoreQueryFilters()
                .Where(tl => tl.DeletedAt != null)
                .Where(tl => tl.Name.Contains(searchTerm))
                .Include(tl => tl.TaskItems)
                .OrderByDescending(tl => tl.UpdatedAt)
                .ToListAsync(ct);
        }

        public async Task<List<TaskList>> SearchTaskListsAsync(string searchTerm, CancellationToken ct = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.TaskLists
                .Where(tl => tl.DeletedAt == null)
                .Where(tl => tl.Name.Contains(searchTerm))
                .Include(tl => tl.TaskItems)
                .OrderBy(tl => tl.Order)
                .ThenByDescending(tl => tl.UpdatedAt)
                .ToListAsync(ct);
        }
    }
}
