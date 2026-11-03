using Microsoft.VisualStudio.TestTools.UnitTesting;
using WashinPC.Localization;
using WashinPC.ViewModels;
using WashinPC.Core;
using WashinPC.Services;

namespace WashinPC.Tests.ViewModels
{
    [TestClass]
    public class SettingsViewModelTests
    {
        [TestMethod]
        public void SystemThemeSurvivesSettingsAndRestartAndManualModeIgnoresSystemChanges()
        {
            var previous = AppSettings.ThemeMode;
            var reader = ThemeService.SystemDarkModeReader;
            try
            {
                bool dark = true; ThemeService.SystemDarkModeReader = () => dark;
                AppSettings.ThemeMode = ThemeService.ModeAuto;
                var first = new SettingsViewModel(); Assert.IsTrue(first.IsThemeAuto); Assert.AreEqual("auto", AppSettings.ThemeMode); Assert.IsTrue(AppSettings.DarkMode);
                dark = false; ThemeService.RefreshSystemTheme(); Assert.IsFalse(AppSettings.DarkMode); Assert.AreEqual("auto", AppSettings.ThemeMode);
                var reopened = new SettingsViewModel(); Assert.IsTrue(reopened.IsThemeAuto); Assert.AreEqual("auto", AppSettings.ThemeMode);
                reopened.SetThemeDarkCommand.Execute(null); dark = false; ThemeService.RefreshSystemTheme(); Assert.AreEqual("dark", AppSettings.ThemeMode); Assert.IsTrue(AppSettings.DarkMode);
                reopened.SetThemeLightCommand.Execute(null); dark = true; ThemeService.RefreshSystemTheme(); Assert.AreEqual("light", AppSettings.ThemeMode); Assert.IsFalse(AppSettings.DarkMode);
            }
            finally { ThemeService.SystemDarkModeReader = reader; AppSettings.ThemeMode = previous; }
        }
        [TestInitialize]
        public void Init()
        {
            Localizer.Initialize();
            Localizer.Current = Lang.EN;
        }

        [TestMethod]
        public void LanguageOption_ToStringReturnsDisplayName()
        {
            var option = new LanguageOption(Lang.EN)
            {
                DisplayName = "English"
            };

            Assert.AreEqual("English", option.ToString());
        }

        [TestMethod]
        public void LanguageOption_DisplayNameCanBeLocalized()
        {
            var option = new LanguageOption(Lang.RU);

            Localizer.Current = Lang.RU;
            option.DisplayName = Loc.SettingsLangRussian.Value;
            Assert.AreEqual("Русский", option.ToString());

            Localizer.Current = Lang.EN;
            option.DisplayName = Loc.SettingsLangRussian.Value;
            Assert.AreEqual("Russian", option.ToString());
        }
    }
}
