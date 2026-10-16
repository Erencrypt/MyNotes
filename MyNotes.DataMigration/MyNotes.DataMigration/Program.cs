using MyNotes.DataMigration;

var cleanupOnly = args.Contains("--cleanup-only", StringComparer.OrdinalIgnoreCase);
if (!args.Contains("--yes", StringComparer.OrdinalIgnoreCase))
{
    Console.WriteLine(cleanupOnly
        ? "This will delete legacy files only, without importing them."
        : "This will import legacy data from the MyNotes LocalAppData folder, then delete imported files.");
    Console.Write("Continue? [y/N] ");

    if (!string.Equals(Console.ReadLine()?.Trim(), "y", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine("Migration cancelled.");
        return;
    }
}

try
{
    var migration = new LegacyDataMigrationService();
    if (cleanupOnly)
    {
        var deleted = migration.CleanupLegacyFilesOnly();
        Console.WriteLine($"Legacy files deleted: {deleted}");
        return;
    }

    var result = await migration.MigrateAsync();

    Console.WriteLine("Legacy data migration completed.");
    Console.WriteLine($"Task items imported: {result.ImportedTaskItems}");
    Console.WriteLine($"Notes imported: {result.ImportedNotes}");
    Console.WriteLine($"Reminders imported: {result.ImportedReminders}");
    Console.WriteLine($"Legacy files deleted: {result.DeletedLegacyFiles}");
    return;
}
catch (Exception exception)
{
    Console.Error.WriteLine($"Migration failed: {exception.Message}");
    Environment.ExitCode = 1;
}
