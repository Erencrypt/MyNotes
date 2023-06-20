using Microsoft.UI.Xaml.Controls;
using MyNotes.ViewModels;
using Microsoft.UI.Xaml.Input;
using Windows.Storage;
using Microsoft.UI.Xaml;
using MyNotes.Helpers;
using MyNotes.Contracts.Services;

namespace MyNotes.Views;

public sealed partial class NotesPage : Page
{
    private readonly StorageFolder storageFolder = ApplicationData.Current.LocalFolder;
    private readonly INavigationService navigationService;
    private readonly IAppNotificationService notificationService;

    public NotesViewModel ViewModel
    {
        get;
    }
    public NotesPage()
    {
        ViewModel = App.GetService<NotesViewModel>();
        navigationService = App.GetService<INavigationService>();
        notificationService = App.GetService<IAppNotificationService>();
        InitializeComponent();
        ListFiles();
        if (LstNotes.Items.Count < 1)
        {
            EmptyText.Visibility = Visibility.Visible;
        }
        else
        {
            EmptyText.Visibility = Visibility.Collapsed;
        }
        deleteFlyout.Text = "DeleteFlyout".GetLocalized();
        deleteNoteFly.Content = "DeleteConfirm".GetLocalized();
        ToolTipService.SetToolTip(deleteNote, "Delete".GetLocalized());
        ToolTipService.SetToolTip(newNote, "Add".GetLocalized());
    }
    private void ListFiles()
    {
        DirectoryInfo dinfo = new(storageFolder.Path.ToString() + "\\Notes");
        FileInfo[] Files = dinfo.GetFiles("*.rtf");
        List<FileInfo> orderedList = Files.OrderByDescending(x => x.CreationTime).ToList();
        LstNotes.Items.Clear();
        foreach (FileInfo file in orderedList)
        {
            LstNotes.Items.Add(file.Name[..^4]);
        }
    }
    private void LstNotes_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (LstNotes.SelectedItem != null)
        {
            ShellPage.NoteName = LstNotes.SelectedItem.ToString();
            navigationService.NavigateTo(typeof(NoteDetailsViewModel).FullName!);
        }
    }
    private async void AddNote()
    {
        CreateNoteDialog AddNoteDialog = new()
        {
            XamlRoot = XamlRoot
        };
        await AddNoteDialog.ShowAsync();
        if (AddNoteDialog.Result == NoteCreateResult.NoteCreationOK)
        {
            navigationService.NavigateTo(typeof(NoteDetailsViewModel).FullName!);
        }
    }
    private void NewNote_Click(object sender, RoutedEventArgs e)
    {
        //AddNote();
        notificationService.ShowReminder("header text", "this is reminder text.","time section");
    }
    private void DeleteNote_Click(object sender, RoutedEventArgs e)
    {
        if (LstNotes.Items.Count <= 1)
        {
            EmptyText.Visibility = Visibility.Visible;
        }
        deleteNote.Flyout.Hide();
        MoveFile moveFile = new();
        moveFile.Move("Notes", "Trash", LstNotes, XamlRoot);
    }
    private void LstNotes_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!deleteNote.IsEnabled)
        {
            deleteNote.IsEnabled = true;
        }
    }
}
