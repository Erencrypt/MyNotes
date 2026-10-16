using Microsoft.EntityFrameworkCore;
using MyNotes.Data.Context;
using MyNotes.Data.Models;
using MyNotes.Data.Services.Interfaces;

namespace MyNotes.Data.Services
{
    public class TaskItemService : ITaskItemService
    {
        private readonly IDbContextFactory<AppDbContext> _contextFactory;

        public TaskItemService(IDbContextFactory<AppDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<List<TaskItem>> GetActiveTaskItemsAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.TaskItems
                .Where(ti => ti.DeletedAt == null)
                .OrderBy(ti => ti.Order)
                .ThenByDescending(ti => ti.UpdatedAt)
                .ToListAsync();
        }

        public async Task<List<TaskItem>> GetAllTaskItemsAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.TaskItems
                .Include(ti => ti.TaskList)
                .OrderBy(ti => ti.Order)
                .ThenByDescending(ti => ti.UpdatedAt)
                .ToListAsync();
        }

        public async Task<List<TaskItem>> GetDeletedTaskItemsAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.TaskItems
                .IgnoreQueryFilters()
                .Where(ti => ti.DeletedAt != null)
                .Include(ti => ti.TaskList)
                .OrderBy(ti => ti.Order)
                .ThenByDescending(ti => ti.UpdatedAt)
                .ToListAsync();
        }

        public async Task<TaskItem?> GetTaskItemByIdAsync(Guid id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.TaskItems
                .Include(ti => ti.TaskList)
                .FirstOrDefaultAsync(ti => ti.Id == id);
        }

        public async Task<TaskItem> CreateTaskItemAsync(TaskItem taskItem)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            context.TaskItems.Add(taskItem);
            await context.SaveChangesAsync();
            return taskItem;
        }

        public async Task<TaskItem> UpdateTaskItemAsync(TaskItem taskItem)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            taskItem.UpdatedAt = DateTime.UtcNow;
            context.TaskItems.Update(taskItem);
            await context.SaveChangesAsync();
            return taskItem;
        }

        public async Task UpdateTaskItemOrdersAsync(IEnumerable<TaskItem> items)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var now = DateTime.UtcNow;
            foreach (var item in items)
            {
                var existing = await context.TaskItems.FindAsync(item.Id);
                if (existing != null)
                {
                    existing.Order = item.Order;
                    existing.TaskListId = item.TaskListId;
                    existing.UpdatedAt = now;
                }
            }
            await context.SaveChangesAsync();
        }

        public async Task SoftDeleteTaskItemAsync(Guid id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var taskItem = await context.TaskItems.FindAsync(id);
            if (taskItem != null)
            {
                taskItem.DeletedAt = DateTime.Now;
                taskItem.UpdatedAt = DateTime.Now;
                await context.SaveChangesAsync();
            }
        }

        public async Task RestoreTaskItemAsync(Guid id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var taskItem = await context.TaskItems
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(ti => ti.Id == id && ti.DeletedAt != null);

            if (taskItem != null)
            {
                taskItem.DeletedAt = null;
                taskItem.UpdatedAt = DateTime.Now;
                await context.SaveChangesAsync();
            }
        }

        public async Task PermanentlyDeleteTaskItemAsync(Guid id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var taskItem = await context.TaskItems
               .IgnoreQueryFilters()
               .FirstOrDefaultAsync(ti => ti.Id == id && ti.DeletedAt != null);

            if (taskItem != null)
            {
                context.TaskItems.Remove(taskItem);
                await context.SaveChangesAsync();
            }
        }

        public async Task<List<TaskItem>> SearchDeletedTaskItemsAsync(string searchTerm, CancellationToken ct = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.TaskItems
               .Where(ti => ti.DeletedAt != null)
               .Where(ti => ti.Title.Contains(searchTerm) || ti.Text.Contains(searchTerm))
               .Include(ti => ti.TaskList)
               .OrderByDescending(ti => ti.UpdatedAt)
               .ToListAsync(ct);
        }

        public async Task<List<TaskItem>> SearchTaskItemsAsync(string searchTerm, CancellationToken ct = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.TaskItems
                .Where(ti => ti.DeletedAt == null)
                .Where(ti => ti.Title.Contains(searchTerm) || ti.Text.Contains(searchTerm))
                .Include(ti => ti.TaskList)
                .OrderBy(ti => ti.Order)
                .ThenByDescending(ti => ti.UpdatedAt)
                .ToListAsync(ct);
        }
    }
}
