namespace MyNotes.Models
{
    public class PlannerBoardData
    {
        public List<TaskModel> ToDoTasks { get; set; }
        public List<TaskModel> InProgressTasks { get; set; }
        public List<TaskModel> DoneTasks { get; set; }
    }
}
