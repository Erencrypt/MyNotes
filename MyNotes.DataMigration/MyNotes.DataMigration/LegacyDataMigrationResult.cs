namespace MyNotes.DataMigration
{
    public sealed record LegacyDataMigrationResult(
        int ImportedTaskItems,
        int ImportedNotes,
        int ImportedReminders,
        int DeletedLegacyFiles);
}
