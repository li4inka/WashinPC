using Microsoft.VisualStudio.TestTools.UnitTesting;
using WashinPC.Localization;
using WashinPC.ViewModels;

namespace WashinPC.Tests.ViewModels
{
    [TestClass]
    public class SettingsViewModelTests
    {
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
