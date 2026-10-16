using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using MyNotes.Data.Models;
using MyNotes.Helpers;
using MyNotes.ViewModels;
using System.Diagnostics;
using Windows.ApplicationModel.DataTransfer;
using WinUI3Localizer;

namespace MyNotes.Views
{
    public sealed partial class PlannerPage : Page
    {
        private readonly ILocalizer _Localizer = Localizer.Get();
        // Minimum sensible column height so things don't collapse to nothing
        // if the window gets very short.
        private const double MinColumnHeight = 200;

        public PlannerViewModel ViewModel { get; set; }

        public PlannerPage()
        {
            InitializeComponent();
            ViewModel = App.GetService<PlannerViewModel>();
            DataContext = ViewModel;

            Loaded += PlannerPage_Loaded;
            SizeChanged += PlannerPage_SizeChanged;

            ToolTipService.SetToolTip(AddTaskListButton, _Localizer.GetLocalizedString("Add"));

            _ = InitializeViewModelAsync();
        }

        private async Task InitializeViewModelAsync()
        {
            await ViewModel.Initialize();
            uniformGridLayout.MaximumRowsOrColumns = ViewModel.TaskLists.Count;
            UpdateColumnHeight();
        }

        private void PlannerPage_Loaded(object sender, RoutedEventArgs e) => UpdateColumnHeight();

        private void PlannerPage_SizeChanged(object sender, SizeChangedEventArgs e) => UpdateColumnHeight();

        /// <summary>
        /// UniformGridLayout measures one reference item and applies that same
        /// height to every cell in the row rather than sizing each column to its
        /// own content — so letting columns "auto height" is inherently unreliable
        /// (whichever column grows first becomes the reference; others get clipped
        /// to that size). Instead we compute the real available height here and
        /// give every column that same, genuinely uniform, height. Each column's
        /// ListView then scrolls internally when its content overflows.
        /// </summary>
        private void UpdateColumnHeight()
        {
            var available = ActualHeight - topPanel.ActualHeight - 12 /* top margin */ - 16 /* bottom margin */;

            if (available >= MinColumnHeight)
                uniformGridLayout.MinItemHeight = available;
            else
                uniformGridLayout.MinItemHeight = MinColumnHeight;
        }

        // ── Column Reordering ────────────────────────────────────────────────

        private async void OnMoveColumnLeftClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Guid listId)
            {
                var column = ViewModel.TaskLists.FirstOrDefault(c => c.Id == listId);
                if (column == null)
                    return;

                int currentIndex = ViewModel.TaskLists.IndexOf(column);
                if (currentIndex > 0)
                {
                    await ViewModel.ReorderTaskListAsync(listId, currentIndex - 1);
                }
            }
        }

        private async void OnMoveColumnRightClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Guid listId)
            {
                var column = ViewModel.TaskLists.FirstOrDefault(c => c.Id == listId);
                if (column == null)
                    return;

                int currentIndex = ViewModel.TaskLists.IndexOf(column);
                if (currentIndex >= 0 && currentIndex < ViewModel.TaskLists.Count - 1)
                {
                    await ViewModel.ReorderTaskListAsync(listId, currentIndex + 1);
                }
            }
        }

        // ── Drag-and-drop for TaskItems ─────────────────────────────────────

        private static Guid? _draggedTaskId;

        private void OnDragItemsStarting(object sender, DragItemsStartingEventArgs e)
        {
            if (e.Items.Count > 0 && e.Items[0] is TaskItem task)
            {
                _draggedTaskId = task.Id;
                e.Data.SetText(task.Id.ToString());
                e.Data.RequestedOperation = DataPackageOperation.Move;
            }
        }

        private void OnDragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Move;
        }

        private static async Task<string?> ResolveDraggedTextAsync(DataPackageView dataView)
        {
            try
            {
                var text = await dataView.GetTextAsync();
                if (!string.IsNullOrEmpty(text))
                    return text;
            }
            catch
            {
                // Fallback to static drag tracking
            }

            if (_draggedTaskId.HasValue)
                return _draggedTaskId.Value.ToString();

            return null;
        }

        /// <summary>
        /// Calculates the insertion index in a ListView based on drop vertical coordinates.
        /// </summary>
        private static int GetDropTargetIndex(ListView lv, DragEventArgs e)
        {
            var dropPoint = e.GetPosition(lv);
            for (int i = 0; i < lv.Items.Count; i++)
            {
                if (lv.ContainerFromIndex(i) is UIElement container)
                {
                    var containerTransform = container.TransformToVisual(lv);
                    var containerBounds = containerTransform.TransformBounds(new Windows.Foundation.Rect(0, 0, container.ActualSize.X, container.ActualSize.Y));

                    if (dropPoint.Y < containerBounds.Y + (containerBounds.Height / 2))
                    {
                        return i;
                    }
                }
            }
            return lv.Items.Count;
        }

        /// <summary>
        /// Drop handler for the ListView inside a column.
        /// The ListView's Tag is the parent <see cref="TaskListViewModel"/>.
        /// </summary>
        private async void OnDrop(object sender, DragEventArgs e)
        {
            e.Handled = true;

            var text = await ResolveDraggedTextAsync(e.DataView);
            _draggedTaskId = null;

            if (string.IsNullOrEmpty(text) || !Guid.TryParse(text, out var taskId))
                return;

            TaskListViewModel? targetColumn = (sender as FrameworkElement)?.Tag as TaskListViewModel;

            if (targetColumn != null)
            {
                var lv = sender as ListView;
                int targetIndex = lv != null ? GetDropTargetIndex(lv, e) : targetColumn.Items.Count;
                await ViewModel.ReorderTaskItemAsync(taskId, targetColumn, targetIndex);
            }
        }

        /// <summary>
        /// Drop handler for the outer column background.
        /// Walks down to find the first <see cref="ListView"/> whose Tag is a column.
        /// </summary>
        private async void OnColumnDrop(object sender, DragEventArgs e)
        {
            e.Handled = true;

            var text = await ResolveDraggedTextAsync(e.DataView);
            _draggedTaskId = null;

            if (string.IsNullOrEmpty(text) || !Guid.TryParse(text, out var taskId))
                return;

            TaskListViewModel? targetColumn = (sender as FrameworkElement)?.DataContext as TaskListViewModel;

            if (targetColumn != null)
            {
                var lv = (sender as Panel)?.Children.OfType<ListView>().FirstOrDefault();
                int targetIndex = lv != null ? GetDropTargetIndex(lv, e) : targetColumn.Items.Count;
                await ViewModel.ReorderTaskItemAsync(taskId, targetColumn, targetIndex);
            }
        }

        // ── Hover-reveal action buttons ──────────────────────────────────────

        private void TaskListGrid_PointerEntered(object sender, PointerRoutedEventArgs e) =>
            SetTaskListActionButtonsVisibility(sender, Visibility.Visible);

        private void TaskListGrid_PointerExited(object sender, PointerRoutedEventArgs e) =>
            SetTaskListActionButtonsVisibility(sender, Visibility.Collapsed);

        private static void SetTaskListActionButtonsVisibility(object sender, Visibility visibility)
        {
            if (sender is not Grid grid)
                return;

            var headerBorder = grid.Children.OfType<Border>().FirstOrDefault();
            if (headerBorder?.Child is Grid headerGrid)
            {
                foreach (var panel in headerGrid.Children.OfType<StackPanel>())
                {
                    if (panel.Name == "TaskListLeftHeaderButtons" || panel.Name == "TaskListRightHeaderButtons")
                    {
                        panel.Visibility = visibility;
                    }
                }
            }
        }

        private void TaskItemGrid_PointerEntered(object sender, PointerRoutedEventArgs e) =>
            SetActionButtonsVisibility(sender, Visibility.Visible);

        private void TaskItemGrid_PointerExited(object sender, PointerRoutedEventArgs e) =>
            SetActionButtonsVisibility(sender, Visibility.Collapsed);

        private static void SetActionButtonsVisibility(object sender, Visibility visibility)
        {
            if (sender is not Grid grid)
                return;

            // DataTemplate elements live in their own namescope, so we traverse children directly
            var panel = grid.Children
                            .OfType<StackPanel>()
                            .FirstOrDefault(sp => sp.Name == "ActionButtons");
            if (panel != null)
                panel.Visibility = visibility;
        }

        // ── Task CRUD ────────────────────────────────────────────────────────

        private async void OnDeleteTaskClicked(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not TaskItem taskToDelete)
                return;

            // Find which column owns this item and remove it
            foreach (var column in ViewModel.TaskLists)
            {
                if (column.TryRemoveItem(taskToDelete))
                {
                    await ViewModel.DeleteTaskItemAsync(taskToDelete.Id);
                    Debug.WriteLine($"Deleted task '{taskToDelete.Title}' from list '{column.Name}'.");
                    break;
                }
            }
        }
        private async void OnEditTaskClicked(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not TaskItem taskToEdit)
                return;

            await ViewModel.EditTaskItemAsync(taskToEdit, XamlRoot);
        }

        private async void OnAddTaskButtonClick(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn)
                return;

            // The button's Tag is bound to the column's Id (Guid)
            if (btn.Tag is not Guid listId)
                return;

            var targetColumn = ViewModel.TaskLists.FirstOrDefault(tl => tl.Id == listId);
            if (targetColumn == null)
                return;

            await ViewModel.AddTaskItemAsync(targetColumn, XamlRoot);
        }
        private void OnEditTaskListButtonClick(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn)
                return;

            // The button's Tag is bound to the column's Id (Guid)
            if (btn.Tag is not Guid listId)
                return;

            NavigationHelper.NavigateTo<TaskListEditorPage>(listId);
        }

        private void AddTaskListButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationHelper.NavigateTo<TaskListEditorPage>();
        }
    }
}
