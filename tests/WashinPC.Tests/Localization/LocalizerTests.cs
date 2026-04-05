using Microsoft.VisualStudio.TestTools.UnitTesting;
using WashinPC.Localization;

namespace WashinPC.Tests.Localization
{
    [TestClass]
    public class LocalizerTests
    {
        [TestInitialize]
        public void Init()
        {
            Localizer.Initialize();
        }

        [TestMethod]
        public void ContextMenuRefresh_HasRuAndEnValues()
        {
            Localizer.Current = Lang.RU;
            Assert.AreEqual("Обновить", Localizer.T("contextmenu.btn.refresh"));

            Localizer.Current = Lang.EN;
            Assert.AreEqual("Refresh", Localizer.T("contextmenu.btn.refresh"));
        }

        [TestMethod]
        public void RegistryExpertMode_HasRuAndEnValues()
        {
            Localizer.Current = Lang.RU;
            Assert.AreEqual("Экспертный режим реестра", Localizer.T("settings.registry_expert_mode"));

            Localizer.Current = Lang.EN;
            Assert.AreEqual("Registry expert mode", Localizer.T("settings.registry_expert_mode"));
        }

        [TestMethod]
        public void UnknownKey_ReturnsKey()
        {
            var key = "non.existent.key";
            Assert.AreEqual(key, Localizer.T(key));
        }
    }
}
