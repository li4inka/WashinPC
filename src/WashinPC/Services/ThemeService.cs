using System;
using System.Linq;
using System.Windows;
using Microsoft.Win32;
using WashinPC.Core;
using WashinPC.Core.Logging;

namespace WashinPC.Services
{
    internal static class ThemeService
    {
        public const string ModeAuto = "auto";
        public const string ModeLight = "light";
        public const string ModeDark = "dark";

        private static readonly Uri LightDictionary = new Uri("Styles/Theme.Light.xaml", UriKind.Relative);
        private static readonly Uri DarkDictionary = new Uri("Styles/Theme.Dark.xaml", UriKind.Relative);
        private static readonly string[] RequiredBrushKeys =
        {
            "Brush.WindowBg",
            "Brush.PanelBg",
            "Brush.Border",
            "Brush.Input",
            "Brush.Primary",
            "Brush.Secondary",
            "Brush.Secondary20",
            "Brush.Secondary30",
            "Brush.Secondary50",
            "Brush.Text",
            "Brush.TextMuted",
            "Brush.CardHeaderBg",
            "Brush.PanelBorder",
            "Brush.SidebarBg",
            "Brush.SidebarBorder",
            "Brush.SidebarText",
            "Brush.SidebarMuted",
            "Brush.BottomBarBg"
        };

        public static bool IsKnownMode(string mode)
        {
            return string.Equals(mode, ModeAuto, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(mode, ModeLight, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(mode, ModeDark, StringComparison.OrdinalIgnoreCase);
        }

        public static string Resolve(string mode)
        {
            if (string.Equals(mode, ModeLight, StringComparison.OrdinalIgnoreCase))
                return ModeLight;

            if (string.Equals(mode, ModeDark, StringComparison.OrdinalIgnoreCase))
                return ModeDark;

            return IsSystemDarkMode() ? ModeDark : ModeLight;
        }

        public static void ApplyFromSettings()
        {
            Apply(AppSettings.ThemeMode);
        }

        public static void Apply(string mode)
        {
            if (!IsKnownMode(mode))
                mode = ModeAuto;

            AppSettings.ThemeMode = mode;

            var effective = Resolve(mode);
            ApplyThemeDictionary(effective == ModeDark ? DarkDictionary : LightDictionary);

            AppSettings.DarkMode = effective == ModeDark;
        }

        private static void ApplyThemeDictionary(Uri source)
        {
            var app = Application.Current;
            if (app == null)
                return;

            var merged = app.Resources.MergedDictionaries;
            var themeDictionary = merged.FirstOrDefault(d =>
                d.Source != null &&
                (d.Source.OriginalString.EndsWith("Theme.Light.xaml", StringComparison.OrdinalIgnoreCase) ||
                 d.Source.OriginalString.EndsWith("Theme.Dark.xaml", StringComparison.OrdinalIgnoreCase)));

            if (themeDictionary == null)
            {
                var inserted = new ResourceDictionary { Source = source };
                merged.Insert(0, inserted);
                LogThemeDictionaryState(source, inserted);
                return;
            }

            if (themeDictionary.Source == null || !string.Equals(themeDictionary.Source.OriginalString, source.OriginalString, StringComparison.OrdinalIgnoreCase))
                themeDictionary.Source = source;

            LogThemeDictionaryState(source, themeDictionary);
        }

        private static void LogThemeDictionaryState(Uri requestedSource, ResourceDictionary appliedDictionary)
        {
            try
            {
                var appliedSource = appliedDictionary?.Source?.OriginalString ?? "<null>";
                var requested = requestedSource?.OriginalString ?? "<null>";

                var missing = RequiredBrushKeys
                    .Where(key => appliedDictionary == null || !appliedDictionary.Contains(key))
                    .ToArray();

                var missingText = missing.Length == 0 ? "none" : string.Join(", ", missing);
                AppLogger.Info(
                    "theme",
                    "apply.dictionary",
                    string.Format(
                        "Theme dictionary applied. Requested='{0}', Applied='{1}', MissingBrushKeys={2}, Missing=[{3}]",
                        requested,
                        appliedSource,
                        missing.Length,
                        missingText));
            }
            catch (Exception ex)
            {
                AppLogger.Error("theme", "apply.dictionary.error", "Failed to log applied theme dictionary state.", ex);
            }
        }

        private static bool IsSystemDarkMode()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    var value = key?.GetValue("AppsUseLightTheme");
                    if (value is int intValue)
                        return intValue == 0;
                }
            }
            catch
            {
                // ignore and use fallback
            }

            return false;
        }
    }
}
