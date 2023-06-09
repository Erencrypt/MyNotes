using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using Windows.Storage;
using System.Collections.ObjectModel;
using static MyNotes.Views.RemindersPage;

namespace MyNotes.Helpers
{
    public class MoveFile
    {
        private readonly StorageFolder notesFolder = ApplicationData.Current.LocalFolder;
        public async void Move(string from, string to, ListView list, XamlRoot root)
        {
            try
            {
                var selectedItem = list.SelectedItem;
                if (selectedItem != null)
                {
                    var directory = notesFolder.Path.ToString() + "\\"+from+"\\" + list.SelectedItem.ToString() + ".rtf";
                    var dir = notesFolder.Path.ToString() + "\\" + to + "\\";
                    var folder = await StorageFolder.GetFolderFromPathAsync(dir);
                    var file = await StorageFile.GetFileFromPathAsync(directory);
                    await file.CopyAsync(folder, selectedItem.ToString() + ".rtf", NameCollisionOption.GenerateUniqueName);
                    await file.DeleteAsync();
                    list.Items.Remove(selectedItem);
                }
                else
                {
                    ContentDialog noWifiDialog = new()
                    { XamlRoot = root, Title = "Info".GetLocalized(), Content = "NoSelection".GetLocalized(), CloseButtonText = "Ok".GetLocalized() };
                    await noWifiDialog.ShowAsync();
                }
            }
            catch (Exception ex)
            {
                ContentDialog noWifiDialog = new()
                { XamlRoot = root, Title = "Error".GetLocalized(), Content = "Error_Meesage2".GetLocalized() + ex.Message, CloseButtonText = "Ok".GetLocalized() };
                await noWifiDialog.ShowAsync();
            }
        }
        public async void Move(string from, string to, ListView list, XamlRoot root, Reminder reminder, ObservableCollection<Reminder> items)
        {
            try
            {
                var selectedItem = list.SelectedItem;
                if (selectedItem != null)
                {
                    var directory = notesFolder.Path.ToString() + "\\" + from + "\\" + reminder.ReminderHeader.ToString() + ".txt";
                    var dir = notesFolder.Path.ToString() + "\\" + to + "\\";
                    var folder = await StorageFolder.GetFolderFromPathAsync(dir);
                    var file = await StorageFile.GetFileFromPathAsync(directory);
                    await file.CopyAsync(folder, reminder.ReminderHeader.ToString() + ".txt", NameCollisionOption.GenerateUniqueName);
                    await file.DeleteAsync();
                    items.Remove(reminder);
                }
                else
                {
                    ContentDialog noWifiDialog = new()
                    { XamlRoot = root, Title = "Info".GetLocalized(), Content = "NoSelection".GetLocalized(), CloseButtonText = "Ok".GetLocalized() };
                    await noWifiDialog.ShowAsync();
                }
            }
            catch (Exception ex)
            {
                ContentDialog noWifiDialog = new()
                { XamlRoot = root, Title = "Error".GetLocalized(), Content = "Error_Meesage2".GetLocalized() + ex.Message, CloseButtonText = "Ok".GetLocalized() };
                await noWifiDialog.ShowAsync();
            }
        }
    }
}
