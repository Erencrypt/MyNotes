using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinUI3Localizer;

namespace MyNotes.Views.Dialogs
{
    /// <summary>
    /// Simple informational dialog: an optional title, a message and a single "Ok" button.
    /// </summary>
    public sealed partial class StatusDialog : ContentDialog
    {
        public StatusDialog(string message, string? title = null)
        {
            InitializeComponent();
            Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
            DefaultButton = ContentDialogButton.Close;

            if (!string.IsNullOrEmpty(title))
            {
                Title = title;
            }

            MessageTextBlock.Text = message;
            CloseButtonText = Localizer.Get().GetLocalizedString("Ok");
        }
    }
}
