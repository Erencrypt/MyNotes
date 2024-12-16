namespace MyNotes.Models
{
    public class PlannerBoardData
    {
        public required List<TaskModel> ToDoTasks { get; set; }
        public required List<TaskModel> InProgressTasks { get; set; }
        public required List<TaskModel> DoneTasks { get; set; }
    }
}
