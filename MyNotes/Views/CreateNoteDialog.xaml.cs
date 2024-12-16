using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
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
    private async Task<bool> TryCreateNoteAsync(string noteName)
    {
        try
        {
            var directory = notesFolder.Path + @"\Notes\";
            var fileLocation = directory + noteName + ".rtf";

            if (File.Exists(fileLocation))
            {
                errorTextBlock.Visibility = Visibility.Visible;
                errorTextBlock.Text = "CreateNote_ErrorExistingFile".GetLocalized();
                return false;
            }
            else
            {
                StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(directory);
                await folder.CreateFileAsync(noteName + ".rtf", CreationCollisionOption.OpenIfExists);
                ShellPage.NoteName = noteName;
                Result = NoteCreateResult.NoteCreationOK;
                return true;
            }
        }
        catch (Exception ex)
        {
            errorTextBlock.Visibility = Visibility.Visible;
            errorTextBlock.Text = "Error_Message".GetLocalized() + ex.Message;
            LogWriter.Log(ex.Message, LogWriter.LogLevel.Error);
            return false;
        }
    }

    private async void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        if (string.IsNullOrEmpty(noteNameTextBox.Text))
        {
            args.Cancel = true;
            errorTextBlock.Visibility = Visibility.Visible;
            errorTextBlock.Text = "Required_Message".GetLocalized();
        }
        else
        {
            bool success = await TryCreateNoteAsync(noteNameTextBox.Text);
            args.Cancel = !success; // Cancel if note creation fails
        }
    }

    private async void PrimaryButtonKeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        if (!string.IsNullOrEmpty(noteNameTextBox.Text) && await TryCreateNoteAsync(noteNameTextBox.Text))
        {
            Hide(); // Close the dialog if successful
        }

        args.Handled = true;
    }

    private void ContentDialog_CloseButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        Result = NoteCreateResult.NoteCreationCancel;
    }
}
