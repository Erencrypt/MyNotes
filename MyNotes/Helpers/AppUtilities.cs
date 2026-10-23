using Microsoft.Win32;

namespace MyNotes.Helpers
{
    /// <summary>
    /// Utility class for common application operations and database management
    /// </summary>
    public static class AppUtilities
    {
        /// <summary>
        /// Gets the database path
        /// </summary>
        public static string GetDatabasePath()
        {
            return AppConfig.DatabasePath;
        }

        /// <summary>
        /// Gets the connection string for the database
        /// </summary>
        public static string GetConnectionString()
        {
            return AppConfig.DatabaseConnectionString;
        }

        /// <summary>
        /// Checks if the database file exists
        /// </summary>
        /// <returns>True if database exists, false otherwise</returns>
        public static bool IsDatabaseExists()
        {
            return AppConfig.DatabaseExists();
        }

        /// <summary>
        /// Gets the formatted database file size
        /// </summary>
        /// <returns>Formatted file size string (e.g., "1.5 MB")</returns>
        public static string GetDatabaseSize()
        {
            return AppConfig.GetFormattedDatabaseSize();
        }

        /// <summary>
        /// Gets the last modified date of the database file
        /// </summary>
        /// <returns>Last modified date, or null if file doesn't exist</returns>
        public static DateTime? GetDatabaseLastModified()
        {
            return AppConfig.GetDatabaseLastModified();
        }

        /// <summary>
        /// Gets the MyNotes folder path
        /// </summary>
        public static string GetMyNotesFolderPath()
        {
            return AppConfig.MyNotesFolderPath;
        }

        /// <summary>
        /// Gets the local application data path
        /// </summary>
        public static string GetLocalAppDataPath()
        {
            return AppConfig.LocalAppDataPath;
        }

        /// <summary>
        /// Gets application information
        /// </summary>
        /// <returns>Tuple containing app name and version</returns>
        public static (string Name, string Version) GetAppInfo()
        {
            return (AppConfig.AppName, AppConfig.AppVersion);
        }

        /// <summary>
        /// Gets database information
        /// </summary>
        /// <returns>Tuple containing database path, size, and last modified date</returns>
        public static (string Path, string Size, DateTime? LastModified) GetDatabaseInfo()
        {
            return (
                AppConfig.DatabasePath,
                AppConfig.GetFormattedDatabaseSize(),
                AppConfig.GetDatabaseLastModified()
            );
        }

        /// <summary>
        /// Validates that the database is accessible
        /// </summary>
        /// <returns>True if database is accessible, false otherwise</returns>
        public static bool ValidateDatabaseAccess()
        {
            try
            {
                AppConfig.EnsureMyNotesDirectoryExists();
                return File.Exists(AppConfig.DatabasePath) || Directory.Exists(AppConfig.MyNotesFolderPath);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the startup registry key path
        /// </summary>
        public static string GetStartupRegistryKey()
        {
            return AppConfig.StartupRegistryKey;
        }

        /// <summary>
        /// Gets the startup registry value name for MyNotes
        /// </summary>
        public static string GetStartupRegistryValueName()
        {
            return AppConfig.StartupRegistryValueName;
        }

        /// <summary>
        /// Checks if a startup registry entry exists for the current user.
        /// </summary>
        public static bool StartupEntryExists(string valueName)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(AppConfig.StartupRegistryKey, false);
                return key?.GetValue(valueName) != null;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Creates or updates a startup registry entry for the current user.
        /// </summary>
        public static void SetStartupEntry(string valueName, string executablePath)
        {
            using var key = Registry.CurrentUser.OpenSubKey(AppConfig.StartupRegistryKey, true);
            if (key is null)
            {
                throw new InvalidOperationException("The current user's Windows startup registry key is unavailable.");
            }

            key.SetValue(valueName, $"\"{executablePath}\"");
        }

        /// <summary>
        /// Removes a startup registry entry for the current user.
        /// </summary>
        public static void RemoveStartupEntry(string valueName)
        {
            using var key = Registry.CurrentUser.OpenSubKey(AppConfig.StartupRegistryKey, true);
            key?.DeleteValue(valueName, false);
        }

        /// <summary>
        /// Gets the current process executable path.
        /// </summary>
        public static string? GetProcessPath()
        {
            return Environment.ProcessPath;
        }
    }
}
