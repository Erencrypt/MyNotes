using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MyNotes.Data.Context
{
    /// <summary>
    /// EF tools use MyNotes.Data as startup project so this assembly loads without WinUI/WinApp SDK module initialization.
    /// </summary>
    public sealed class AppDbContextDesignTimeFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseSqlite(DatabaseLocations.ConnectionString);
            return new AppDbContext(optionsBuilder.Options);
        }
    }
}
