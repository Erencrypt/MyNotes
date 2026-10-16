namespace MyNotes.Data.Models
{
    public class TaskItem :BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public Guid? TaskListId { get; set; }
        public TaskList? TaskList { get; set; }
        public int Order { get; set; } = 0;

        // Equality by identity so HashSet/Contains work correctly
        // across different EF Core object instances for the same row
        public override bool Equals(object? obj) => obj is TaskItem other && Id == other.Id;
        public override int GetHashCode() => Id.GetHashCode();
    }
}
