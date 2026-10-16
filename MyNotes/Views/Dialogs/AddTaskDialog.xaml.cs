using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinUI3Localizer;

namespace MyNotes.Views.Dialogs
{
    public sealed partial class AddTaskDialog : ContentDialog
    {
        private readonly ILocalizer _localizer = Localizer.Get();
        public string TaskTitle => TitleInput.Text;
        public string TaskText => TextInput.Text;

        public AddTaskDialog()
        {
            InitializeComponent();
            Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
            DefaultButton = ContentDialogButton.Primary;

            PrimaryButtonText = _localizer.GetLocalizedString("Add");
            CloseButtonText = _localizer.GetLocalizedString("Cancel");
        }
        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            if (string.IsNullOrEmpty(TitleInput.Text))
            {
                args.Cancel = true;
                errorTextBlock.Visibility = Visibility.Visible;
                errorTextBlock.Text = _localizer.GetLocalizedString("Title is required");
            }
        }
        private void ContentDialog_CloseButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        { }
    }
}
