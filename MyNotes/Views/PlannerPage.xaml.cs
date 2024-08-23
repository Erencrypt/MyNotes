using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using MyNotes.Models;
using MyNotes.ViewModels;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Windows.ApplicationModel.DataTransfer;

namespace MyNotes.Views;

public sealed partial class PlannerPage : Page
{
    public PlannerViewModel ViewModel { get; set; }

    public PlannerPage()
    {
        InitializeComponent();
        ViewModel = App.GetService<PlannerViewModel>();
        DataContext = ViewModel;
        ViewModel.LoadDataAsync().ConfigureAwait(false);
    }

    private void OnDragItemsStarting(object sender, DragItemsStartingEventArgs e)
    {
        if (e.Items[0] is TaskModel task)
        {
            e.Data.SetText(task.Id.ToString());
            e.Data.RequestedOperation = DataPackageOperation.Move;
        }
    }

    private void OnDragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = DataPackageOperation.Move;
    }

    private async void OnDrop(object sender, DragEventArgs e)
    {
        if (e.DataView.Contains(StandardDataFormats.Text))
        {
            var taskIdString = await e.DataView.GetTextAsync();
            if (Guid.TryParse(taskIdString, out var taskId))
            {
                ListView targetListView = null!;

                if (sender is StackPanel stackPanel)
                {
                    targetListView = stackPanel.Children.OfType<ListView>().FirstOrDefault()!;
                }
                else if (sender is ListView lv)
                {
                    targetListView = lv;
                }

                if (targetListView != null)
                {
                    var targetCollection = targetListView.ItemsSource as ObservableCollection<TaskModel>;

                    var task = ViewModel.FindAndRemoveTaskById(taskId);

                    if (task != null)
                    {
                        targetCollection.Add(task);

                        task.Status = targetListView.Name;

                        await ViewModel.SavePlannerBoardDataAsync(new PlannerBoardData
                        {
                            ToDoTasks = ViewModel.ToDoTasks.ToList(),
                            InProgressTasks = ViewModel.InProgressTasks.ToList(),
                            DoneTasks = ViewModel.DoneTasks.ToList()
                        });
                    }
                }
            }
        }
    }

    private async void OnDeleteTaskClicked(object sender, RoutedEventArgs e)
    {
        var button = sender as Button;
        var taskToDelete = button?.DataContext as TaskModel;

        if (taskToDelete != null)
        {
            ObservableCollection<TaskModel> targetCollection = null;

            switch (taskToDelete.Status)
            {
                case "ToDoListView":
                    targetCollection = ViewModel.ToDoTasks;
                    break;
                case "InProgressListView":
                    targetCollection = ViewModel.InProgressTasks;
                    break;
                case "DoneListView":
                    targetCollection = ViewModel.DoneTasks;
                    break;
            }

            if (targetCollection != null && targetCollection.Contains(taskToDelete))
            {
                targetCollection.Remove(taskToDelete);

                await ViewModel.SavePlannerBoardDataAsync(new PlannerBoardData
                {
                    ToDoTasks = ViewModel.ToDoTasks.ToList(),
                    InProgressTasks = ViewModel.InProgressTasks.ToList(),
                    DoneTasks = ViewModel.DoneTasks.ToList()
                });
            }
            else
            {
                Debug.WriteLine("Task not found in the collection.");
            }
        }
    }

    private async void OnAddTaskButtonClick(object sender, RoutedEventArgs e)
    {
        Button button = (Button)sender;
        string stat = string.Empty;
        var tasks = ViewModel.ToDoTasks;

        switch (button.Name)
        {
            case "ToDoButton":
                stat = "ToDoListView";
                break;
            case "InProgressButton":
                stat = "InProgressListView";
                tasks = ViewModel.InProgressTasks;
                break;
            case "DoneButton":
                stat = "DoneListView";
                tasks = ViewModel.DoneTasks;
                break;
        }
        AddTaskDialog taskDialog = new()
        {
            XamlRoot = XamlRoot
        };
        var result = await taskDialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            var newTask = new TaskModel
            {
                Title = taskDialog.TaskTitle,
                Text = taskDialog.TaskText,
                Status = stat
            };

            tasks.Add(newTask);

            await ViewModel.SavePlannerBoardDataAsync(new PlannerBoardData
            {
                ToDoTasks = ViewModel.ToDoTasks.ToList(),
                InProgressTasks = ViewModel.InProgressTasks.ToList(),
                DoneTasks = ViewModel.DoneTasks.ToList()
            });
        }
    }

    private void TextBlock_Tapped(object sender, TappedRoutedEventArgs e)
    {
        TextBlock block = (TextBlock)sender;

        switch (block.Name)
        {
            case "ToDoText":
                ToDoListView.SelectedItem = null;
                break;
            case "InProgressText":
                InProgressListView.SelectedItem = null;
                break;
            case "DoneText":
                DoneListView.SelectedItem = null;
                break;
        }
    }
}
