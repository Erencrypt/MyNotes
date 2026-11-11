using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Controls;
using System.Text.RegularExpressions;

namespace MyNotes.Helpers
{
    /// <summary>
    /// Converts note content (RTF) to the plain text stored in <c>Note.PlainText</c>.
    /// Uses a RichEditBox for decoding, so it must run on the UI thread.
    /// </summary>
    public static partial class RtfHelper
    {
        public static string ToPlainText(string? content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return string.Empty;
            }

            if (!content.StartsWith("{\\rtf", StringComparison.Ordinal))
            {
                // Already plain text.
                return Normalize(content);
            }

            var reb = new RichEditBox();
            reb.Document.SetText(TextSetOptions.FormatRtf, content);
            reb.Document.GetText(TextGetOptions.NoHidden, out string plainText);
            return Normalize(plainText);
        }

        /// <summary>Collapses line breaks and tabs to single spaces and trims.</summary>
        public static string Normalize(string? text)
            => LineBreakRegex().Replace(text ?? string.Empty, " ").Replace("\t", " ").Trim();

        [GeneratedRegex(@"\r?\n\s*|\r\s*")]
        private static partial Regex LineBreakRegex();
    }
}
