using CommunityToolkit.Mvvm.ComponentModel;
using MyNotes.Models;
using System.Collections.ObjectModel;
using Windows.Storage;
using System.Text.Json;
using MyNotes.Contracts.Services;
using MyNotes.Helpers;

namespace MyNotes.ViewModels;

public partial class PlannerViewModel : ObservableRecipient
{
    private readonly StorageFolder storageFolder = App.StorageFolder;
    public ObservableCollection<TaskModel> ToDoTasks { get; set; }
    public ObservableCollection<TaskModel> InProgressTasks { get; set; }
    public ObservableCollection<TaskModel> DoneTasks { get; set; }

    public PlannerViewModel()
    {
        ToDoTasks = [];
        InProgressTasks = [];
        DoneTasks = [];
    }
    public async Task LoadDataAsync()
    {
        try
        {
            var boardData = await LoadPlannerBoardDataAsync();
            LogWriter.Log("----", LogWriter.LogLevel.Debug);
            LogWriter.Log($"Coming Data: ToDoTasks={boardData.ToDoTasks.Count}, InProgressTasks={boardData.InProgressTasks.Count}, DoneTasks={boardData.DoneTasks.Count}", LogWriter.LogLevel.Debug);
            if (boardData != null)
            {
                // Perform UI updates here
                ToDoTasks = new ObservableCollection<TaskModel>(boardData.ToDoTasks);
                InProgressTasks = new ObservableCollection<TaskModel>(boardData.InProgressTasks);
                DoneTasks = new ObservableCollection<TaskModel>(boardData.DoneTasks);

                // Notify UI that the collections have changed
                OnPropertyChanged(nameof(ToDoTasks));
                OnPropertyChanged(nameof(InProgressTasks));
                OnPropertyChanged(nameof(DoneTasks));
            }
            else
            {
                ToDoTasks = [];
                InProgressTasks = [];
                DoneTasks = [];
            }
            LogWriter.Log($"loaded Data: ToDoTasks={ToDoTasks.Count}, InProgressTasks={InProgressTasks.Count}, DoneTasks={DoneTasks.Count}", LogWriter.LogLevel.Debug);

        }
        catch (Exception ex)
        {
            LogWriter.Log($"Error loading kanban board data: {ex.Message}", LogWriter.LogLevel.Debug);
            ToDoTasks = [];
            InProgressTasks = [];
            DoneTasks = [];
        }
    }


    public TaskModel FindAndRemoveTaskById(Guid id)
    {
        foreach (var taskList in new[] { ToDoTasks, InProgressTasks, DoneTasks })
        {
            var task = taskList.FirstOrDefault(t => t.Id == id);
            if (task != null)
            {
                taskList.Remove(task);
                return task;
            }
        }
        return null!;
    }

    public async Task SavePlannerBoardDataAsync(PlannerBoardData boardData)
    {
        try
        {
            var json = JsonSerializer.Serialize(boardData);
            var file = await storageFolder.CreateFileAsync(Path.Combine("ApplicationData", "PlannerBoardData.json"), CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteTextAsync(file, json);
        }
        catch (Exception ex)
        {
            LogWriter.Log("Save board data error:" + ex.Message, LogWriter.LogLevel.Debug);
        }
    }

    public async Task<PlannerBoardData> LoadPlannerBoardDataAsync()
    {
        try
        {
            var file = await storageFolder.GetFileAsync(Path.Combine("ApplicationData", "PlannerBoardData.json"));
            var json = await FileIO.ReadTextAsync(file);
            return JsonSerializer.Deserialize<PlannerBoardData>(json)!;
        }
        catch (FileNotFoundException)
        {
            var file = await storageFolder.CreateFileAsync(Path.Combine("ApplicationData", "PlannerBoardData.json"), CreationCollisionOption.ReplaceExisting);
            PlannerBoardData boardData = new()
            {
                ToDoTasks = [],
                InProgressTasks = [],
                DoneTasks = []
            };
            var json = JsonSerializer.Serialize(boardData);
            await FileIO.WriteTextAsync(file, json);
            return boardData;
        }
    }
}
