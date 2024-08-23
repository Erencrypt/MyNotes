using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MyNotes.Helpers;

namespace MyNotes.Views
{
    public sealed partial class AddTaskDialog : ContentDialog
    {
        public string TaskTitle => TitleInput.Text;
        public string TaskText => TextInput.Text;
        public AddTaskDialog()
        {
            InitializeComponent();
            PrimaryButtonText = "Add Task";
            CloseButtonText = "Cancel".GetLocalized();
        }
        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            if (string.IsNullOrEmpty(TitleInput.Text))
            {
                args.Cancel = true;
                errorTextBlock.Visibility = Visibility.Visible;
                errorTextBlock.Text = "Title is required";
            }
        }
        private void ContentDialog_CloseButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {}
    }
}
