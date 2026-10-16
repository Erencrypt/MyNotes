using MyNotes.Data.Models;

namespace MyNotes.Data.Services.Interfaces
{
    /// <summary>
    /// Service interface for note operations
    /// </summary>
    public interface INoteService
    {
        Task<List<Note>> GetAllNotesAsync();
        Task<List<Note>> GetActiveNotesAsync();
        Task<List<Note>> GetDeletedNotesAsync();
        Task<Note?> GetNoteByIdAsync(Guid id);
        Task<Note> CreateNoteAsync(Note note);
        Task<Note> UpdateNoteAsync(Note note);
        Task SoftDeleteNoteAsync(Guid id);
        Task RestoreNoteAsync(Guid id);
        Task PermanentlyDeleteNoteAsync(Guid id);
        Task<List<Note>> SearchNotesAsync(string searchTerm, CancellationToken ct = default);
        Task<List<Note>> SearchDeletedNotesAsync(string searchTerm, CancellationToken ct = default);
    }
}
