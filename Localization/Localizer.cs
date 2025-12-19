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

        /// <summary>
        /// Событие смены языка (для списков/результатов, которые не входят в Loc.*)
        /// </summary>
        public static event Action LanguageChanged;

        public static Lang Current
        {
            get { return _current; }
            set
            {
                if (_current == value)
                    return;

                _current = value;
                AppSettings.Language = value == Lang.RU ? "ru" : "en";

                // обновляем статические LocalizedString из Loc
                Loc.RefreshAll();

                // уведомляем VM, чтобы они могли обновить динамические строки
                var handler = LanguageChanged;
                if (handler != null)
                    handler();
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
                { "copyright.label", "© Kulich Software" },

                { "status.scanning", "Сканирование…" },
                { "status.searching", "Поиск…" },
                { "status.cleaning", "Очистка…" },

                { "cleaner.scanning", "Сканирование очистки…" },
                { "cleaner.searching", "Поиск временных файлов…" },
                { "cleaner.deleted", "Удалено файлов: {0}" },

                { "privacy.scanning", "Сканирование приватности…" },
                { "privacy.searching", "Поиск приватных данных…" },

                { "registry.scanning", "Сканирование реестра…" },
                { "registry.searching", "Поиск проблемных записей реестра…" },
                { "registry.fixed", "Исправлено проблем реестра: {0}" },

                // ===== SUMMARY =====
                { "summary.files", "Файлов: {0}, Размер: {1}" },
                { "summary.registry", "Найдено записей: {0}" },

                // ===== CLEANER =====
                { "cleaner.confirm.clean", "Очистить выбранные файлы без возможности восстановления?" },
                { "cleaner.recycle.empty", "Корзина уже пуста." },
                { "cleaner.recycle.confirm", "Очистить корзину?\nВсе файлы будут удалены без возможности восстановления." },
                { "cleaner.recycle.scanning", "Очистка корзины…" },
                { "cleaner.recycle.cleaning", "Удаление файлов из корзины…" },
                { "cleaner.recycle.failed", "Не удалось очистить корзину.\nПопробуйте запустить программу от имени администратора." },
                { "cleaner.recycle.done", "Корзина успешно очищена" },

                // ===== REGISTRY =====
                { "registry.no.selection", "Нет выбранных проблем для исправления." },
                { "registry.confirm.fix", "Исправить выбранные проблемы реестра?\nРекомендуется создать резервные копии." },
                { "registry.fixed.with.backup", "Исправлено: {0}. Бэкапов: {1}" },

                // Registry categories
                { "registry.cat.app_paths", "Application Paths" },
                { "registry.cat.uninstall", "Записи деинсталляции" },
                { "registry.cat.shared_dlls", "Shared DLLs" },

                // Registry details templates
                { "registry.details.path_missing", "Путь не существует: {0}" },
                { "registry.details.uninstall_missing", "Отсутствует файл деинсталляции: {0}" },
                { "registry.details.file_missing", "Файл не существует: {0}" },

                // Cleaner groups (у тебя уже были)
                { "cleaner.user_temp", "Пользовательский TEMP" },
                { "cleaner.windows_temp", "Windows Temp" },
                { "cleaner.prefetch", "Prefetch" },
                { "cleaner.recent", "Список последних документов" },
                { "cleaner.edge_cache", "Edge (Chromium) Cache" },
                { "cleaner.chrome_cache", "Google Chrome Cache" },
                { "privacy.chrome", "Google Chrome" },
                { "privacy.edge", "Microsoft Edge" },

                // ContextMenu
                { "tab.contextmenu", "Контекстное меню" },

                { "contextmenu.summary", "Элементов: {0}" },
                { "contextmenu.applied", "Применено изменений: {0}" },

                { "contextmenu.col.disable", "Откл." },
                { "contextmenu.col.name", "Действие" },
                { "contextmenu.col.target", "Цель" },
                { "contextmenu.col.kind", "Тип" },
                { "contextmenu.col.scope", "Область" },
                { "contextmenu.col.source", "Источник" },
                { "contextmenu.col.registry", "Реестр" },

                { "contextmenu.btn.apply", "Применить" },
                { "btn.disable", "Отключить" },
                { "btn.enable", "Включить" },

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
                { "copyright.label", "© Kulich Software" },

                { "status.scanning", "Scanning…" },
                { "status.searching", "Searching…" },
                { "status.cleaning", "Cleaning…" },

                { "cleaner.scanning", "Cleaner scan…" },
                { "cleaner.searching", "Searching temporary files…" },
                { "cleaner.deleted", "Deleted files: {0}" },

                { "privacy.scanning", "Privacy scan…" },
                { "privacy.searching", "Searching privacy data…" },

                { "registry.scanning", "Registry scan…" },
                { "registry.searching", "Searching registry issues…" },
                { "registry.fixed", "Registry issues fixed: {0}" },

                // ===== SUMMARY =====
                { "summary.files", "Files: {0}, Size: {1}" },
                { "summary.registry", "Records found: {0}" },

                // ===== CLEANER =====
                { "cleaner.confirm.clean", "Clean selected files without the possibility of recovery?" },
                { "cleaner.recycle.empty", "Recycle Bin is already empty." },
                { "cleaner.recycle.confirm", "Empty Recycle Bin?\nAll files will be permanently deleted." },
                { "cleaner.recycle.scanning", "Cleaning Recycle Bin…" },
                { "cleaner.recycle.cleaning", "Removing files from Recycle Bin…" },
                { "cleaner.recycle.failed", "Failed to empty Recycle Bin.\nTry running the program as administrator." },
                { "cleaner.recycle.done", "Recycle Bin successfully emptied" },

                // ===== REGISTRY =====
                { "registry.no.selection", "No registry issues selected to fix." },
                { "registry.confirm.fix", "Fix selected registry issues?\nIt is recommended to create backups." },
                { "registry.fixed.with.backup", "Fixed: {0}. Backups: {1}" },

                // Registry categories
                { "registry.cat.app_paths", "Application Paths" },
                { "registry.cat.uninstall", "Uninstall entries" },
                { "registry.cat.shared_dlls", "Shared DLLs" },

                // Registry details templates
                { "registry.details.path_missing", "Path does not exist: {0}" },
                { "registry.details.uninstall_missing", "Uninstaller file missing: {0}" },
                { "registry.details.file_missing", "File does not exist: {0}" },

                // Cleaner groups (у тебя уже были)
                { "cleaner.user_temp", "User TEMP files" },
                { "cleaner.windows_temp", "Windows TEMP files" },
                { "cleaner.prefetch", "Prefetch files" },
                { "cleaner.recent", "Recent documents list" },
                { "cleaner.edge_cache", "Microsoft Edge (Chromium) Cache" },
                { "cleaner.chrome_cache", "Google Chrome Cache" },
                { "privacy.chrome", "Google Chrome" },
                { "privacy.edge", "Microsoft Edge" },

                // ContextMenu
                { "tab.contextmenu", "Context Menu" },

                { "contextmenu.summary", "Items: {0}" },
                { "contextmenu.applied", "Changes applied: {0}" },

                { "contextmenu.col.disable", "Off" },
                { "contextmenu.col.name", "Action" },
                { "contextmenu.col.target", "Target" },
                { "contextmenu.col.kind", "Type" },
                { "contextmenu.col.scope", "Scope" },
                { "contextmenu.col.source", "Source" },
                { "contextmenu.col.registry", "Registry" },

                { "contextmenu.btn.apply", "Apply" },
                { "btn.disable", "Disable" },
                { "btn.enable", "Enable" },

            };

        public static void Initialize()
        {
            try
            {
                string saved = AppSettings.Language;

                if (string.Equals(saved, "ru", StringComparison.OrdinalIgnoreCase))
                    _current = Lang.RU;
                else if (string.Equals(saved, "en", StringComparison.OrdinalIgnoreCase))
                    _current = Lang.EN;
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
                _current = lang == "ru" ? Lang.RU : Lang.EN;
                AppSettings.Language = _current == Lang.RU ? "ru" : "en";
            }
            catch
            {
                _current = Lang.EN;
                AppSettings.Language = "en";
            }
        }

        public static string T(string key, params object[] args)
        {
            Dictionary<string, string> dict = Current == Lang.RU ? RU : EN;

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
        public static readonly LocalizedString TabCleaner = new LocalizedString("tab.cleaner");
        public static readonly LocalizedString TabPrivacy = new LocalizedString("tab.privacy");
        public static readonly LocalizedString TabRegistry = new LocalizedString("tab.registry");
        public static readonly LocalizedString TabSettings = new LocalizedString("tab.settings");

        public static readonly LocalizedString BtnScan = new LocalizedString("btn.scan");
        public static readonly LocalizedString BtnClean = new LocalizedString("btn.clean");
        public static readonly LocalizedString BtnFix = new LocalizedString("btn.fix");
        public static readonly LocalizedString BtnRecycle = new LocalizedString("btn.recycle");

        public static readonly LocalizedString BtnOk = new LocalizedString("btn.ok");
        public static readonly LocalizedString BtnYes = new LocalizedString("btn.yes");
        public static readonly LocalizedString BtnNo = new LocalizedString("btn.no");

        public static readonly LocalizedString ChkAggressive = new LocalizedString("chk.aggr");
        public static readonly LocalizedString ChkBackup = new LocalizedString("chk.backup");

        public static readonly LocalizedString SettingsLanguage = new LocalizedString("settings.language");
        public static readonly LocalizedString SettingsInstantApply = new LocalizedString("settings.instant.apply");

        public static readonly LocalizedString ColSource = new LocalizedString("col.source");
        public static readonly LocalizedString ColSize = new LocalizedString("col.size");
        public static readonly LocalizedString ColCategory = new LocalizedString("col.category");
        public static readonly LocalizedString ColKey = new LocalizedString("col.key");
        public static readonly LocalizedString ColDetails = new LocalizedString("col.details");

        public static readonly LocalizedString VersionLabel = new LocalizedString("version.label");
        public static readonly LocalizedString BuildLabel = new LocalizedString("build.label");
        public static readonly LocalizedString Copyright = new LocalizedString("copyright.label");

        // ===== STATUS / VM TEXTS =====
        public static readonly LocalizedString StatusScanning = new LocalizedString("status.scanning");
        public static readonly LocalizedString StatusSearching = new LocalizedString("status.searching");
        public static readonly LocalizedString StatusCleaning = new LocalizedString("status.cleaning");

        // ===== CLEANER =====
        public static readonly LocalizedString CleanerScanning = new LocalizedString("cleaner.scanning");
        public static readonly LocalizedString CleanerSearching = new LocalizedString("cleaner.searching");
        public static readonly LocalizedString CleanerDeleted = new LocalizedString("cleaner.deleted");

        // ===== PRIVACY =====
        public static readonly LocalizedString PrivacyScanning = new LocalizedString("privacy.scanning");
        public static readonly LocalizedString PrivacySearching = new LocalizedString("privacy.searching");

        // ===== REGISTRY =====
        public static readonly LocalizedString RegistryScanning = new LocalizedString("registry.scanning");
        public static readonly LocalizedString RegistrySearching = new LocalizedString("registry.searching");
        public static readonly LocalizedString RegistryFixed = new LocalizedString("registry.fixed");

        // ===== SUMMARY =====
        public static readonly LocalizedString SummaryFiles = new LocalizedString("summary.files");
        public static readonly LocalizedString SummaryRegistry = new LocalizedString("summary.registry");

        // ===== CLEANER dialogs =====
        public static readonly LocalizedString CleanerConfirmClean = new LocalizedString("cleaner.confirm.clean");
        public static readonly LocalizedString CleanerRecycleEmpty = new LocalizedString("cleaner.recycle.empty");
        public static readonly LocalizedString CleanerRecycleConfirm = new LocalizedString("cleaner.recycle.confirm");
        public static readonly LocalizedString CleanerRecycleScanning = new LocalizedString("cleaner.recycle.scanning");
        public static readonly LocalizedString CleanerRecycleCleaning = new LocalizedString("cleaner.recycle.cleaning");
        public static readonly LocalizedString CleanerRecycleFailed = new LocalizedString("cleaner.recycle.failed");
        public static readonly LocalizedString CleanerRecycleDone = new LocalizedString("cleaner.recycle.done");

        // ===== REGISTRY dialogs =====
        public static readonly LocalizedString RegistryNoSelection = new LocalizedString("registry.no.selection");
        public static readonly LocalizedString RegistryConfirmFix = new LocalizedString("registry.confirm.fix");
        public static readonly LocalizedString RegistryFixedWithBackup = new LocalizedString("registry.fixed.with.backup");

        // ===== Context Menu =====
        public static readonly LocalizedString TabContextMenu = new LocalizedString("tab.contextmenu");

        public static readonly LocalizedString ContextMenuColDisable = new LocalizedString("contextmenu.col.disable");
        public static readonly LocalizedString ContextMenuColName = new LocalizedString("contextmenu.col.name");
        public static readonly LocalizedString ContextMenuColTarget = new LocalizedString("contextmenu.col.target");
        public static readonly LocalizedString ContextMenuColKind = new LocalizedString("contextmenu.col.kind");
        public static readonly LocalizedString ContextMenuColScope = new LocalizedString("contextmenu.col.scope");
        public static readonly LocalizedString ContextMenuColSource = new LocalizedString("contextmenu.col.source");
        public static readonly LocalizedString ContextMenuColRegistry = new LocalizedString("contextmenu.col.registry");

        public static readonly LocalizedString ContextMenuBtnApply = new LocalizedString("contextmenu.btn.apply");
        public static readonly LocalizedString BtnDisable = new LocalizedString("btn.disable");
        public static readonly LocalizedString BtnEnable = new LocalizedString("btn.enable");


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

            StatusScanning.Refresh();
            StatusSearching.Refresh();
            StatusCleaning.Refresh();

            CleanerScanning.Refresh();
            CleanerSearching.Refresh();
            CleanerDeleted.Refresh();

            PrivacyScanning.Refresh();
            PrivacySearching.Refresh();

            RegistryScanning.Refresh();
            RegistrySearching.Refresh();
            RegistryFixed.Refresh();

            SummaryFiles.Refresh();
            SummaryRegistry.Refresh();

            CleanerConfirmClean.Refresh();
            CleanerRecycleEmpty.Refresh();
            CleanerRecycleConfirm.Refresh();
            CleanerRecycleScanning.Refresh();
            CleanerRecycleCleaning.Refresh();
            CleanerRecycleFailed.Refresh();
            CleanerRecycleDone.Refresh();

            RegistryNoSelection.Refresh();
            RegistryConfirmFix.Refresh();
            RegistryFixedWithBackup.Refresh();

            TabContextMenu.Refresh();

            ContextMenuColDisable.Refresh();
            ContextMenuColName.Refresh();
            ContextMenuColTarget.Refresh();
            ContextMenuColKind.Refresh();
            ContextMenuColScope.Refresh();
            ContextMenuColSource.Refresh();
            ContextMenuColRegistry.Refresh();

            ContextMenuBtnApply.Refresh();
            BtnDisable.Refresh();
            BtnEnable.Refresh();


        }
    }
}
