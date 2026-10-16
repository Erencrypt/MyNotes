using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MyNotes.Data.Models;
using MyNotes.Data.Services.Interfaces;
using MyNotes.Views.Dialogs;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace MyNotes.ViewModels
{
    public partial class PlannerViewModel : BaseViewModel
    {
        private readonly ITaskListService _taskListService;
        private readonly ITaskItemService _taskItemService;

        /// <summary>
        /// The dynamic collection of task list columns.
        /// Each <see cref="TaskListViewModel"/> wraps one <see cref="TaskList"/>
        /// and owns an <see cref="ObservableCollection{TaskItem}"/> for its rows.
        /// </summary>
        public ObservableCollection<TaskListViewModel> TaskLists { get; } = [];

        public PlannerViewModel(ITaskListService taskListService, ITaskItemService taskItemService)
        {
            _taskListService = taskListService;
            _taskItemService = taskItemService;
        }

        public override async Task Initialize()
        {
            await LoadTaskListsAsync();
        }

        private async Task LoadTaskListsAsync()
        {
            IsLoading = true;
            try
            {
                // GetAllTaskListsAsync already does .Include(tl => tl.TaskItems)
                var lists = await _taskListService.GetAllTaskListsAsync();

                TaskLists.Clear();
                foreach (var list in lists.Where(tl => tl.DeletedAt == null))
                {
                    TaskLists.Add(new TaskListViewModel(list));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading task lists: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Searches all columns for a task item by id, removes it from its
        /// current column, and returns it. Returns null if not found.
        /// Mirrors the old <c>FindAndRemoveTaskById</c> helper.
        /// </summary>
        public TaskItem? FindAndRemoveTaskById(Guid itemId)
        {
            foreach (var column in TaskLists)
            {
                var item = column.FindAndRemove(itemId);
                if (item != null)
                    return item;
            }
            return null;
        }

        /// <summary>
        /// Updates a task item in the database.
        /// </summary>
        public async Task UpdateTaskItemAsync(TaskItem task)
        {
            try
            {
                await _taskItemService.UpdateTaskItemAsync(task);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating task item: {ex.Message}");
            }
        }

        /// <summary>
        /// Deletes a task item from the database.
        /// </summary>
        public async Task DeleteTaskItemAsync(Guid taskId)
        {
            try
            {
                await _taskItemService.SoftDeleteTaskItemAsync(taskId);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error deleting task item: {ex.Message}");
            }
        }

        /// <summary>
        /// Creates a new task item in the database.
        /// </summary>
        public async Task<TaskItem?> CreateTaskItemAsync(TaskItem task)
        {
            try
            {
                return await _taskItemService.CreateTaskItemAsync(task);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error creating task item: {ex.Message}");
                return null;
            }
        }

        public async Task AddTaskItemAsync(TaskListViewModel targetColumn, XamlRoot xamlRoot)
        {
            AddTaskDialog taskDialog = new() { XamlRoot = xamlRoot };
            var result = await taskDialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                var newTask = new TaskItem
                {
                    Title = taskDialog.TaskTitle,
                    Text = taskDialog.TaskText,
                    TaskListId = targetColumn.Id,
                    Order = targetColumn.Items.Count
                };

                var createdTask = await CreateTaskItemAsync(newTask);
                if (createdTask != null)
                {
                    targetColumn.Items.Add(createdTask);
                    targetColumn.UpdateItemOrders();
                    Debug.WriteLine($"Task '{createdTask.Title}' added to list '{targetColumn.Name}'.");
                }
            }
        }

        /// <summary>
        /// Moves a task to a target column at targetIndex and persists the updated order of items in affected columns.
        /// </summary>
        public async Task ReorderTaskItemAsync(Guid taskId, TaskListViewModel targetColumn, int targetIndex)
        {
            TaskListViewModel? sourceColumn = TaskLists.FirstOrDefault(col => col.Items.Any(i => i.Id == taskId));
            int oldIndex = sourceColumn?.Items.Select((item, idx) => (item, idx)).FirstOrDefault(x => x.item.Id == taskId).idx ?? -1;

            TaskItem? task = sourceColumn?.FindAndRemove(taskId);

            if (task == null)
            {
                task = FindAndRemoveTaskById(taskId);
            }

            if (task == null)
                return;

            if (sourceColumn == targetColumn && oldIndex >= 0 && targetIndex > oldIndex)
            {
                targetIndex--;
            }

            targetColumn.InsertItem(targetIndex, task);

            List<TaskListViewModel> affectedColumns = [targetColumn];
            if (sourceColumn != null && sourceColumn != targetColumn && !affectedColumns.Contains(sourceColumn))
            {
                affectedColumns.Add(sourceColumn);
            }

            var itemsToUpdate = affectedColumns.SelectMany(col => col.Items);
            try
            {
                await _taskItemService.UpdateTaskItemOrdersAsync(itemsToUpdate);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving task order: {ex.Message}");
            }
        }

        /// <summary>
        /// Moves a TaskList column to targetIndex in the TaskLists collection and persists the new order to the database.
        /// </summary>
        public async Task ReorderTaskListAsync(Guid listId, int targetIndex)
        {
            var column = TaskLists.FirstOrDefault(c => c.Id == listId);
            if (column == null)
                return;

            int oldIndex = TaskLists.IndexOf(column);
            if (oldIndex < 0)
                return;

            if (targetIndex < 0 || targetIndex >= TaskLists.Count)
                targetIndex = TaskLists.Count - 1;

            if (oldIndex == targetIndex)
                return;

            TaskLists.Move(oldIndex, targetIndex);

            for (int i = 0; i < TaskLists.Count; i++)
            {
                TaskLists[i].Order = i;
            }

            try
            {
                await _taskListService.UpdateTaskListOrdersAsync(TaskLists.Select(vm => vm.TaskList));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving task list order: {ex.Message}");
            }
        }

        /// <summary>
        /// Opens an edit dialog pre-populated with the given task's current values.
        /// On confirmation, updates the in-memory item and persists to the database.
        /// </summary>
        public async Task EditTaskItemAsync(TaskItem task, XamlRoot xamlRoot)
        {
            EditTaskDialog editDialog = new(task) { XamlRoot = xamlRoot };
            var result = await editDialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                task.Title = editDialog.TaskTitle;
                task.Text = editDialog.TaskText;
                await UpdateTaskItemAsync(task);

                // TaskItem has no INotifyPropertyChanged, so force the ListView
                // to re-render the row by replacing the item in its collection.
                var owningColumn = TaskLists.FirstOrDefault(col => col.Items.Contains(task));
                owningColumn?.RefreshItem(task);
            }
        }
    }
}
