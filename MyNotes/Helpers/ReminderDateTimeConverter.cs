using Microsoft.UI.Xaml.Data;
using MyNotes.Data.Models;
using MyNotes.Data.Models.Enums;

namespace MyNotes.Helpers
{
    /// <summary>
    /// Converter to format reminder DateTime display
    /// Repeating reminders show only time (HH:mm)
    /// Non-repeating reminders show full date and time
    /// </summary>
    public partial class ReminderDateTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is not Reminder reminder)
            {
                return string.Empty;
            }

            string timeFormatString = App.TimeFormat == TimeFormat.TwelveHour ? "hh:mm tt" : "HH:mm";

            // Repeating reminders: show only time. Non-repeating: show date and time.
            return reminder.Repeat
                ? reminder.ReminderDateTime.ToString(timeFormatString)
                : reminder.ReminderDateTime.ToString($"dd/MM/yyyy {timeFormatString}");
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
