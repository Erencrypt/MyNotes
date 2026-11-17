using Microsoft.UI.Xaml;
using Windows.UI;
using Windows.UI.ViewManagement;

namespace MyNotes.Helpers
{
    /// <summary>
    /// Applies either the Windows system accent color or a user-picked custom color to the app
    /// by overriding the SystemAccentColor* resources that the WinUI accent brushes are built on.
    /// </summary>
    public static class AccentColorHelper
    {
        private static readonly UISettings _uiSettings = new();

        private static readonly string[] _keys =
        [
            "SystemAccentColor",
            "SystemAccentColorLight1",
            "SystemAccentColorLight2",
            "SystemAccentColorLight3",
            "SystemAccentColorDark1",
            "SystemAccentColorDark2",
            "SystemAccentColorDark3",
        ];

        public static Color SystemAccentColor => _uiSettings.GetColorValue(UIColorType.Accent);

        private static IEnumerable<(string ThemeKey, ResourceDictionary Dictionary)> EnumerateThemeDictionaries(ResourceDictionary root)
        {
            foreach (var (key, value) in root.ThemeDictionaries)
            {
                if (key is string themeKey && value is ResourceDictionary dictionary)
                {
                    yield return (themeKey, dictionary);
                }
            }

            foreach (var merged in root.MergedDictionaries)
            {
                foreach (var item in EnumerateThemeDictionaries(merged))
                {
                    yield return item;
                }
            }
        }

        /// <summary>
        /// Applies the accent palette. Pass null to use the system accent color.
        /// Call once at startup, before any window is created: brushes pick up the colors when they are
        /// first created. Changing them at runtime leaves controls with stale or wrong-theme values.
        /// </summary>
        public static void Apply(Color? customColor)
        {
            var palette = customColor is Color c ? BuildPalette(c) : GetSystemPalette();
            var resources = Application.Current.Resources;

            for (int i = 0; i < _keys.Length; i++)
            {
                resources[_keys[i]] = palette[i];
            }

            // XamlControlsResources resolves SystemAccentColor* inside its own theme dictionaries,
            // so the app-level values above don't reach its brushes; set them there too.
            foreach (var (themeKey, dictionary) in EnumerateThemeDictionaries(resources))
            {
                if (themeKey == "HighContrast")
                {
                    continue;
                }

                for (int i = 0; i < _keys.Length; i++)
                {
                    dictionary[_keys[i]] = palette[i];
                }
            }
        }

        private static Color[] GetSystemPalette() =>
        [
            _uiSettings.GetColorValue(UIColorType.Accent),
            _uiSettings.GetColorValue(UIColorType.AccentLight1),
            _uiSettings.GetColorValue(UIColorType.AccentLight2),
            _uiSettings.GetColorValue(UIColorType.AccentLight3),
            _uiSettings.GetColorValue(UIColorType.AccentDark1),
            _uiSettings.GetColorValue(UIColorType.AccentDark2),
            _uiSettings.GetColorValue(UIColorType.AccentDark3),
        ];

        private static Color[] BuildPalette(Color baseColor)
        {
            RgbToHsl(baseColor, out var h, out var s, out var l);
            return
            [
                baseColor,
                HslToRgb(h, s, Math.Min(1, l + (1 - l) * 0.25)),
                HslToRgb(h, s, Math.Min(1, l + (1 - l) * 0.50)),
                HslToRgb(h, s, Math.Min(1, l + (1 - l) * 0.75)),
                HslToRgb(h, s, l * 0.80),
                HslToRgb(h, s, l * 0.60),
                HslToRgb(h, s, l * 0.40),
            ];
        }


        #region converters
        private static void RgbToHsl(Color c, out double h, out double s, out double l)
        {
            double r = c.R / 255d, g = c.G / 255d, b = c.B / 255d;
            double max = Math.Max(r, Math.Max(g, b));
            double min = Math.Min(r, Math.Min(g, b));
            l = (max + min) / 2;

            if (max == min)
            {
                h = s = 0;
                return;
            }

            double d = max - min;
            s = l > 0.5 ? d / (2 - max - min) : d / (max + min);
            if (max == r) h = (g - b) / d + (g < b ? 6 : 0);
            else if (max == g) h = (b - r) / d + 2;
            else h = (r - g) / d + 4;
            h /= 6;
        }

        private static Color HslToRgb(double h, double s, double l)
        {
            double r, g, b;
            if (s == 0)
            {
                r = g = b = l;
            }
            else
            {
                double q = l < 0.5 ? l * (1 + s) : l + s - l * s;
                double p = 2 * l - q;
                r = HueToRgb(p, q, h + 1d / 3);
                g = HueToRgb(p, q, h);
                b = HueToRgb(p, q, h - 1d / 3);
            }

            return Color.FromArgb(0xFF, ToByte(r), ToByte(g), ToByte(b));
        }

        private static double HueToRgb(double p, double q, double t)
        {
            if (t < 0) t += 1;
            if (t > 1) t -= 1;
            if (t < 1d / 6) return p + (q - p) * 6 * t;
            if (t < 1d / 2) return q;
            if (t < 2d / 3) return p + (q - p) * (2d / 3 - t) * 6;
            return p;
        }

        private static byte ToByte(double v) => (byte)Math.Clamp(Math.Round(v * 255), 0, 255);

        #endregion
    }
}
