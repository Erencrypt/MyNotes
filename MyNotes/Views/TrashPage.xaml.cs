using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using MyNotes.Helpers;
using MyNotes.ViewModels;
using Windows.Storage;

namespace MyNotes.Views;

public sealed partial class TrashPage : Page
{
    private readonly StorageFolder notesFolder = ApplicationData.Current.LocalFolder;
    public TrashViewModel ViewModel
    {get;}
    public TrashPage()
    {
        ViewModel = App.GetService<TrashViewModel>();
        InitializeComponent();
        ListFiles();
        deleteFlyout.Text = "DeleteFlyout2".GetLocalized();
        deleteNoteFly.Content = "DeleteNote_Button".GetLocalized();
        ToolTipService.SetToolTip(deleteNote, "DeleteNote".GetLocalized());
        ToolTipService.SetToolTip(restoreNote, "RestoreNote".GetLocalized());
    }
    private void ListFiles()
    {
        DirectoryInfo dinfo = new DirectoryInfo(notesFolder.Path.ToString() + "\\Trash");
        FileInfo[] Files = dinfo.GetFiles("*.rtf");
        LstNotes.Items.Clear();
        foreach (FileInfo file in Files)
        {
            LstNotes.Items.Add(file.Name.Substring(0, file.Name.Length - 4));
        }
    }
    private async void DeleteNote()
    {
        try
        {
            var selectedItem = LstNotes.SelectedItem;
            if (selectedItem != null)
            {
                var directory = notesFolder.Path.ToString() + @"\Trash\" + LstNotes.SelectedItem.ToString() + ".rtf";
                var file = await StorageFile.GetFileFromPathAsync(directory);
                await file.DeleteAsync();
                LstNotes.Items.Remove(selectedItem);
            }
            else
            {
                ContentDialog noWifiDialog = new ContentDialog()
                {XamlRoot = XamlRoot,Title = "Info".GetLocalized(),Content = "NoSelection".GetLocalized(),CloseButtonText = "Ok".GetLocalized()};
                await noWifiDialog.ShowAsync();
            }
        }
        catch (Exception ex)
        {
            ContentDialog noWifiDialog = new ContentDialog()
            {
                XamlRoot = XamlRoot,
                Title = "Error".GetLocalized(),
                Content = "Error_Meesage2".GetLocalized() + ex.Message,
                CloseButtonText = "Ok".GetLocalized()
            };
            await noWifiDialog.ShowAsync();
        }
    }
    private async void RestoreNote()
    {
        try
        {
            var selectedItem = LstNotes.SelectedItem;
            if (selectedItem != null)
            {

                var directory = notesFolder.Path.ToString() + @"\Trash\" + LstNotes.SelectedItem.ToString() + ".rtf";
                var dir = notesFolder.Path.ToString() + @"\Notes\";
                var folder = await StorageFolder.GetFolderFromPathAsync(dir);
                var file = await StorageFile.GetFileFromPathAsync(directory);
                await file.CopyAsync(folder, selectedItem.ToString() + ".rtf", NameCollisionOption.GenerateUniqueName);
                await file.DeleteAsync();
                LstNotes.Items.Remove(selectedItem);
            }
            else
            {
                ContentDialog noWifiDialog = new ContentDialog()
                { XamlRoot = XamlRoot, Title = "Info".GetLocalized(), Content = "NoSelection".GetLocalized(), CloseButtonText = "Ok".GetLocalized() };
                await noWifiDialog.ShowAsync();
            }
        }
        catch (Exception ex)
        {
            ContentDialog noWifiDialog = new ContentDialog()
            { XamlRoot = XamlRoot, Title = "Error".GetLocalized(), Content = "Error_Meesage2".GetLocalized() + ex.Message, CloseButtonText = "Ok".GetLocalized() };
            await noWifiDialog.ShowAsync();
        }
    }
    private void DeleteNote_Click(object sender, RoutedEventArgs e)
    {
        DeleteNote();
        deleteNote.Flyout.Hide();
    }

    private void restoreNote_Click(object sender, RoutedEventArgs e)
    {
        RestoreNote();
    }
}
