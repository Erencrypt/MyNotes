using Microsoft.UI.Xaml.Data;
using MyNotes.Data.Models;
using MyNotes.Data.Models.Enums;
using MyNotes.Data.Services.Interfaces;

namespace MyNotes.Helpers
{
    /// <summary>
    /// Converter to format reminder DateTime display
    /// Repeating reminders show only time (HH:mm)
    /// Non-repeating reminders show full date and time
    /// </summary>
    public partial class ReminderDateTimeConverter : IValueConverter
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

            if (value is not Reminder reminder)
            {
                return string.Empty;
            }

            if (reminder.Repeat)
            {
                // Repeating reminders: show only time
                string timeFormatString = _timeFormat == TimeFormat.TwelveHour ? "hh:mm tt" : "HH:mm";
                return reminder.ReminderDateTime.ToString(timeFormatString);
            }
            else
            {
                // Non-repeating reminders: show date and time
                string timeFormatString = _timeFormat == TimeFormat.TwelveHour ? "hh:mm tt" : "HH:mm";
                return reminder.ReminderDateTime.ToString($"dd/MM/yyyy {timeFormatString}");
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
