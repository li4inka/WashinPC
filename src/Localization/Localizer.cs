using System;
using System.Collections.Generic;
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
                try
                {
                    AppSettings.Language = value == Lang.RU ? "ru" : "en";
                }
                catch
                {
                    // In restricted environments (tests/sandbox), registry write may be blocked.
                    // Language switch should still work in-memory for the running session.
                }

                Loc.RefreshAll();
                LanguageChanged?.Invoke();
            }
        }

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

        private static readonly Dictionary<string, string> RU = SafeDict(
            ("app.title", "WashinPC"),
            ("tab.cleaner", "Очистка"),
            ("tab.registry", "Реестр"),
            ("tab.privacy", "Приватность"),
            ("tab.settings", "Настройки"),
            ("tab.contextmenu", "Контекстное меню"),

            ("btn.scan", "Скан"),
            ("btn.clean", "Очистить"),
            ("btn.fix", "Исправить"),
            ("btn.recycle", "Очистить корзину"),
            ("btn.ok", "OK"),
            ("btn.yes", "Да"),
            ("btn.no", "Нет"),
            ("btn.enable", "Включить"),
            ("btn.disable", "Отключить"),

            ("chk.aggr", "Агрессивный режим"),
            ("chk.backup", "Создать резервную копию"),

            ("col.source", "Источник"),
            ("col.size", "Размер"),
            ("col.category", "Категория"),
            ("col.key", "Ключ"),
            ("col.details", "Детали"),

            ("status.scanning", "Сканирование…"),
            ("status.searching", "Поиск…"),
            ("status.cleaning", "Очистка…"),
            ("summary.files", "Файлов: {0}, Размер: {1}"),
            ("summary.registry", "Записей найдено: {0}"),

            ("cleaner.scanning", "Сканирование очистки…"),
            ("cleaner.searching", "Поиск временных файлов…"),
            ("cleaner.deleted", "Удалено файлов: {0}"),

            ("privacy.scanning", "Сканирование приватности…"),
            ("privacy.searching", "Поиск приватных данных…"),

            ("registry.categories", "Категории реестра"),
            ("registry.scanning", "Сканирование реестра…"),
            ("registry.searching", "Поиск проблем реестра…"),
            ("registry.fixed", "Исправлено проблем реестра: {0}"),
            ("registry.no.selection", "Не выбраны проблемы реестра для исправления."),
            ("registry.confirm.fix", "Исправить выбранные проблемы реестра?\nРекомендуется создать резервные копии."),

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

            ("registry.info.title", "Внимание"),

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

            ("cleaner.user_temp", "User TEMP files"),
            ("cleaner.windows_temp", "Windows TEMP files"),
            ("cleaner.prefetch", "Prefetch files"),
            ("cleaner.recent", "Recent documents"),

            ("privacy.chrome", "Google Chrome"),
            ("privacy.edge", "Microsoft Edge"),

            ("settings.language", "Язык интерфейса"),
            ("settings.instant.apply", "Изменения применяются сразу")
        );

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

            ("registry.info.title", "Warning"),

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

        static Localizer()
        {
            EnsureCriticalKeys();
        }

        private static void AddIfMissing(Dictionary<string, string> dict, string key, string value)
        {
            if (!dict.ContainsKey(key))
                dict[key] = value;
        }

        private static void AddPair(string key, string ru, string en)
        {
            AddIfMissing(RU, key, ru);
            AddIfMissing(EN, key, en);
        }

        private static void EnsureCriticalKeys()
        {
            AddPair("admin.required", "Для запуска приложения требуются права администратора.", "Administrator privileges are required to run this app.");

            AddPair("version.label", "Версия", "Version");
            AddPair("build.label", "Сборка", "Build");
            AddPair("copyright.label", "© Kulich Software", "© Kulich Software");

            AddPair("cleaner.confirm.clean", "Удалить выбранные файлы?", "Delete selected files?");
            AddPair("privacy.confirm.clean", "Удалить выбранные приватные данные?", "Delete selected privacy data?");
            AddPair("cleaner.recycle.empty", "Корзина уже пуста.", "Recycle Bin is already empty.");
            AddPair("cleaner.recycle.confirm", "Очистить корзину без возможности восстановления?", "Empty Recycle Bin permanently?");
            AddPair("cleaner.recycle.scanning", "Проверка корзины…", "Checking Recycle Bin…");
            AddPair("cleaner.recycle.cleaning", "Очистка корзины…", "Cleaning Recycle Bin…");
            AddPair("cleaner.recycle.failed", "Не удалось очистить корзину.", "Failed to clean Recycle Bin.");
            AddPair("cleaner.recycle.done", "Корзина очищена.", "Recycle Bin has been cleaned.");
            AddPair("operation.done", "Операция завершена.", "Operation completed.");
            AddPair("operation.done.with_warnings", "Операция завершена с предупреждениями.", "Operation completed with warnings.");

            AddPair("registry.nothing_selected", "Не выбрано ни одной записи для исправления.", "No registry entries selected.");
            AddPair("registry.fixing", "Исправление реестра…", "Fixing registry…");
            AddPair("registry.fixed.summary", "Исправлено: {0}, пропущено: {1}, ошибок: {2}", "Fixed: {0}, skipped: {1}, errors: {2}");
            AddPair("registry.fixed.with.backup", "Исправление завершено с резервными копиями.", "Fix completed with backups.");
            AddPair("registry.restorepoint.failed.confirm", "Не удалось создать точку восстановления.\nПродолжить без неё?\n{0}", "System restore point was not created.\nContinue without it?\n{0}");
            AddPair("registry.restorepoint.cancelled", "Исправление отменено пользователем.", "Fix cancelled by user.");
            AddPair("registry.safe_only.disabled", "Недоступно в безопасном режиме.", "Unavailable in Safe-only mode.");
            AddPair("registry.risk.safe", "Безопасные", "Safe");
            AddPair("registry.risk.advanced", "Расширенные", "Advanced");
            AddPair("registry.risk.dangerous", "Опасные", "Dangerous");

            AddPair("registry.cat.info.registry.cat.active_x", "ActiveX и COM-записи. Удаление может привести к неработоспособности программ.", "ActiveX and COM entries. Removal may break installed programs.");
            AddPair("registry.cat.info.registry.cat.fonts", "Записи установленных шрифтов. Ошибочное удаление может повлиять на интерфейс Windows.", "Installed font entries. Incorrect removal may affect the Windows interface.");
            AddPair("registry.cat.info.registry.cat.mui_cache", "Кэш локализованных ресурсов. Обычно безопасно, но может замедлить первый запуск программ.", "Localized resource cache. Usually safe, but may slow first launches of programs.");
            AddPair("registry.cat.info.registry.cat.start_menu", "Записи меню Пуск. Удаление может привести к исчезновению ярлыков.", "Start Menu entries. Deletion may remove shortcuts.");
            AddPair("registry.cat.info.registry.cat.sound_events", "Системные звуковые события. Может повлиять на звуки Windows.", "System sound events. May affect Windows sounds.");

            AddPair("registry.details.path_missing", "Файл не найден: {0}", "Path not found: {0}");
            AddPair("registry.details.uninstall_missing", "Деинсталлятор не найден: {0}", "Uninstall target missing: {0}");
            AddPair("registry.details.file_missing", "Файл не найден: {0}", "File not found: {0}");
            AddPair("registry.details.unused_extension", "Расширение не связано с приложениями: {0}", "Unused extension without valid association: {0}");
            AddPair("registry.details.com_server_missing", "COM-сервер не найден: {0}", "COM server missing: {0}");
            AddPair("registry.details.typelib_missing", "Библиотека типов не найдена: {0}", "Type library missing: {0}");
            AddPair("registry.details.app_command_missing", "Команда приложения не найдена: {0}", "Application command target missing: {0}");
            AddPair("registry.details.font_missing", "Файл шрифта не найден: {0}", "Font file missing: {0}");
            AddPair("registry.details.help_missing", "Файл справки не найден: {0}", "Help file missing: {0}");
            AddPair("registry.details.installer_package_missing", "Пакет установщика не найден: {0}", "Installer package missing: {0}");
            AddPair("registry.details.obsolete_reference", "Устаревшая ссылка: {0}", "Obsolete reference: {0}");
            AddPair("registry.details.startup_missing", "Элемент автозагрузки не найден: {0}", "Startup target missing: {0}");
            AddPair("registry.details.menuorder_reset", "Сброс повреждённого порядка меню", "Reset corrupted menu order");
            AddPair("registry.details.muicache_missing", "MUI-ресурс не найден: {0}", "MUI resource missing: {0}");
            AddPair("registry.details.sound_missing", "Звуковой файл не найден: {0}", "Sound file missing: {0}");
            AddPair("registry.details.service_image_missing", "Файл службы не найден: {0}", "Service image file missing: {0}");

            AddPair("contextmenu.toggle.unsupported", "Безопасное переключение для этого типа пункта не поддерживается.", "Safe toggle is not supported for this menu item type.");
            AddPair("contextmenu.delete.unsupported", "Удаление для этой записи не поддерживается.", "Delete is not supported for this menu entry.");
            AddPair("contextmenu.col.delete", "Удал.", "Del");
            AddPair("contextmenu.btn.refresh", "Обновить", "Refresh");
            AddPair("contextmenu.confirm.delete", "Удалить выбранные пункты контекстного меню ({0})?", "Delete selected context menu entries ({0})?");
            AddPair("contextmenu.delete.cancelled", "Удаление отменено.", "Delete cancelled.");
            AddPair("contextmenu.target.any_file", "Файл", "File");
            AddPair("contextmenu.target.shortcut", "Ярлык", "Shortcut");
            AddPair("contextmenu.target.folder", "Папка", "Folder");
            AddPair("contextmenu.target.folder_background", "Фон папки", "Folder Background");
            AddPair("contextmenu.target.desktop", "Рабочий стол", "Desktop");
            AddPair("contextmenu.target.drive", "Диск", "Drive");
            AddPair("contextmenu.kind.shellex", "ShellEx Handler", "ShellEx Handler");
            AddPair("contextmenu.kind.shellverb", "Shell Verb", "Shell Verb");
            AddPair("contextmenu.scope.hkcu", "Текущий пользователь", "Current User");
            AddPair("contextmenu.scope.hklm", "Вся система", "All Users (Machine)");

            AddPair("settings.registry_expert_mode", "Экспертный режим реестра", "Registry expert mode");
            AddPair("settings.registry_expert_hint", "Открывает категории Advanced и Dangerous в разделе реестра. Используйте только если понимаете риски.", "Unlocks Advanced and Dangerous categories in the registry section. Use only if you understand the risks.");
            AddPair("settings.lang.russian", "Русский", "Russian");
            AddPair("settings.lang.english", "English", "English");
            AddPair("settings.appearance", "Оформление", "Appearance");
            AddPair("settings.theme.auto", "Auto", "Auto");
            AddPair("settings.theme.light", "Light", "Light");
            AddPair("settings.theme.dark", "Dark", "Dark");
            AddPair("settings.theme.using", "Используется тема: {0}", "Using {0} theme");
            AddPair("settings.theme.auto.status", "Автовыбор по системе", "Auto-detected theme");

            AddPair("cleaner.section.system_files", "Системные файлы", "System Files");
            AddPair("privacy.section.browser_data", "Данные браузеров", "Browser Data");
            AddPair("registry.scan_categories", "Категории сканирования", "Scan Categories");
            AddPair("registry.scan_results", "Результаты сканирования", "Scan Results");
            AddPair("registry.empty_hint", "Запустите сканирование для поиска проблем реестра", "Run a scan to find registry issues");
        }

        public static void Initialize()
        {
            try
            {
                var saved = AppSettings.Language;
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
        public static readonly LocalizedString SettingsRegistryExpertMode = new LocalizedString("settings.registry_expert_mode");
        public static readonly LocalizedString SettingsRegistryExpertHint = new LocalizedString("settings.registry_expert_hint");
        public static readonly LocalizedString SettingsLangRussian = new LocalizedString("settings.lang.russian");
        public static readonly LocalizedString SettingsLangEnglish = new LocalizedString("settings.lang.english");
        public static readonly LocalizedString SettingsAppearance = new LocalizedString("settings.appearance");
        public static readonly LocalizedString SettingsThemeAuto = new LocalizedString("settings.theme.auto");
        public static readonly LocalizedString SettingsThemeLight = new LocalizedString("settings.theme.light");
        public static readonly LocalizedString SettingsThemeDark = new LocalizedString("settings.theme.dark");

        public static readonly LocalizedString CleanerSectionSystemFiles = new LocalizedString("cleaner.section.system_files");
        public static readonly LocalizedString PrivacySectionBrowserData = new LocalizedString("privacy.section.browser_data");
        public static readonly LocalizedString RegistryScanCategories = new LocalizedString("registry.scan_categories");
        public static readonly LocalizedString RegistryScanResults = new LocalizedString("registry.scan_results");
        public static readonly LocalizedString RegistryEmptyHint = new LocalizedString("registry.empty_hint");

        public static readonly LocalizedString ColSource = new LocalizedString("col.source");
        public static readonly LocalizedString ColSize = new LocalizedString("col.size");
        public static readonly LocalizedString ColCategory = new LocalizedString("col.category");
        public static readonly LocalizedString ColKey = new LocalizedString("col.key");
        public static readonly LocalizedString ColDetails = new LocalizedString("col.details");

        public static readonly LocalizedString VersionLabel = new LocalizedString("version.label");
        public static readonly LocalizedString BuildLabel = new LocalizedString("build.label");
        public static readonly LocalizedString Copyright = new LocalizedString("copyright.label");

        public static readonly LocalizedString StatusScanning = new LocalizedString("status.scanning");
        public static readonly LocalizedString StatusSearching = new LocalizedString("status.searching");
        public static readonly LocalizedString StatusCleaning = new LocalizedString("status.cleaning");

        public static readonly LocalizedString CleanerScanning = new LocalizedString("cleaner.scanning");
        public static readonly LocalizedString CleanerSearching = new LocalizedString("cleaner.searching");
        public static readonly LocalizedString CleanerDeleted = new LocalizedString("cleaner.deleted");

        public static readonly LocalizedString PrivacyScanning = new LocalizedString("privacy.scanning");
        public static readonly LocalizedString PrivacySearching = new LocalizedString("privacy.searching");

        public static readonly LocalizedString RegistryCategories = new LocalizedString("registry.categories");
        public static readonly LocalizedString RegistryScanning = new LocalizedString("registry.scanning");
        public static readonly LocalizedString RegistrySearching = new LocalizedString("registry.searching");
        public static readonly LocalizedString RegistryFixed = new LocalizedString("registry.fixed");

        public static readonly LocalizedString SummaryFiles = new LocalizedString("summary.files");
        public static readonly LocalizedString SummaryRegistry = new LocalizedString("summary.registry");

        public static readonly LocalizedString CleanerConfirmClean = new LocalizedString("cleaner.confirm.clean");
        public static readonly LocalizedString PrivacyConfirmClean = new LocalizedString("privacy.confirm.clean");
        public static readonly LocalizedString CleanerRecycleEmpty = new LocalizedString("cleaner.recycle.empty");
        public static readonly LocalizedString CleanerRecycleConfirm = new LocalizedString("cleaner.recycle.confirm");
        public static readonly LocalizedString CleanerRecycleScanning = new LocalizedString("cleaner.recycle.scanning");
        public static readonly LocalizedString CleanerRecycleCleaning = new LocalizedString("cleaner.recycle.cleaning");
        public static readonly LocalizedString CleanerRecycleFailed = new LocalizedString("cleaner.recycle.failed");
        public static readonly LocalizedString CleanerRecycleDone = new LocalizedString("cleaner.recycle.done");
        public static readonly LocalizedString OperationDone = new LocalizedString("operation.done");
        public static readonly LocalizedString OperationDoneWithWarnings = new LocalizedString("operation.done.with_warnings");

        public static readonly LocalizedString RegistryNoSelection = new LocalizedString("registry.no.selection");
        public static readonly LocalizedString RegistryConfirmFix = new LocalizedString("registry.confirm.fix");
        public static readonly LocalizedString RegistryFixedWithBackup = new LocalizedString("registry.fixed.with.backup");

        public static readonly LocalizedString TabContextMenu = new LocalizedString("tab.contextmenu");

        public static readonly LocalizedString ContextMenuColDisable = new LocalizedString("contextmenu.col.disable");
        public static readonly LocalizedString ContextMenuColDelete = new LocalizedString("contextmenu.col.delete");
        public static readonly LocalizedString ContextMenuColName = new LocalizedString("contextmenu.col.name");
        public static readonly LocalizedString ContextMenuColTarget = new LocalizedString("contextmenu.col.target");
        public static readonly LocalizedString ContextMenuColKind = new LocalizedString("contextmenu.col.kind");
        public static readonly LocalizedString ContextMenuColScope = new LocalizedString("contextmenu.col.scope");
        public static readonly LocalizedString ContextMenuColSource = new LocalizedString("contextmenu.col.source");
        public static readonly LocalizedString ContextMenuColRegistry = new LocalizedString("contextmenu.col.registry");

        public static readonly LocalizedString ContextMenuBtnApply = new LocalizedString("contextmenu.btn.apply");
        public static readonly LocalizedString ContextMenuBtnRefresh = new LocalizedString("contextmenu.btn.refresh");
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
            SettingsRegistryExpertMode.Refresh();
            SettingsRegistryExpertHint.Refresh();
            SettingsLangRussian.Refresh();
            SettingsLangEnglish.Refresh();
            SettingsAppearance.Refresh();
            SettingsThemeAuto.Refresh();
            SettingsThemeLight.Refresh();
            SettingsThemeDark.Refresh();

            CleanerSectionSystemFiles.Refresh();
            PrivacySectionBrowserData.Refresh();
            RegistryScanCategories.Refresh();
            RegistryScanResults.Refresh();
            RegistryEmptyHint.Refresh();

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
            PrivacyConfirmClean.Refresh();
            CleanerRecycleEmpty.Refresh();
            CleanerRecycleConfirm.Refresh();
            CleanerRecycleScanning.Refresh();
            CleanerRecycleCleaning.Refresh();
            CleanerRecycleFailed.Refresh();
            CleanerRecycleDone.Refresh();
            OperationDone.Refresh();
            OperationDoneWithWarnings.Refresh();

            RegistryNoSelection.Refresh();
            RegistryConfirmFix.Refresh();
            RegistryFixedWithBackup.Refresh();

            TabContextMenu.Refresh();

            ContextMenuColDisable.Refresh();
            ContextMenuColDelete.Refresh();
            ContextMenuColName.Refresh();
            ContextMenuColTarget.Refresh();
            ContextMenuColKind.Refresh();
            ContextMenuColScope.Refresh();
            ContextMenuColSource.Refresh();
            ContextMenuColRegistry.Refresh();

            ContextMenuBtnApply.Refresh();
            ContextMenuBtnRefresh.Refresh();
            BtnDisable.Refresh();
            BtnEnable.Refresh();
        }
    }
}
