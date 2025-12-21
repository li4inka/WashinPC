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

        public static event Action LanguageChanged;

        public static Lang Current
        {
            get => _current;
            set
            {
                if (_current == value)
                    return;

                _current = value;
                AppSettings.Language = value == Lang.RU ? "ru" : "en";

                Loc.RefreshAll();
                LanguageChanged?.Invoke();
            }
        }

        // ===================== SAFE DICT =====================

        private static Dictionary<string, string> SafeDict(params (string k, string v)[] items)
        {
            var dict = new Dictionary<string, string>();
            foreach (var (k, v) in items)
            {
                if (!dict.ContainsKey(k))
                    dict[k] = v;
            }
            return dict;
        }

        // ===================== RU =====================

        private static readonly Dictionary<string, string> RU = SafeDict(
            // App / Tabs
            ("app.title", "WashinPC"),
            ("tab.cleaner", "Очистка"),
            ("tab.registry", "Реестр"),
            ("tab.privacy", "Приватность"),
            ("tab.settings", "Настройки"),
            ("tab.contextmenu", "Контекстное меню"),

            // Buttons
            ("btn.scan", "Сканировать"),
            ("btn.clean", "Очистить"),
            ("btn.fix", "Исправить"),
            ("btn.recycle", "Очистить корзину"),
            ("btn.ok", "OK"),
            ("btn.yes", "Да"),
            ("btn.no", "Нет"),
            ("btn.enable", "Включить"),
            ("btn.disable", "Отключить"),

            // Checkboxes
            ("chk.aggr", "Агрессивный режим"),
            ("chk.backup", "Создать резервную копию"),

            // Columns
            ("col.source", "Источник"),
            ("col.size", "Размер"),
            ("col.category", "Категория"),
            ("col.key", "Ключ"),
            ("col.details", "Детали"),

            // Status / Summary
            ("status.scanning", "Сканирование…"),
            ("status.searching", "Поиск…"),
            ("status.cleaning", "Очистка…"),
            ("summary.files", "Файлов: {0}, Размер: {1}"),
            ("summary.registry", "Найдено записей: {0}"),

            // Cleaner
            ("cleaner.scanning", "Сканирование очистки…"),
            ("cleaner.searching", "Поиск временных файлов…"),
            ("cleaner.deleted", "Удалено файлов: {0}"),

            // Privacy
            ("privacy.scanning", "Сканирование приватности…"),
            ("privacy.searching", "Поиск приватных данных…"),

            // Registry
            ("registry.categories", "Категории очистки"),
            ("registry.scanning", "Сканирование реестра…"),
            ("registry.searching", "Поиск проблемных записей реестра…"),
            ("registry.fixed", "Исправлено проблем реестра: {0}"),
            ("registry.no.selection", "Нет выбранных проблем для исправления."),
            ("registry.confirm.fix", "Исправить выбранные проблемы реестра?\nРекомендуется создать резервные копии."),

            // Registry categories (ЕДИНЫЙ БЛОК)
            ("registry.cat.uninstall", "Записи деинсталляции"),
            ("registry.cat.shared_dlls", "Shared DLLs"),
            ("registry.cat.unused_file_ext", "Неиспользуемые расширения файлов"),
            ("registry.cat.help_files", "Файлы справки"),
            ("registry.cat.obsolete_software", "Устаревшее ПО"),
            ("registry.cat.applications", "Приложения"),
            ("registry.cat.type_libraries", "Type Libraries"),
            ("registry.cat.installer_refs", "Installer References"),
            ("registry.cat.startup", "Автозагрузка"),
            ("registry.cat.active_x", "ActiveX и Class Issues"),
            ("registry.cat.fonts", "Шрифты"),
            ("registry.cat.mui_cache", "Кэш MUI"),
            ("registry.cat.start_menu", "Меню Пуск"),
            ("registry.cat.sound_events", "Звуковые события"),

            // Context menu
            ("contextmenu.summary", "Элементов: {0}"),
            ("contextmenu.applied", "Применено изменений: {0}"),
            ("contextmenu.col.disable", "Откл."),
            ("contextmenu.col.name", "Действие"),
            ("contextmenu.col.target", "Цель"),
            ("contextmenu.col.kind", "Тип"),
            ("contextmenu.col.scope", "Область"),
            ("contextmenu.col.source", "Источник"),
            ("contextmenu.col.registry", "Реестр"),
            ("contextmenu.btn.apply", "Применить"),

            ("cleaner.user_temp", "Пользовательский TEMP"),
            ("cleaner.windows_temp", "Windows TEMP"),
            ("cleaner.prefetch", "Prefetch"),
            ("cleaner.recent", "Недавние документы"),

            ("privacy.chrome", "Google Chrome"),
            ("privacy.edge", "Microsoft Edge"),

            ("settings.language", "Язык интерфейса"),
            ("settings.instant.apply", "Изменения применяются сразу")

        );

        // ===================== EN =====================

        private static readonly Dictionary<string, string> EN = SafeDict(
            ("app.title", "WashinPC"),
            ("tab.cleaner", "Cleaner"),
            ("tab.registry", "Registry"),
            ("tab.privacy", "Privacy"),
            ("tab.settings", "Settings"),
            ("tab.contextmenu", "Context Menu"),

            ("btn.scan", "Scan"),
            ("btn.clean", "Clean"),
            ("btn.fix", "Fix"),
            ("btn.recycle", "Empty Recycle Bin"),
            ("btn.ok", "OK"),
            ("btn.yes", "Yes"),
            ("btn.no", "No"),
            ("btn.enable", "Enable"),
            ("btn.disable", "Disable"),

            ("chk.aggr", "Aggressive mode"),
            ("chk.backup", "Create backup"),

            ("col.source", "Source"),
            ("col.size", "Size"),
            ("col.category", "Category"),
            ("col.key", "Key"),
            ("col.details", "Details"),

            ("status.scanning", "Scanning…"),
            ("status.searching", "Searching…"),
            ("status.cleaning", "Cleaning…"),
            ("summary.files", "Files: {0}, Size: {1}"),
            ("summary.registry", "Records found: {0}"),

            ("cleaner.scanning", "Cleaner scan…"),
            ("cleaner.searching", "Searching temporary files…"),
            ("cleaner.deleted", "Deleted files: {0}"),

            ("privacy.scanning", "Privacy scan…"),
            ("privacy.searching", "Searching privacy data…"),

            ("registry.categories", "Registry categories"),
            ("registry.scanning", "Registry scan…"),
            ("registry.searching", "Searching registry issues…"),
            ("registry.fixed", "Registry issues fixed: {0}"),
            ("registry.no.selection", "No registry issues selected."),
            ("registry.confirm.fix", "Fix selected registry issues?\nIt is recommended to create backups."),

            ("registry.cat.uninstall", "Uninstall entries"),
            ("registry.cat.shared_dlls", "Shared DLLs"),
            ("registry.cat.unused_file_ext", "Unused file extensions"),
            ("registry.cat.help_files", "Help files"),
            ("registry.cat.obsolete_software", "Obsolete software"),
            ("registry.cat.applications", "Applications"),
            ("registry.cat.type_libraries", "Type Libraries"),
            ("registry.cat.installer_refs", "Installer References"),
            ("registry.cat.startup", "Startup"),
            ("registry.cat.active_x", "ActiveX and Class Issues"),
            ("registry.cat.fonts", "Fonts"),
            ("registry.cat.mui_cache", "MUI Cache"),
            ("registry.cat.start_menu", "Start Menu"),
            ("registry.cat.sound_events", "Sound Events"),

            ("contextmenu.summary", "Items: {0}"),
            ("contextmenu.applied", "Changes applied: {0}"),
            ("contextmenu.col.disable", "Off"),
            ("contextmenu.col.name", "Action"),
            ("contextmenu.col.target", "Target"),
            ("contextmenu.col.kind", "Type"),
            ("contextmenu.col.scope", "Scope"),
            ("contextmenu.col.source", "Source"),
            ("contextmenu.col.registry", "Registry"),
            ("contextmenu.btn.apply", "Apply"),

            ("cleaner.user_temp", "User TEMP files"),
            ("cleaner.windows_temp", "Windows TEMP files"),
            ("cleaner.prefetch", "Prefetch files"),
            ("cleaner.recent", "Recent documents"),

            ("privacy.chrome", "Google Chrome"),
            ("privacy.edge", "Microsoft Edge"),

            ("settings.language", "Interface language"),
            ("settings.instant.apply", "Changes are applied instantly")

        );

        // ===================== API =====================

        public static void Initialize()
        {
            try
            {
                string saved = AppSettings.Language;
                _current = saved == "ru" ? Lang.RU : Lang.EN;
            }
            catch
            {
                _current = Lang.EN;
            }

            Loc.RefreshAll();
        }

        public static string T(string key, params object[] args)
        {
            var dict = Current == Lang.RU ? RU : EN;
            if (!dict.TryGetValue(key, out var value))
                value = key;

            return args != null && args.Length > 0
                ? string.Format(value, args)
                : value;
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
        public static readonly LocalizedString RegistryCategories = new LocalizedString("registry.categories");

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
