using Microsoft.UI.Xaml.Data;
using WinUI3Localizer;

namespace MyNotes.Helpers
{
    public sealed class DeletedTaskItemsCountConverter : IValueConverter
    {
        private readonly ILocalizer _localizer = Localizer.Get();

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var count = value is int itemCount ? itemCount : 0;
            return string.Format(_localizer.GetLocalizedString("Trash_DeletedTaskItemsCount"), count);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
