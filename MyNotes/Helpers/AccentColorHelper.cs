using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
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

        // Palette indices: 0 = base, 1-3 = Light1-3, 4-6 = Dark1-3.
        // Mirrors which SystemAccentColor* shade WinUI uses for each brush in Light vs Dark themes.
        private static readonly (string Key, int Light, int Dark)[] _brushMap =
        [
            ("AccentFillColorDefaultBrush", 4, 2),
            ("AccentFillColorSecondaryBrush", 4, 2),
            ("AccentFillColorTertiaryBrush", 4, 2),
            ("AccentFillColorSelectedTextBackgroundBrush", 0, 0),
            ("AccentTextFillColorPrimaryBrush", 5, 3),
            ("AccentTextFillColorSecondaryBrush", 6, 3),
            ("AccentTextFillColorTertiaryBrush", 4, 2),
            ("SystemControlHighlightAccentBrush", 0, 0),
            ("SystemControlBackgroundAccentBrush", 0, 0),
            ("SystemControlForegroundAccentBrush", 0, 0),
            ("SystemControlHighlightAltAccentBrush", 0, 0),
            ("SystemControlDisabledAccentBrush", 0, 0),
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
        /// When <paramref name="refreshRoot"/> is set, the element tree is forced to re-resolve
        /// its theme resources so the change is visible immediately.
        /// </summary>
        public static void Apply(Color? customColor, FrameworkElement? refreshRoot = null)
        {
            var palette = customColor is Color c ? BuildPalette(c) : GetSystemPalette();
            var resources = Application.Current.Resources;

            for (int i = 0; i < _keys.Length; i++)
            {
                resources[_keys[i]] = palette[i];
            }

            // XamlControlsResources defines the accent brushes (and the control brushes that alias them)
            // inside its own theme dictionaries, resolving SystemAccentColor* locally. App-level overrides
            // never reach those, so update the colors and the existing brush instances in place.
            foreach (var (themeKey, dictionary) in EnumerateThemeDictionaries(resources))
            {
                if (themeKey == "HighContrast")
                {
                    continue;
                }

                var isLight = themeKey == "Light";

                for (int i = 0; i < _keys.Length; i++)
                {
                    dictionary[_keys[i]] = palette[i];
                }

                foreach (var (brushKey, lightIndex, darkIndex) in _brushMap)
                {
                    if (dictionary.TryGetValue(brushKey, out var value) && value is SolidColorBrush brush)
                    {
                        brush.Color = palette[isLight ? lightIndex : darkIndex];
                    }
                }
            }

            if (refreshRoot is not null)
            {
                RefreshTheme(refreshRoot);
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

        /// <summary>
        /// ThemeResource references only re-resolve on a theme change, so flip the requested
        /// theme to the opposite of the current one and back again.
        /// </summary>
        private static void RefreshTheme(FrameworkElement root)
        {
            var original = root.RequestedTheme;
            var actual = root.ActualTheme;
            root.RequestedTheme = actual == ElementTheme.Dark ? ElementTheme.Light : ElementTheme.Dark;
            root.RequestedTheme = actual;
            root.RequestedTheme = original;
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
