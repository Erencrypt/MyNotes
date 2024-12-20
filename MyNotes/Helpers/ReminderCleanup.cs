using MyNotes.Contracts.Services;
using MyNotes.Models;
using System.Text;
using System.Text.Json;
using Windows.Storage;

namespace MyNotes.Helpers
{
    public class ReminderCleanup
    {
        private readonly IAppNotificationService notificationService;
        private int DeletedCount;
        private int AddedCount;
        public ReminderCleanup()
        {
            notificationService = App.GetService<IAppNotificationService>();
        }

        public async Task Clean(bool isStartup)
        {
            try
            {
                DeletedCount = 0;
                AddedCount = 0;
                await App.StorageFolder.CreateFolderAsync("Reminders", CreationCollisionOption.OpenIfExists);
                DirectoryInfo dinfo = new(Path.Combine(App.StorageFolder.Path,"Reminders"));
                FileInfo[] Files = dinfo.GetFiles("*.json");
                List<FileInfo> orderedList = Files.OrderByDescending(x => x.CreationTime).ToList();
                string fullPath;
                MoveFile moveFile = new();

                foreach (FileInfo file in orderedList)
                {
                    fullPath = Path.Combine(dinfo.FullName, file.Name);
                    string readText = string.Empty;

                    try
                    {
                        readText = File.ReadAllText(fullPath, Encoding.UTF8);
                    }
                    catch (Exception readEx)
                    {
                        LogWriter.Log($"Error reading file {fullPath}: {readEx.Message}", LogWriter.LogLevel.Error);
                        continue;
                    }

                    if (string.IsNullOrEmpty(readText))
                    {
                        LogWriter.Log($"Empty or invalid file {file.Name}", LogWriter.LogLevel.Warning);
                        continue;
                    }

                    Reminder readedReminder;
                    try
                    {
                        readedReminder = JsonSerializer.Deserialize<Reminder>(readText, App.JsonOptions)!;
                    }
                    catch (Exception deserializationEx)
                    {
                        LogWriter.Log($"Error deserializing file {fullPath}: {deserializationEx.Message}", LogWriter.LogLevel.Error);
                        continue;
                    }

                    DateTime t = Convert.ToDateTime(readedReminder.DateTime);
                    if (readedReminder.Repeat)
                    {
                        if (t.TimeOfDay > DateTime.Now.TimeOfDay)
                        {
                            App.Reminders.Add(readedReminder);
                            AddedCount++;
                        }
                    }
                    else if (!readedReminder.Repeat)
                    {
                        if (t.Date == DateTime.Now.Date)
                        {
                            if (t.TimeOfDay < DateTime.Now.TimeOfDay)
                            {
                                moveFile.Move("Reminders", "Trash", file.Name[..^5], root: null!);
                                DeletedCount++;
                            }
                            else if (t.TimeOfDay > DateTime.Now.TimeOfDay)
                            {
                                App.Reminders.Add(readedReminder);
                                AddedCount++;
                            }
                        }
                        else if (t.Date < DateTime.Now.Date)
                        {
                            moveFile.Move("Reminders", "Trash", file.Name[..^5], root: null!);
                            DeletedCount++;
                        }
                    }
                }
                if (isStartup)
                {
                    ShowNotifications();
                    LogWriter.Log("Startup notifications showed up", LogWriter.LogLevel.Debug);
                }
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex.Message, LogWriter.LogLevel.Error);
            }
        }
        private void ShowNotifications()
        {
            string localizationString = AddedCount == 1 ? "AppNotification_ActiveReminder"
                : AddedCount > 1 ? "AppNotification_ActiveReminders"
                : string.Empty;

            if (DeletedCount > 0 && AddedCount > 0)
            {
                notificationService.ShowDeletedMessage("Info".GetLocalized(), string.Format(localizationString.GetLocalized(), AddedCount.ToString()) +
                    "\n" +
                    string.Format("AppNotification_Expired".GetLocalized(), DeletedCount.ToString()));
            }
            else if (DeletedCount > 0 && AddedCount == 0)
            {
                notificationService.ShowDeletedMessage("Info".GetLocalized(), string.Format("AppNotification_Expired".GetLocalized(), DeletedCount.ToString()));
            }
            else if (DeletedCount == 0 && AddedCount > 0)
            {
                notificationService.ShowInfoMessage("Info".GetLocalized(), string.Format(localizationString.GetLocalized(), AddedCount.ToString()));
            }
        }
    }
}
