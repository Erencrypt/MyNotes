using MyNotes.Data.Models;

namespace MyNotes.Data.Services.Interfaces
{
    /// <summary>
    /// Service interface for task item operations
    /// </summary>
    public interface ITaskItemService
    {
        Task<List<TaskItem>> GetAllTaskItemsAsync();
        Task<List<TaskItem>> GetActiveTaskItemsAsync();
        Task<List<TaskItem>> GetDeletedTaskItemsAsync();
        Task<TaskItem?> GetTaskItemByIdAsync(Guid id);
        Task<TaskItem> CreateTaskItemAsync(TaskItem taskItem);
        Task<TaskItem> UpdateTaskItemAsync(TaskItem taskItem);
        Task UpdateTaskItemOrdersAsync(IEnumerable<TaskItem> items);
        Task SoftDeleteTaskItemAsync(Guid id);
        Task RestoreTaskItemAsync(Guid id);
        Task PermanentlyDeleteTaskItemAsync(Guid id);
        Task<List<TaskItem>> SearchTaskItemsAsync(string searchTerm, CancellationToken ct = default);
        Task<List<TaskItem>> SearchDeletedTaskItemsAsync(string searchTerm, CancellationToken ct = default);
    }
}
