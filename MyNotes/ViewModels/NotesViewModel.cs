using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MyNotes.Contracts.Services;
using MyNotes.Views;
using Windows.Storage;

namespace MyNotes.ViewModels;

public partial class NotesViewModel : ObservableRecipient
{
    private readonly StorageFolder storageFolder = App.StorageFolder;
    private readonly INavigationService navigationService;
    public NotesViewModel(INavigationService navigation)
    {
        navigationService = navigation;
    }
    public async void AddNote(XamlRoot xamlRoot)
    {
        CreateNoteDialog AddNoteDialog = new()
        {
            XamlRoot = xamlRoot
        };
        await AddNoteDialog.ShowAsync();
        if (AddNoteDialog.Result == NoteCreateResult.NoteCreationOK)
        {
            navigationService.NavigateTo(typeof(NoteDetailsViewModel).FullName!);
        }
    }
    public void ListFiles(string SearchText, ListView listView)
    {
        DirectoryInfo dinfo = new(storageFolder.Path + "\\Notes");
        FileInfo[] Files = dinfo.GetFiles("*.rtf");
        List<FileInfo> orderedList = SearchText == null ? orderedList = Files.OrderByDescending(x => x.LastAccessTime).ToList() :
        orderedList = Files.Where(x => x.Name[..^4].ToLower().Contains(SearchText.ToLower())).OrderByDescending(x => x.LastAccessTime).ToList();
        listView.Items.Clear();
        foreach (FileInfo file in orderedList)
        {
            listView.Items.Add(file.Name[..^4]);
        }
    }
}
