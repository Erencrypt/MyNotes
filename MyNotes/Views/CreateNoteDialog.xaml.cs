using Microsoft.UI.Xaml.Controls;
using Windows.Storage;
using MyNotes.Helpers;
using Microsoft.UI.Xaml;

namespace MyNotes.Views;

public enum NoteCreateResult
{
    NoteCreationOK,
    NoteCreationFail,
    NoteCreationCancel,
    Nothing
}
public sealed partial class CreateNoteDialog : ContentDialog
{
    public NoteCreateResult Result
    {
        get; private set;
    }
    private readonly StorageFolder notesFolder = ApplicationData.Current.LocalFolder;
    public CreateNoteDialog()
    {
        this.InitializeComponent();
    }
    private async void CreateNote(ContentDialogButtonClickEventArgs args)
    {
        try
        {
            var directory = notesFolder.Path.ToString() + @"\Notes\";
            var filelocation = directory + noteNameTextBox.Text + ".rtf";
            if (File.Exists(filelocation))
            {
                args.Cancel = true;
                errorTextBlock.Visibility = Visibility.Visible;
                errorTextBlock.Text = "There is a note with the same name\nalready exist. Please use another name.";
            }
            else
            {
                StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(directory);
                await (_ = folder.CreateFileAsync(noteNameTextBox.Text + ".rtf", CreationCollisionOption.OpenIfExists));
                ShellPage.NoteName = noteNameTextBox.Text;
                Result = NoteCreateResult.NoteCreationOK;
            }
        }
        catch (Exception ex)
        {
            Result = NoteCreateResult.NoteCreationFail;
            args.Cancel = true;
            errorTextBlock.Visibility = Visibility.Visible;
            errorTextBlock.Text = "An error occured. Error message:"+ex.Message;
        }
    }
    private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        if (string.IsNullOrEmpty(noteNameTextBox.Text))
        {
            args.Cancel = true;
            errorTextBlock.Visibility = Visibility.Visible;
            errorTextBlock.Text = "Required_Message".GetLocalized();
        }
        else
        {
            CreateNote(args);
        }
    }
    private void ContentDialog_CloseButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        Result = NoteCreateResult.NoteCreationCancel;
    }
}
