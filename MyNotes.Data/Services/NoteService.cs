using Microsoft.EntityFrameworkCore;
using MyNotes.Data.Context;
using MyNotes.Data.Models;
using MyNotes.Data.Services.Interfaces;

namespace MyNotes.Data.Services
{
    /// <summary>
    /// Service implementation for note operations
    /// </summary>
    public class NoteService : INoteService
    {
        private readonly IDbContextFactory<AppDbContext> _contextFactory;

        public NoteService(IDbContextFactory<AppDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<List<Note>> GetAllNotesAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Notes
                .Include(n => n.Reminders)
                .OrderByDescending(n => n.UpdatedAt)
                .ToListAsync();
        }

        public async Task<List<Note>> GetActiveNotesAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var notes = await context.Notes
                .Where(n => n.DeletedAt == null)
                .Include(n => n.Reminders)
                .OrderByDescending(n => n.UpdatedAt)
                .ToListAsync();
            return notes;
        }

        public async Task<List<Note>> GetDeletedNotesAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Notes
                .IgnoreQueryFilters()
                .Where(n => n.DeletedAt != null)
                .Include(n => n.Reminders)
                .OrderByDescending(n => n.UpdatedAt)
                .ToListAsync();
        }

        public async Task<Note?> GetNoteByIdAsync(Guid id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Notes
                .Include(n => n.Reminders)
                .FirstOrDefaultAsync(n => n.Id == id);
        }

        public async Task<Note> CreateNoteAsync(Note note)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            context.Notes.Add(note);
            await context.SaveChangesAsync();
            return note;
        }

        public async Task<Note> UpdateNoteAsync(Note note)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            note.UpdatedAt = DateTime.UtcNow;
            context.Notes.Update(note);
            await context.SaveChangesAsync();
            return note;
        }

        public async Task SoftDeleteNoteAsync(Guid id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var note = await context.Notes.FindAsync(id);
            if (note != null)
            {
                note.DeletedAt = DateTime.Now;
                note.UpdatedAt = DateTime.Now;
                await context.SaveChangesAsync();
            }
        }

        public async Task RestoreNoteAsync(Guid id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var note = await context.Notes
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(n => n.Id == id && n.DeletedAt != null);

            if (note != null)
            {
                note.DeletedAt = null;
                note.UpdatedAt = DateTime.Now;
                await context.SaveChangesAsync();
            }
        }

        public async Task PermanentlyDeleteNoteAsync(Guid id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var note = await context.Notes
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(n => n.Id == id && n.DeletedAt != null);

            if (note != null)
            {
                context.Notes.Remove(note);
                await context.SaveChangesAsync();
            }
        }

        public async Task<List<Note>> SearchNotesAsync(string searchTerm, CancellationToken ct = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Notes
                .Where(n => n.DeletedAt == null)
                .Where(n => n.Title.Contains(searchTerm) || n.Content.Contains(searchTerm))
                .Include(n => n.Reminders)
                .OrderByDescending(n => n.UpdatedAt)
                .ToListAsync(ct);
        }

        public async Task<List<Note>> SearchDeletedNotesAsync(string searchTerm, CancellationToken ct = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Notes
                .Where(n => n.DeletedAt != null)
                .Where(n => n.Title.Contains(searchTerm) || n.Content.Contains(searchTerm))
                .Include(n => n.Reminders)
                .OrderByDescending(n => n.UpdatedAt)
                .ToListAsync(ct);
        }
    }
}
