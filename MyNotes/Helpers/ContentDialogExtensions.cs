using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace MyNotes.Helpers
{
    public static class ContentDialogExtensions
    {
        /// <summary>
        /// Shows the dialog in the window's current theme. Dialogs open in the popup layer, outside the
        /// root element that ThemeService sets RequestedTheme on, so they would otherwise follow the
        /// app/system theme instead of the theme picked in Settings.
        /// </summary>
        public static IAsyncOperation<ContentDialogResult> ShowThemedAsync(this ContentDialog dialog)
        {
            if (dialog.XamlRoot?.Content is FrameworkElement root)
            {
                dialog.RequestedTheme = root.ActualTheme;
            }

            return dialog.ShowAsync();
        }
    }
}
