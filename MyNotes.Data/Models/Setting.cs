namespace MyNotes.Data.Models
{
    /// <summary>
    /// Represents an application setting
    /// </summary>
    public class Setting : BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }
}
