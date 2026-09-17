using System;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;

namespace LuaSTGEditorAva.Services
{
    public static class ThemeManager
    {
        public static readonly string[] Available = new[] { "Gray", "Light" };

        private static ColorPaletteResources BuildGrayPalette()
        {
            return new ColorPaletteResources
            {
                Accent = Color.Parse("#0078D7"),
                ChromeDisabledHigh = Color.Parse("#4A4A52"),
                ChromeHigh = Color.Parse("#8E8E98"),
                ChromeLow = Color.Parse("#2C2C33"),
                ChromeMedium = Color.Parse("#51515B"),
                ChromeMediumLow = Color.Parse("#3C3C44"),
                ChromeGray = Color.Parse("#7C7C86"),
                RegionColor = Color.Parse("#232329"),
            };
        }

        private static FluentTheme FluentTheme =>
            Application.Current?.Styles.OfType<FluentTheme>().FirstOrDefault();

        public static string Normalize(string theme)
        {
            if (string.Equals(theme, "Light", StringComparison.OrdinalIgnoreCase))
                return "Light";
            return "Gray";
        }

        public static void Apply(string theme)
        {
            try
            {
                Application app = Application.Current;
                if (app == null) return;
                string normalized = Normalize(theme);
                FluentTheme fluent = FluentTheme;
                if (normalized == "Light")
                {
                    if (fluent != null && fluent.Palettes.ContainsKey(ThemeVariant.Dark))
                        fluent.Palettes.Remove(ThemeVariant.Dark);
                    app.RequestedThemeVariant = ThemeVariant.Light;
                }
                else
                {
                    if (fluent != null)
                        fluent.Palettes[ThemeVariant.Dark] = BuildGrayPalette();
                    app.RequestedThemeVariant = ThemeVariant.Dark;
                }
            }
            catch
            {
            }
        }
    }
}
