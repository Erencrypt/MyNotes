using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MyNotes.Models;
using System.Collections.ObjectModel;
using Windows.Storage;

namespace MyNotes.Helpers
{
    public class MoveFile
    {
        private readonly StorageFolder storageFolder = App.StorageFolder;
        public async void Move(string from, string to, string filename, XamlRoot root)
        {
            try
            {
                if (filename != null)
                {
                    Mover(from, to, filename, ".json");
                }
                else
                {
                    if (root != null)
                    {
                        ContentDialog infoDialog = new()
                        { XamlRoot = root, Title = "Info".GetLocalized(), Content = "NoSelection".GetLocalized(), CloseButtonText = "Ok".GetLocalized() };
                        await infoDialog.ShowAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                if (root != null)
                {
                    ContentDialog infoDialog = new()
                    { XamlRoot = root, Title = "Error".GetLocalized(), Content = "Error_Meesage".GetLocalized() + ex.Message, CloseButtonText = "Ok".GetLocalized() };
                    await infoDialog.ShowAsync();
                    LogWriter.Log(ex.Message, LogWriter.LogLevel.Error);
                }
            }
        }
        public async void Move(string from, string to, ListView list, XamlRoot root)
        {
            try
            {
                var selectedItem = list.SelectedItem;
                if (selectedItem != null)
                {
                    Mover(from, to, selectedItem.ToString()!, ".rtf");
                    list.Items.Remove(selectedItem);
                }
                else
                {
                    ContentDialog infoDialog = new()
                    { XamlRoot = root, Title = "Info".GetLocalized(), Content = "NoSelection".GetLocalized(), CloseButtonText = "Ok".GetLocalized() };
                    await infoDialog.ShowAsync();
                }
            }
            catch (Exception ex)
            {
                ContentDialog infoDialog = new()
                { XamlRoot = root, Title = "Error".GetLocalized(), Content = "Error_Meesage".GetLocalized() + ex.Message, CloseButtonText = "Ok".GetLocalized() };
                await infoDialog.ShowAsync();
                LogWriter.Log(ex.Message, LogWriter.LogLevel.Error);
            }
        }
        public async void Move(string from, string to, ListView list, XamlRoot root, Reminder reminder, ObservableCollection<Reminder> items)
        {
            try
            {
                Reminder selectedItem = (Reminder)list.SelectedItem;
                if (selectedItem != null)
                {
                    Mover(from, to, selectedItem.ReminderHeader!, ".json");
                    items.Remove(reminder);
                }
                else
                {
                    ContentDialog infoDialog = new()
                    { XamlRoot = root, Title = "Info".GetLocalized(), Content = "NoSelection".GetLocalized(), CloseButtonText = "Ok".GetLocalized() };
                    await infoDialog.ShowAsync();
                }
            }
            catch (Exception ex)
            {
                ContentDialog infoDialog = new()
                { XamlRoot = root, Title = "Error".GetLocalized(), Content = "Error_Meesage".GetLocalized() + ex.Message, CloseButtonText = "Ok".GetLocalized() };
                await infoDialog.ShowAsync();
                LogWriter.Log(ex.Message, LogWriter.LogLevel.Error);
            }
        }
        private async void Mover(string from, string to, string filename, string extention)
        {
            var directory = storageFolder.Path + "\\" + from + "\\" + filename + extention;
            var dir = storageFolder.Path + "\\" + to + "\\";
            var folder = await StorageFolder.GetFolderFromPathAsync(dir);
            var file = await StorageFile.GetFileFromPathAsync(directory);
            await file.CopyAsync(folder, filename + extention, NameCollisionOption.GenerateUniqueName);
            await file.DeleteAsync();
        }
    }
}
