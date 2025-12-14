using System;
using System.Collections.Generic;

namespace WashinPC
{
    public enum Lang { RU, EN }

    public static class Localizer
    {
        public static Lang Current = Lang.RU;

        static readonly Dictionary<string, string> RU = new Dictionary<string, string> {
            {"app.title", "WashinPC"},
            {"tab.cleaner", "Очистка"},
            {"tab.registry", "Реестр"},
            {"tab.privacy", "Приватность"},
            {"tab.settings", "Настройки"},
            {"btn.scan", "Сканировать"},
            {"btn.clean", "Очистить"},
            {"btn.fix", "Исправить"},
            {"btn.recycle", "Очистить Корзину"},
            {"chk.aggr", "Агрессивный режим"},

            {"status.found.items", "Найдено файлов: {0}"},
            {"status.deleted.items", "Удалено объектов: {0}"},
            {"status.found.issues", "Найдено проблем: {0}"},

            {"confirm.clean", "Выполнить очистку отмеченных областей? (безвозвратно)"},
            {"confirm.fix", "Исправить отмеченные проблемы реестра? (рекомендуется сделать бэкап)"},
            {"fixed.count", "Исправлено: {0}"},
            {"label.total", "Найдено файлов: {0}, размер: {1} MB ({2} GB)"},
            {"label.total.privacy", "Выбрано: {0} файлов, размер: {1} MB ({2} GB)"},
            {"none.selected", "Ничего не выбрано"},
            {"recycle.done", "Корзина очищена"},
            {"settings.language", "Язык интерфейса"},
            {"version.label", ""},
            {"admin.required", "Требуются права администратора"},
            {"app.error", "Ошибка"}
        };

        static readonly Dictionary<string, string> EN = new Dictionary<string, string> {
            {"app.title", "WashinPC"},
            {"tab.cleaner", "Cleaner"},
            {"tab.registry", "Registry"},
            {"tab.privacy", "Privacy"},
            {"tab.settings", "Settings"},
            {"btn.scan", "Scan"},
            {"btn.clean", "Clean"},
            {"btn.fix", "Fix"},
            {"btn.recycle", "Empty Recycle Bin"},
            {"chk.aggr", "Aggressive mode"},

            {"status.found.items", "Found files: {0}"},
            {"status.deleted.items", "Deleted items: {0}"},
            {"status.found.issues", "Found issues: {0}"},

            {"confirm.clean", "Proceed with cleaning selected areas? (irreversible)"},
            {"confirm.fix", "Fix the selected registry entries? (backup recommended)"},
            {"fixed.count", "Fixed: {0}"},
            {"label.total", "Found files: {0}, size: {1} MB ({2} GB)"},
            {"label.total.privacy", "Selected: {0} files, size: {1} MB ({2} GB)"},
            {"none.selected", "Nothing selected"},
            {"recycle.done", "Recycle Bin emptied"},
            {"settings.language", "Interface language"},
            {"version.label", ""},
            {"admin.required", "Administrator privileges required"},
            {"app.error", "Error"}
        };

        public static string T(string key, params object[] args)
        {
            string s;
            var dict = Current == Lang.RU ? RU : EN;
            if (!dict.TryGetValue(key, out s)) s = key;
            if (args != null && args.Length > 0) return string.Format(s, args);
            return s;
        }
    }
}