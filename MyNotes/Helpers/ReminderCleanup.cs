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
        public async void Clean(bool isStartup)
        {
            try
            {
                DeletedCount = 0;
                AddedCount = 0;
                await App.StorageFolder.CreateFolderAsync("Reminders", CreationCollisionOption.OpenIfExists);
                DirectoryInfo dinfo = new(App.StorageFolder.Path + "\\Reminders");
                FileInfo[] Files = dinfo.GetFiles("*.json");
                List<FileInfo> orderedList = Files.OrderByDescending(x => x.CreationTime).ToList();
                string fullPath;
                MoveFile moveFile = new();
                foreach (FileInfo file in orderedList)
                {
                    fullPath = dinfo.ToString() + "\\" + file.Name;
                    string readText = File.ReadAllText(fullPath, Encoding.UTF8);
                    Reminder readedReminder = JsonSerializer.Deserialize<Reminder>(readText)!;

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
                                moveFile.Move("Reminders", "Trash", readedReminder.ReminderHeader!, root: null!);
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
                            moveFile.Move("Reminders", "Trash", readedReminder.ReminderHeader!, root: null!);
                            DeletedCount++;
                        }
                    }
                }
                if (isStartup)
                {
                    ShowNotifications();
                }
            }
            catch (Exception)
            {
                throw;
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
