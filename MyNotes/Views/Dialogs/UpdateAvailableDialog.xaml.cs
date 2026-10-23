using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinUI3Localizer;

namespace MyNotes.Views.Dialogs
{
    public sealed partial class UpdateAvailableDialog : ContentDialog
    {
        public UpdateAvailableDialog(string title, string message)
        {
            InitializeComponent();
            Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
            DefaultButton = ContentDialogButton.Primary;

            var localizer = Localizer.Get();
            Title = title;
            MessageTextBlock.Text = message;
            PrimaryButtonText = localizer.GetLocalizedString("Update_DownloadAndInstall");
            CloseButtonText = localizer.GetLocalizedString("Update_Later");
        }
    }
}
