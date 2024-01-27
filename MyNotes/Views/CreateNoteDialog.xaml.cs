using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MyNotes.Helpers;
using Windows.Storage;

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
    private readonly StorageFolder notesFolder = App.StorageFolder;
    public CreateNoteDialog()
    {
        InitializeComponent();
        noteNameTextBox.Header = "CreateNote_NameBoxHeader".GetLocalized();
        Title = "CreateNote_Title".GetLocalized();
        PrimaryButtonText = "CreateNote_ButtonText".GetLocalized();
        CloseButtonText = "Cancel".GetLocalized();
    }
    private async void CreateNote(ContentDialogButtonClickEventArgs args)
    {
        try
        {
            var directory = notesFolder.Path + @"\Notes\";
            var filelocation = directory + noteNameTextBox.Text + ".rtf";
            if (File.Exists(filelocation))
            {
                args.Cancel = true;
                errorTextBlock.Visibility = Visibility.Visible;
                errorTextBlock.Text = "CreateNote_ErrorExistingFile".GetLocalized();
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
            errorTextBlock.Text = "Error_Meesage".GetLocalized() + ex.Message;
            LogWriter.Log(ex.Message, LogWriter.LogLevel.Error);
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
