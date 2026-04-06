using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WashinPC.ViewModels;

namespace WashinPC.Tests.Styles
{
    [TestClass]
    public class ThemeDarkResourceTests
    {
        [TestMethod]
        public void ThemeDark_HasRequiredBrushKeys()
        {
            var root = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".."));
            var darkThemePath = Path.Combine(root, "src", "WashinPC", "Styles", "Theme.Dark.xaml");
            Assert.IsTrue(File.Exists(darkThemePath), "Theme.Dark.xaml was not found.");

            var doc = XDocument.Load(darkThemePath);
            XNamespace xNs = "http://schemas.microsoft.com/winfx/2006/xaml";

            var keys = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
            foreach (var key in doc.Descendants()
                .Select(e => (string)e.Attribute(xNs + "Key"))
                .Where(k => !string.IsNullOrWhiteSpace(k)))
            {
                keys.Add(key);
            }

            var required = new[]
            {
                "Brush.WindowBg",
                "Brush.PanelBg",
                "Brush.Border",
                "Brush.Input",
                "Brush.Primary",
                "Brush.Secondary",
                "Brush.SecondaryForeground",
                "Brush.Secondary20",
                "Brush.Secondary30",
                "Brush.Secondary50",
                "Brush.Accent",
                "Brush.Text",
                "Brush.TextMuted",
                "Brush.CardHeaderBg",
                "Brush.PanelBorder",
                "Brush.SidebarBg",
                "Brush.SidebarBorder",
                "Brush.SidebarText",
                "Brush.SidebarMuted",
                "Brush.SidebarAccent",
                "Brush.BottomBarBg",
                "Brush.Border50",
                "Brush.PrimaryShadow20"
            };

            foreach (var key in required)
                Assert.IsTrue(keys.Contains(key), $"Missing dark theme key: {key}");
        }

        [TestMethod]
        public void ThemeService_ExposesDarkDictionaryAndModes()
        {
            var assembly = typeof(SettingsViewModel).Assembly;
            var themeServiceType = assembly.GetType("WashinPC.Services.ThemeService", throwOnError: true);

            var isKnownMode = themeServiceType.GetMethod("IsKnownMode", BindingFlags.Public | BindingFlags.Static);
            var resolve = themeServiceType.GetMethod("Resolve", BindingFlags.Public | BindingFlags.Static);
            var darkDictionary = themeServiceType.GetField("DarkDictionary", BindingFlags.NonPublic | BindingFlags.Static);

            Assert.IsNotNull(isKnownMode);
            Assert.IsNotNull(resolve);
            Assert.IsNotNull(darkDictionary);

            Assert.AreEqual(true, isKnownMode.Invoke(null, new object[] { "dark" }));
            Assert.AreEqual(true, isKnownMode.Invoke(null, new object[] { "light" }));
            Assert.AreEqual(true, isKnownMode.Invoke(null, new object[] { "auto" }));
            Assert.AreEqual(false, isKnownMode.Invoke(null, new object[] { "unknown" }));

            Assert.AreEqual("dark", resolve.Invoke(null, new object[] { "dark" }));
            Assert.AreEqual("light", resolve.Invoke(null, new object[] { "light" }));

            var darkUri = darkDictionary.GetValue(null) as Uri;
            Assert.IsNotNull(darkUri);
            StringAssert.EndsWith(darkUri.OriginalString, "Theme.Dark.xaml");
        }
    }
}
