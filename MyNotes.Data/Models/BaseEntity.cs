namespace MyNotes.Data.Models
{
    /// <summary>
    /// Base entity class with common properties for all entities
    /// </summary>
    public abstract class BaseEntity
    {
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        public DateTime? DeletedAt { get; set; }
        public bool IsDeleted => DeletedAt.HasValue;
    }
}
