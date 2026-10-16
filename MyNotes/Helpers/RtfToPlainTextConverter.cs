using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using System.Text.RegularExpressions;

namespace MyNotes.Helpers
{
    public sealed partial class RtfToPlainTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var content = value as string;
            if (string.IsNullOrWhiteSpace(content))
            {
                return string.Empty;
            }

            try
            {
                // If looks like RTF, parse via RichEditBox to preserve decoding rules
                if (content.StartsWith("{\\rtf", StringComparison.Ordinal))
                {
                    var reb = new RichEditBox();
                    reb.Document.SetText(TextSetOptions.FormatRtf, content);
                    reb.Document.GetText(TextGetOptions.NoHidden, out string plainText);
                    return NormalizeWhitespace(plainText).Trim();
                }

                // Fallback when content is already plain text
                return NormalizeWhitespace(content).Trim();
            }
            catch
            {
                // If parsing fails, return original shortened text
                return Truncate(content, 500);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value;
        }

        private static string NormalizeWhitespace(string input)
        {
            return Regex.Replace(input ?? string.Empty, @"\r?\n\s*", " ").Replace("\t", " ");
        }

        private static string Truncate(string input, int maxLength)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input ?? string.Empty;
            }
            return input.Length <= maxLength ? input : input.Substring(0, maxLength);
        }
    }
}


