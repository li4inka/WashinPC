using System;
using System.Windows;
using System.Windows.Threading;
using WashinPC.Core;
using WashinPC.Core.Logging;
using WashinPC.Core.Utils;
using WashinPC.Localization;
using WashinPC.Services;

namespace WashinPC
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            AppBootstrap.Initialize();
            Localizer.Initialize();
            ThemeService.ApplyFromSettings();
            RegisterGlobalExceptionLogging();
            AppLogger.Info("app", "startup", "Application startup sequence initialized.");

            if (!Utils.IsAdministrator())
            {
                AppLogger.Warn("app", "startup.admin_required", "Application requires administrator privileges.");
                MessageBox.Show(
                    Localizer.T("admin.required"),
                    "WashinPC",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown();
                return;
            }

            var win = new MainWindow();
            MainWindow = win;
            win.Show();
            AppLogger.Info("app", "startup.ready", "Main window shown.");
        }

        protected override void OnExit(ExitEventArgs e)
        {
            AppLogger.Info("app", "shutdown", "Application shutdown.");
            base.OnExit(e);
        }

        private void RegisterGlobalExceptionLogging()
        {
            DispatcherUnhandledException += OnDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainUnhandledException;
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            AppLogger.Error("app", "exception.dispatcher", "Unhandled dispatcher exception.", e.Exception);
        }

        private void OnCurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;
            AppLogger.Error("app", "exception.domain", "Unhandled app-domain exception.", ex);
        }
    }
}