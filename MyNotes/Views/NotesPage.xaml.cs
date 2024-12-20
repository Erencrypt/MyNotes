using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using MyNotes.Contracts.Services;
using MyNotes.Helpers;
using MyNotes.ViewModels;

namespace MyNotes.Views;

public sealed partial class NotesPage : Page
{
    private readonly INavigationService navigationService;
    //TODO: add rename option
    public NotesViewModel ViewModel
    {
        get;
    }
    public NotesPage()
    {
        ViewModel = App.GetService<NotesViewModel>();
        navigationService = App.GetService<INavigationService>();
        InitializeComponent();
        ViewModel.ListFiles(string.Empty, LstNotes);
        EmptyText.Visibility = LstNotes.Items.Count < 1 ? Visibility.Visible : Visibility.Collapsed;
        deleteFlyout.Text = "DeleteFlyout".GetLocalized();
        deleteNoteFly.Content = "DeleteConfirm".GetLocalized();
        EmptyText.Text = "Notes_EmptyText".GetLocalized();
        NotesSearch.PlaceholderText = "Search".GetLocalized();
        ToolTipService.SetToolTip(deleteNote, "Delete".GetLocalized());
        ToolTipService.SetToolTip(newNote, "Add".GetLocalized());
    }

    private void LstNotes_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (LstNotes.SelectedItem != null)
        {
            ShellPage.NoteName = LstNotes.SelectedItem.ToString();
            navigationService.NavigateTo(typeof(NoteDetailsViewModel).FullName!);
        }
    }

    private void NewNote_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.AddNote(XamlRoot);
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
    private void NotesSearch_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        ViewModel.ListFiles(NotesSearch.Text, LstNotes);
    }
}
