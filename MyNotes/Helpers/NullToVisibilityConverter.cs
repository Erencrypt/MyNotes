using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace MyNotes.Helpers
{
    /// <summary>
    /// Converts null to Visibility.Collapsed and non-null to Visibility.Visible.
    /// </summary>
    public partial class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value == null ? Visibility.Collapsed : Visibility.Visible;
        }

        public object? ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is Visibility visibility)
            {
                return visibility == Visibility.Visible ? new object() : null;
            }
            return null;
        }
    }
}


