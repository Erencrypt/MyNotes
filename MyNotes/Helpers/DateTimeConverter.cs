using Microsoft.UI.Xaml.Data;
using MyNotes.Data.Models.Enums;
using MyNotes.Data.Services.Interfaces;

namespace MyNotes.Helpers
{
    /// <summary>
    /// Converter to format DateTime display
    /// </summary>
    public partial class DateTimeConverter : IValueConverter
    {
        public TimeFormat _timeFormat;
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            try
            {
                _timeFormat = App.GetService<ISettingService>().GetSettingAsync<TimeFormat>(DataKeys.HourFormat, TimeFormat.TwelveHour).Result;
            }
            catch
            {
                _timeFormat = TimeFormat.TwelveHour;
            }

            if (value is not DateTime time)
            {
                return string.Empty;
            }

            string timeFormatString = _timeFormat == TimeFormat.TwelveHour ? "hh:mm tt" : "HH:mm";
            return time.ToString($"dd/MM/yyyy {timeFormatString}");
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
