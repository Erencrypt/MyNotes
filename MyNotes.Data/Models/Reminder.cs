namespace MyNotes.Data.Models
{
    /// <summary>
    /// Represents a reminder for a note
    /// </summary>
    public class Reminder : BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string ReminderHeader { get; set; } = string.Empty;
        public string ReminderText { get; set; } = string.Empty;
        public DateTime ReminderDateTime { get; set; }
        public bool Repeat { get; set; }
        public bool Alarm { get; set; }
        public bool IsActive { get; set; } = true;

        public Guid? NoteId { get; set; }
        public Note? Note { get; set; }

        public string IconGlyph => Repeat ? "\uE823" : "\uEC92";

        // Equality by identity so HashSet/Contains work correctly
        // across different EF Core object instances for the same row
        public override bool Equals(object? obj) => obj is Reminder other && Id == other.Id;
        public override int GetHashCode() => Id.GetHashCode();

    }
}
