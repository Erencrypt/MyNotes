using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace MyNotes.Helpers
{
    /// <summary>
    /// Converts boolean to Visibility. When true (repeat is checked), returns Collapsed. When false, returns Visible.
    /// </summary>
    public partial class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Collapsed : Visibility.Visible;
            }
            return Visibility.Visible;
        }

        public object? ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is Visibility visibility)
            {
                return visibility == Visibility.Visible;
            }
            return false;
        }
    }
}
