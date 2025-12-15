using System;
using System.Windows;
using WashinPC.Core;
using WashinPC.Localization;


namespace WashinPC
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            AppBootstrap.Initialize();

            if (!Utils.IsAdministrator())
            {
                MessageBox.Show(
                    Localizer.T("admin.required"),
                    "WashinPC",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown();
                return;
            }

            Localizer.AutoDetectLanguage();
            WashinPC.Localization.Loc.RefreshAll();

            var win = new MainWindow();
            MainWindow = win;
            win.Show();
        }
    }
}
