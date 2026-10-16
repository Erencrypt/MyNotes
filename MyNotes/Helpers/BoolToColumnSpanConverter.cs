using Microsoft.UI.Xaml.Data;

namespace MyNotes.Helpers
{
    /// <summary>
    /// Converts boolean to column span. When true (repeat is checked), returns 2. When false, returns 1.
    /// </summary>
    public partial class BoolToColumnSpanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool boolValue)
            {
                return boolValue ? 2 : 1;
            }
            return 1;
        }

        public object? ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is int colSpan)
            {
                return colSpan == 2;
            }
            return false;
        }
    }
}
