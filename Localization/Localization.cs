using WashinPC.Core;

namespace WashinPC.Localization
{
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

        public static readonly LocalizedString AppTitle =
            new LocalizedString("app.title");

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
            AppTitle.Refresh();

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
