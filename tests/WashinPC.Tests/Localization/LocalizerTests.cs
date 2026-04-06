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
            const string key = "non.existent.key";
            Assert.AreEqual(key, Localizer.T(key));
        }

        [TestMethod]
        public void CriticalKeys_DoNotContainMojibakeArtifacts()
        {
            var criticalKeys = new[]
            {
                "tab.cleaner",
                "tab.registry",
                "tab.privacy",
                "tab.settings",
                "settings.language",
                "settings.registry_expert_mode",
                "settings.registry_expert_hint",
                "registry.scan_categories",
                "registry.scan_results",
                "summary.files",
                "summary.registry"
            };

            foreach (var lang in new[] { Lang.RU, Lang.EN })
            {
                Localizer.Current = lang;

                foreach (var key in criticalKeys)
                {
                    var value = Localizer.T(key);
                    Assert.IsFalse(value.Contains("вЂ"));
                    Assert.IsFalse(value.Contains("В©"));
                }
            }
        }

        [TestMethod]
        public void Formatting_WorksForSummaryAndThemeKeys()
        {
            Localizer.Current = Lang.EN;
            Assert.AreEqual("Files: 5, Size: 10 MB", Localizer.T("summary.files", 5, "10 MB"));
            Assert.AreEqual("Records found: 3", Localizer.T("summary.registry", 3));
            Assert.AreEqual("Using Dark theme", Localizer.T("settings.theme.using", "Dark"));

            Localizer.Current = Lang.RU;
            Assert.AreEqual("Файлов: 5, Размер: 10 MB", Localizer.T("summary.files", 5, "10 MB"));
            Assert.AreEqual("Записей найдено: 3", Localizer.T("summary.registry", 3));
            Assert.AreEqual("Используется тема: Темная", Localizer.T("settings.theme.using", "Темная"));
        }
    }
}
