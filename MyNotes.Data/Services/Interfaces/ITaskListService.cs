using MyNotes.Data.Models;

namespace MyNotes.Data.Services.Interfaces
{
    /// <summary>
    /// Service interface for task list operations
    /// </summary>
    public interface ITaskListService
    {
        Task<List<TaskList>> GetAllTaskListsAsync();
        Task<List<TaskList>> GetActiveTaskListsAsync();
        Task<List<TaskList>> GetDeletedTaskListsAsync();
        Task<TaskList?> GetTaskListByIdAsync(Guid id);
        Task<TaskList> CreateTaskListAsync(TaskList taskList);
        Task<TaskList> UpdateTaskListAsync(TaskList taskList);
        Task UpdateTaskListOrdersAsync(IEnumerable<TaskList> taskLists);
        Task SoftDeleteTaskListAsync(Guid id);
        Task RestoreTaskListAsync(Guid id);
        Task PermanentlyDeleteTaskListAsync(Guid id);
        Task<List<TaskList>> SearchTaskListsAsync(string searchTerm, CancellationToken ct = default);
        Task<List<TaskList>> SearchDeletedTaskListsAsync(string searchTerm, CancellationToken ct = default);
    }
}
