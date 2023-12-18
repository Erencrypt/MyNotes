using MyNotes.Contracts.Services;
using MyNotes.Models;
using System.Text;
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
                FileInfo[] Files = dinfo.GetFiles("*.txt");
                List<FileInfo> orderedList = Files.OrderByDescending(x => x.CreationTime).ToList();
                string fullPath;
                MoveFile moveFile = new();
                foreach (FileInfo file in orderedList)
                {
                    fullPath = dinfo.ToString() + "\\" + file.Name;
                    string readText = File.ReadAllText(fullPath, Encoding.UTF8);
                    string[] lines = readText.Split("\r\n");
                    if (lines.Length < 3)
                    {
                        continue;
                    }
                    else
                    {
                        DateTime t;
                        if (lines.Length == 3)
                        {
                            t = Convert.ToDateTime(lines[2]);
                            if (t.TimeOfDay > DateTime.Now.TimeOfDay)
                            {
                                App.Reminders.Add(new Reminder() { ReminderHeader = file.Name[..^4], ReminderText = lines[1], DateTime = t.ToString(), Repeat = lines[0] });
                                AddedCount++;
                            }
                        }
                        if (lines.Length == 4)
                        {
                            t = Convert.ToDateTime(lines[3] + " " + lines[2]);
                            if (t.Date == DateTime.Now.Date)
                            {
                                if (t.TimeOfDay < DateTime.Now.TimeOfDay)
                                {
                                    moveFile.Move("Reminders", "Trash", file.Name[..^4].ToString(), root: null!);
                                    DeletedCount++;
                                }
                                else if (t.TimeOfDay > DateTime.Now.TimeOfDay)
                                {
                                    App.Reminders.Add(new Reminder() { ReminderHeader = file.Name[..^4], ReminderText = lines[1], DateTime = t.ToString(), Repeat = lines[0] });
                                    AddedCount++;
                                }
                            }
                            else if (t.Date < DateTime.Now.Date)
                            {
                                moveFile.Move("Reminders", "Trash", file.Name[..^4].ToString(), root: null!);
                                DeletedCount++;
                            }
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
