using Microsoft.UI.Xaml.Controls;
using MyNotes.ViewModels;
using Microsoft.UI.Xaml.Input;
using Windows.Storage;
using Microsoft.UI.Xaml;
using MyNotes.Helpers;

namespace MyNotes.Views;

public sealed partial class NotesPage : Page
{
    private readonly StorageFolder notesFolder = ApplicationData.Current.LocalFolder;
    public NotesViewModel ViewModel
    {
        get;
    }
    public NotesPage()
    {
        ViewModel = App.GetService<NotesViewModel>();
        InitializeComponent();
        ListFiles();
        if (LstNotes.Items.Count<1)
        {
            EmptyText.Visibility = Visibility.Visible;
        }
        else
        { 
            EmptyText.Visibility = Visibility.Collapsed;
        }
        deleteFlyout.Text = "DeleteFlyout".GetLocalized();
        deleteNoteFly.Content = "DeleteNote_Button".GetLocalized();
        ToolTipService.SetToolTip(deleteNote, "DeleteNote".GetLocalized());
        ToolTipService.SetToolTip(newNote, "AddNote".GetLocalized());
        
    }
    private void ListFiles()
    {
        DirectoryInfo dinfo = new(notesFolder.Path.ToString() + "\\Notes");
        FileInfo[] Files = dinfo.GetFiles("*.rtf");
        LstNotes.Items.Clear();
        foreach (FileInfo file in Files)
        {
            LstNotes.Items.Add(file.Name[..^4]);
        }
    }
    private void LstNotes_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (LstNotes.SelectedItem != null)
        {
            ShellPage.NoteName = LstNotes.SelectedItem.ToString();
            Frame.Navigate(typeof(NoteDetailsPage));
        }
    }
    private async void AddNote()
    {
        CreateNoteDialog AddNoteDialog = new()
        {
            XamlRoot = XamlRoot
        };
        await AddNoteDialog.ShowAsync();
        if (AddNoteDialog.Result== NoteCreateResult.NoteCreationOK)
        {
            Frame.Navigate(typeof(NoteDetailsPage));
        }
    }
    private void NewNote_Click(object sender, RoutedEventArgs e)
    {
        AddNote();
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
        deleteNote.IsEnabled = true;
    }
}
