using Microsoft.Win32;
using MyNotes.Data;

namespace MyNotes.Helpers
{
    /// <summary>
    /// Centralized configuration class for application-wide settings and paths
    /// </summary>
    public static class AppConfig
    {
        #region Paths

        /// <summary>
        /// Gets the local application data path
        /// </summary>
        public static string LocalAppDataPath => Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        /// <summary>
        /// Gets the MyNotes folder path in LocalAppData
        /// </summary>
        public static string MyNotesFolderPath => DatabaseLocations.FolderPath;

        /// <summary>
        /// Gets the full path to the SQLite database file
        /// </summary>
        public static string DatabasePath => DatabaseLocations.DatabasePath;

        /// <summary>
        /// Gets the connection string for the SQLite database
        /// </summary>
        public static string DatabaseConnectionString => DatabaseLocations.ConnectionString;

        #endregion

        #region Application Settings

        /// <summary>
        /// Application name
        /// </summary>
        public static string AppName => "MyNotes";

        /// <summary>
        /// Application version
        /// </summary>
        public static string AppVersion =>
            typeof(AppConfig).Assembly.GetName().Version?.ToString()
            ?? throw new InvalidOperationException("The application assembly version is unavailable.");

        /// <summary>
        /// Database file name
        /// </summary>
        public static string DatabaseFileName => "MyNotes.db";

        #endregion

        #region Helper Methods

        /// <summary>
        /// Ensures the MyNotes directory exists
        /// </summary>
        public static void EnsureMyNotesDirectoryExists()
        {
            if (!Directory.Exists(MyNotesFolderPath))
            {
                Directory.CreateDirectory(MyNotesFolderPath);
            }
        }

        /// <summary>
        /// Checks if the database file exists
        /// </summary>
        /// <returns>True if the database file exists, false otherwise</returns>
        public static bool DatabaseExists()
        {
            return File.Exists(DatabasePath);
        }

        /// <summary>
        /// Gets the size of the database file in bytes
        /// </summary>
        /// <returns>Database file size in bytes, or 0 if file doesn't exist</returns>
        public static long GetDatabaseSize()
        {
            if (DatabaseExists())
            {
                var fileInfo = new FileInfo(DatabasePath);
                return fileInfo.Length;
            }
            return 0;
        }

        /// <summary>
        /// Gets the last modified date of the database file
        /// </summary>
        /// <returns>Last modified date, or null if file doesn't exist</returns>
        public static DateTime? GetDatabaseLastModified()
        {
            if (DatabaseExists())
            {
                var fileInfo = new FileInfo(DatabasePath);
                return fileInfo.LastWriteTime;
            }
            return null;
        }

        /// <summary>
        /// Gets a formatted string of the database file size
        /// </summary>
        /// <returns>Formatted file size string (e.g., "1.5 MB")</returns>
        public static string GetFormattedDatabaseSize()
        {
            var size = GetDatabaseSize();
            if (size == 0) return "0 B";

            string[] sizes = ["B", "KB", "MB", "GB"];
            int order = 0;
            double sizeInBytes = size;

            while (sizeInBytes >= 1024 && order < sizes.Length - 1)
            {
                order++;
                sizeInBytes /= 1024;
            }

            return $"{sizeInBytes:0.##} {sizes[order]}";
        }

        #endregion

        #region Registry Keys (for startup settings)

        /// <summary>
        /// Represents the registry key used to store and retrieve application startup settings for the current user.
        /// </summary>
        /// <remarks>This key is opened with write access and corresponds to the path specified by <see
        /// cref="AppConfig.StartupRegistryKey"/>. Modifications to this key affect only the current user's registry
        /// hive.</remarks>
        private static readonly RegistryKey RegKey = Registry.CurrentUser.OpenSubKey(AppConfig.StartupRegistryKey, true)!;

        /// <summary>
        /// Registry key for Windows startup programs
        /// </summary>
        public static string StartupRegistryKey => @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

        /// <summary>
        /// Registry value name for MyNotes startup entry
        /// </summary>
        public static string StartupRegistryValueName => "MyNotes";

        /// <summary>
        /// Startup task identifier for packaged MSIX execution.
        /// </summary>
        public static string StartupTaskId => "8163264128256";

        #endregion
    }
}
