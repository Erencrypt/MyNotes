namespace MyNotes.Data.Models
{
    /// <summary>
    /// Represents a note in the application
    /// </summary>
    public class Note : BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public List<Reminder>? Reminders { get; set; }

        public bool HasReminders => Reminders != null && Reminders.Count > 0;

        // Equality by identity so HashSet/Contains work correctly
        // across different EF Core object instances for the same row
        public override bool Equals(object? obj) => obj is Note other && Id == other.Id;
        public override int GetHashCode() => Id.GetHashCode();
    }
}
