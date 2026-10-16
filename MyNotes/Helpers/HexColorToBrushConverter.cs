using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace MyNotes.Helpers
{
    /// <summary>
    /// Converts a hex color string ("#RRGGBB" or "#AARRGGBB") to a
    /// <see cref="SolidColorBrush"/> for use in XAML bindings.
    /// Returns a gray fallback brush on null, empty, or malformed input —
    /// never throws from inside the converter.
    /// </summary>
    public partial class HexColorToBrushConverter : IValueConverter
    {
        private static readonly SolidColorBrush Fallback =
            new(Color.FromArgb(0xFF, 0x6C, 0x75, 0x7D)); // #6C757D

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is string hex && TryParseHex(hex, out var color))
                return new SolidColorBrush(color);

            return Fallback;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotImplementedException();

        /// <summary>
        /// Parses "#RRGGBB" (6 hex digits) or "#AARRGGBB" (8 hex digits).
        /// The leading '#' is optional.
        /// </summary>
        private static bool TryParseHex(string hex, out Color color)
        {
            color = default;

            if (string.IsNullOrWhiteSpace(hex))
                return false;

            // Strip optional leading '#'
            var span = hex.AsSpan();
            if (span[0] == '#')
                span = span[1..];

            try
            {
                if (span.Length == 6)
                {
                    // #RRGGBB — fully opaque
                    var r = System.Convert.ToByte(span[0..2].ToString(), 16);
                    var g = System.Convert.ToByte(span[2..4].ToString(), 16);
                    var b = System.Convert.ToByte(span[4..6].ToString(), 16);
                    color = Color.FromArgb(0xFF, r, g, b);
                    return true;
                }

                if (span.Length == 8)
                {
                    // #AARRGGBB
                    var a = System.Convert.ToByte(span[0..2].ToString(), 16);
                    var r = System.Convert.ToByte(span[2..4].ToString(), 16);
                    var g = System.Convert.ToByte(span[4..6].ToString(), 16);
                    var b = System.Convert.ToByte(span[6..8].ToString(), 16);
                    color = Color.FromArgb(a, r, g, b);
                    return true;
                }
            }
            catch (FormatException)
            {
                // Invalid hex characters — fall through to return false
            }

            return false;
        }
    }
}
