using CommunityToolkit.Mvvm.ComponentModel;
using MyNotes.Data.Models;
using System.Collections.ObjectModel;

namespace MyNotes.ViewModels
{
    /// <summary>
    /// Wraps a <see cref="TaskList"/> and exposes its items as an
    /// <see cref="ObservableCollection{T}"/> so each column on the Planner
    /// board can bind to its own live collection — equivalent to the old
    /// per-status collections (ToDoTasks, InProgressTasks, DoneTasks) but
    /// fully dynamic.
    /// </summary>
    public partial class TaskListViewModel : ObservableObject
    {
        [ObservableProperty]
        private TaskList _taskList;

        public ObservableCollection<TaskItem> Items { get; } = [];

        public TaskListViewModel(TaskList taskList)
        {
            _taskList = taskList;

            foreach (var item in taskList.TaskItems
                         .Where(ti => ti.DeletedAt == null)
                         .OrderBy(ti => ti.Order)
                         .ThenByDescending(ti => ti.UpdatedAt))
            {
                Items.Add(item);
            }
        }

        // Convenience pass-throughs used by XAML bindings
        public Guid Id => TaskList.Id;
        public string Name => TaskList.Name;
        public string HeaderColor => TaskList.HeaderColor;
        public int Order { get => TaskList.Order; set => TaskList.Order = value; }

        /// <summary>
        /// Updates the Order property of each item to match its index in the collection.
        /// </summary>
        public void UpdateItemOrders()
        {
            for (int i = 0; i < Items.Count; i++)
            {
                Items[i].Order = i;
            }
        }

        /// <summary>
        /// Inserts an item into this collection at the specified target index and updates all order indices.
        /// </summary>
        public void InsertItem(int index, TaskItem item)
        {
            if (index < 0 || index > Items.Count)
                index = Items.Count;

            item.TaskListId = Id;
            Items.Insert(index, item);
            UpdateItemOrders();
        }

        /// <summary>
        /// Removes the item from this collection (used during drag-drop).
        /// Returns true when the item was found and removed.
        /// </summary>
        public bool TryRemoveItem(TaskItem item)
        {
            var removed = Items.Remove(item);
            if (removed)
            {
                UpdateItemOrders();
            }
            return removed;
        }

        /// <summary>
        /// Finds a task item by id and removes it from this collection.
        /// Returns the item if found, otherwise null.
        /// </summary>
        public TaskItem? FindAndRemove(Guid itemId)
        {
            var item = Items.FirstOrDefault(i => i.Id == itemId);
            if (item != null)
            {
                Items.Remove(item);
                UpdateItemOrders();
            }
            return item;
        }

        /// <summary>
        /// Forces the ListView to re-render the given item by replacing it
        /// at the same index. Required because <see cref="TaskItem"/> does not
        /// implement <see cref="System.ComponentModel.INotifyPropertyChanged"/>.
        /// </summary>
        public void RefreshItem(TaskItem item)
        {
            var index = Items.IndexOf(item);
            if (index < 0)
                return;
            Items.RemoveAt(index);
            Items.Insert(index, item);
        }
    }
}
