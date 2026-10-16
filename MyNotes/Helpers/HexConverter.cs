using Windows.UI;


namespace MyNotes.Helpers
{
    public static class HexConverter
    {
        /// <summary>
        /// Converts a Windows.UI.<see cref="Color"/> to an "#AARRGGBB" hex string
        /// compatible with <see cref="HexColorToBrushConverter"/>.
        /// </summary>
        public static string ColorToHex(Color c)
            => $"#{c.A:X2}{c.R:X2}{c.G:X2}{c.B:X2}";

        /// <summary>
        /// Parses an "#AARRGGBB" or "#RRGGBB" hex string into a <see cref="Color"/>.
        /// Falls back to opaque gray on any parse failure.
        /// </summary>
        public static Color HexToColor(string hex)
        {
            try
            {
                hex = hex.TrimStart('#');
                if (hex.Length == 6)
                    hex = "FF" + hex;
                if (hex.Length != 8)
                    return Color.FromArgb(0xFF, 0x6C, 0x75, 0x7D);

                return Color.FromArgb(
                    Convert.ToByte(hex[0..2], 16),
                    Convert.ToByte(hex[2..4], 16),
                    Convert.ToByte(hex[4..6], 16),
                    Convert.ToByte(hex[6..8], 16));
            }
            catch
            {
                return Color.FromArgb(0xFF, 0x6C, 0x75, 0x7D);
            }
        }
    }
}
