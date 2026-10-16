using MyNotes.Data.Models;
using MyNotes.Data.Services.Interfaces;
using System.Diagnostics;

namespace MyNotes.ViewModels
{
    /// <summary>
    /// ViewModel for the Task List editor page
    /// </summary>
    public partial class TaskListEditorViewModel : BaseViewModel
    {
        private readonly ITaskListService _taskListService;
        public TaskListEditorViewModel(ITaskListService taskListService)
        {
            _taskListService = taskListService;
        }

        private Guid _taskListId;
        private string _taskListName = string.Empty;
        private string _taskListHeaderColor = "#FF6C757D";
        private TaskList? _editingTaskList;

        public Guid TaskListId
        {
            get => _taskListId;
            set => SetProperty(ref _taskListId, value);
        }

        public string TaskListName
        {
            get => _taskListName;
            set => SetProperty(ref _taskListName, value);
        }

        public string TaskListHeaderColor
        {
            get => _taskListHeaderColor;
            set => SetProperty(ref _taskListHeaderColor, value);
        }

        /// <summary>
        /// Persists the task list. If <see cref="TaskListId"/> is set (edit mode)
        /// the existing record is updated; otherwise a new one is created.
        /// Falls back to the default hex color if <paramref name="headerColor"/>
        /// is null or empty.
        /// </summary>
        /// <returns>The saved entity, or <c>null</c> on failure.</returns>
        public async Task<TaskList?> SaveAsync(string name, string headerColor)
        {
            var resolvedColor = string.IsNullOrWhiteSpace(headerColor) ? "#6C757D" : headerColor;

            try
            {
                // Edit mode — mutate the already-tracked entity in-place
                if (TaskListId != Guid.Empty && _editingTaskList is not null)
                {
                    _editingTaskList.Name = name;
                    _editingTaskList.HeaderColor = resolvedColor;

                    return await _taskListService.UpdateTaskListAsync(_editingTaskList);
                }

                // Create mode — insert a new record
                var activeLists = await _taskListService.GetActiveTaskListsAsync();
                var newTaskList = new TaskList
                {
                    Name = name,
                    HeaderColor = resolvedColor,
                    Order = activeLists.Count
                };

                return await _taskListService.CreateTaskListAsync(newTaskList);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error creating task list: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Soft-deletes the task list and its active items.
        /// </summary>
        public async Task DeleteAsync()
        {
            if (TaskListId == Guid.Empty)
                throw new InvalidOperationException("A task list must be loaded before it can be deleted.");

            await _taskListService.SoftDeleteTaskListAsync(TaskListId);
        }

        /// <summary>
        /// Loads an existing <see cref="TaskList"/> by <paramref name="id"/> so the
        /// editor can be pre-populated for editing.
        /// </summary>
        public async Task LoadAsync(Guid id)
        {
            try
            {
                var taskList = await _taskListService.GetTaskListByIdAsync(id);
                if (taskList is null)
                    return;

                _editingTaskList = taskList;   // keep the tracked instance
                TaskListId = taskList.Id;
                TaskListName = taskList.Name;
                TaskListHeaderColor = taskList.HeaderColor;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading task list: {ex.Message}");
            }
        }

    }
}
