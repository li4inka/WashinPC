using System;
using System.Collections.Generic;
using System.Globalization;
using WashinPC.Core;

namespace WashinPC.Localization
{
    public enum Lang
    {
        RU,
        EN
    }

    public static class Localizer
    {
        private static Lang _current = Lang.EN;

        public static Lang Current
        {
            get { return _current; }
            set
            {
                _current = value;
                AppSettings.Language = value == Lang.RU ? "ru" : "en";
            }
        }

        private static readonly Dictionary<string, string> RU =
            new Dictionary<string, string>
            {
                { "app.title", "WashinPC" },
                { "tab.cleaner", "Очистка" },
                { "tab.registry", "Реестр" },
                { "tab.privacy", "Приватность" },
                { "tab.settings", "Настройки" },

                { "btn.scan", "Сканировать" },
                { "btn.clean", "Очистить" },
                { "btn.fix", "Исправить" },
                { "btn.recycle", "Очистить Корзину" },

                { "btn.ok", "OK" },
                { "btn.yes", "Да" },
                { "btn.no", "Нет" },

                { "chk.aggr", "Агрессивный режим" },
                { "chk.backup", "Создать резервную копию" },

                { "settings.language", "Язык интерфейса" },
                { "settings.instant.apply", "Изменения применяются сразу" },

                { "admin.required", "Требуются права администратора" },
                { "app.error", "Ошибка" },

                { "col.source", "Источник" },
                { "col.size", "Размер" },
                { "col.category", "Категория" },
                { "col.key", "Ключ" },
                { "col.details", "Детали" },

                { "version.label", "Версия: " },
                { "build.label", "Сборка: " },
                { "copyright.label", "© Kulich Software" }
            };

        private static readonly Dictionary<string, string> EN =
            new Dictionary<string, string>
            {
                { "app.title", "WashinPC" },
                { "tab.cleaner", "Cleaner" },
                { "tab.registry", "Registry" },
                { "tab.privacy", "Privacy" },
                { "tab.settings", "Settings" },

                { "btn.scan", "Scan" },
                { "btn.clean", "Clean" },
                { "btn.fix", "Fix" },
                { "btn.recycle", "Empty Recycle Bin" },

                { "btn.ok", "OK" },
                { "btn.yes", "Yes" },
                { "btn.no", "No" },

                { "chk.aggr", "Aggressive mode" },
                { "chk.backup", "Create backup" },

                { "settings.language", "Interface language" },
                { "settings.instant.apply", "Changes are applied instantly" },

                { "admin.required", "Administrator privileges required" },
                { "app.error", "Error" },

                { "col.source", "Source" },
                { "col.size", "Size" },
                { "col.category", "Category" },
                { "col.key", "Key" },
                { "col.details", "Details" },

                { "version.label", "Version: " },
                { "build.label", "Build: " },
                { "copyright.label", "© Kulich Software" }
            };

        public static void Initialize()
        {
            try
            {
                string saved = AppSettings.Language;

                if (string.Equals(saved, "ru", StringComparison.OrdinalIgnoreCase))
                    Current = Lang.RU;
                else if (string.Equals(saved, "en", StringComparison.OrdinalIgnoreCase))
                    Current = Lang.EN;
                else
                    AutoDetectLanguage();
            }
            catch
            {
                AutoDetectLanguage();
            }

            Loc.RefreshAll();
        }

        public static void AutoDetectLanguage()
        {
            try
            {
                string lang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
                Current = lang == "ru" ? Lang.RU : Lang.EN;
            }
            catch
            {
                Current = Lang.EN;
            }
        }

        public static string T(string key, params object[] args)
        {
            Dictionary<string, string> dict =
                Current == Lang.RU ? RU : EN;

            string value;
            if (!dict.TryGetValue(key, out value))
                value = key;

            if (args != null && args.Length > 0)
                return string.Format(value, args);

            return value;
        }
    }

    // ===================== LOC =====================

    public static class Loc
    {
        public static readonly LocalizedString TabCleaner =
            new LocalizedString("tab.cleaner");

        public static readonly LocalizedString TabPrivacy =
            new LocalizedString("tab.privacy");

        public static readonly LocalizedString TabRegistry =
            new LocalizedString("tab.registry");

        public static readonly LocalizedString TabSettings =
            new LocalizedString("tab.settings");

        public static readonly LocalizedString BtnScan =
            new LocalizedString("btn.scan");

        public static readonly LocalizedString BtnClean =
            new LocalizedString("btn.clean");

        public static readonly LocalizedString BtnFix =
            new LocalizedString("btn.fix");

        public static readonly LocalizedString BtnRecycle =
            new LocalizedString("btn.recycle");

        public static readonly LocalizedString BtnOk =
            new LocalizedString("btn.ok");

        public static readonly LocalizedString BtnYes =
            new LocalizedString("btn.yes");

        public static readonly LocalizedString BtnNo =
            new LocalizedString("btn.no");

        public static readonly LocalizedString ChkAggressive =
            new LocalizedString("chk.aggr");

        public static readonly LocalizedString ChkBackup =
            new LocalizedString("chk.backup");

        public static readonly LocalizedString SettingsLanguage =
            new LocalizedString("settings.language");

        public static readonly LocalizedString SettingsInstantApply =
            new LocalizedString("settings.instant.apply");

        public static readonly LocalizedString ColSource =
            new LocalizedString("col.source");

        public static readonly LocalizedString ColSize =
            new LocalizedString("col.size");

        public static readonly LocalizedString ColCategory =
            new LocalizedString("col.category");

        public static readonly LocalizedString ColKey =
            new LocalizedString("col.key");

        public static readonly LocalizedString ColDetails =
            new LocalizedString("col.details");

        public static readonly LocalizedString VersionLabel =
            new LocalizedString("version.label");

        public static readonly LocalizedString BuildLabel =
            new LocalizedString("build.label");

        public static readonly LocalizedString Copyright =
            new LocalizedString("copyright.label");

        public static void RefreshAll()
        {
            TabCleaner.Refresh();
            TabPrivacy.Refresh();
            TabRegistry.Refresh();
            TabSettings.Refresh();

            BtnScan.Refresh();
            BtnClean.Refresh();
            BtnFix.Refresh();
            BtnRecycle.Refresh();

            BtnOk.Refresh();
            BtnYes.Refresh();
            BtnNo.Refresh();

            ChkAggressive.Refresh();
            ChkBackup.Refresh();

            SettingsLanguage.Refresh();
            SettingsInstantApply.Refresh();

            ColSource.Refresh();
            ColSize.Refresh();
            ColCategory.Refresh();
            ColKey.Refresh();
            ColDetails.Refresh();

            VersionLabel.Refresh();
            BuildLabel.Refresh();
            Copyright.Refresh();
        }
    }
}
