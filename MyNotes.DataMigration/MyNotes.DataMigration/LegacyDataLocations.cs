namespace MyNotes.DataMigration
{
    internal static class LegacyDataLocations
    {
        public static string FolderPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MyNotes");

        public static string DatabasePath => Path.Combine(FolderPath, "MyNotes.db");
        public static string ApplicationDataPath => Path.Combine(FolderPath, "ApplicationData");
        public static string NotesPath => Path.Combine(FolderPath, "Notes");
        public static string RemindersPath => Path.Combine(FolderPath, "Reminders");
        public static string TrashPath => Path.Combine(FolderPath, "Trash");
    }
}
