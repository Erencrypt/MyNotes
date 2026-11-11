using Microsoft.UI.Xaml.Data;
using MyNotes.Data.Models.Enums;

namespace MyNotes.Helpers
{
    /// <summary>
    /// Converter to format DateTime display
    /// </summary>
    public partial class DateTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is not DateTime time)
            {
                return string.Empty;
            }

            string timeFormatString = App.TimeFormat == TimeFormat.TwelveHour ? "hh:mm tt" : "HH:mm";
            return time.ToString($"dd/MM/yyyy {timeFormatString}");
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
