namespace MyNotes.Data.Services.Interfaces
{
    /// <summary>
    /// Service interface for settings operations
    /// </summary>
    public interface ISettingService
    {
        Task<T?> GetSettingAsync<T>(string key, T? defaultValue = default);
        Task SetSettingAsync<T>(string key, T value);
        Task RemoveSettingAsync(string key);
        Task<bool> HasSettingAsync(string key);
    }
    public class DataKeys
    {
        public static string SpellCheckEnabled = "SpellCheckEnabled";
        public static string SaveWhenExit = "SaveWhenExit";
        public static string Theme = "Theme";
        public static string Backdrop = "Backdrop";
        public static string HourFormat = "TimeFormat";
        public static string Language = "Language";
        public static string AlarmSoundLooping = "AlarmSoundLooping";
        public static string ReminderNotificationPriority = "ReminderNotificationPriority";
        public static string TrashRetentionDays = "TrashRetentionDays";
    }
}
