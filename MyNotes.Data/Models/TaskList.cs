namespace MyNotes.Data.Models
{
    public class TaskList : BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string HeaderColor { get; set; } = "#6C757D"; // Default to muted gray (hex — usable in XAML)
        public int Order { get; set; } = 0;
        public List<TaskItem> TaskItems{ get; set; } = [];

        public bool HasTaskItems => TaskItems != null && TaskItems.Count > 0;
        public int DeletedTaskItemsCount => DeletedAt.HasValue
            ? TaskItems.Count(item => item.DeletedAt.HasValue && item.DeletedAt.Value >= DeletedAt.Value)
            : 0;

        // Equality by identity so HashSet/Contains work correctly
        // across different EF Core object instances for the same row
        public override bool Equals(object? obj) => obj is TaskList other && Id == other.Id;
        public override int GetHashCode() => Id.GetHashCode();
    }
}
