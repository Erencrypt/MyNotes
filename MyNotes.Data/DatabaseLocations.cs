namespace MyNotes.Data
{
    /// <summary>
    /// Database paths shared by the EF layer and the app (single source for LocalAppData\MyNotes\MyNotes.db).
    /// </summary>
    public static class DatabaseLocations
    {
        public static string FolderPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MyNotes");

        public static string DatabasePath => Path.Combine(FolderPath, "MyNotes.db");

        public static string ConnectionString => $"Data Source={DatabasePath}";
    }
}
