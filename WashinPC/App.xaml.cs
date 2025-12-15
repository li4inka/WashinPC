using System;
using System.Windows;
using WashinPC.Core;

namespace WashinPC
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            AppBootstrap.Initialize();

            LoadTheme(AppSettings.DarkMode);

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

            var win = new MainWindow();
            MainWindow = win;
            win.Show();
        }

        public static void LoadTheme(bool dark)
        {
            var dict = new ResourceDictionary
            {
                Source = new Uri(
                    dark
                        ? "Styles/Theme.Dark.xaml"
                        : "Styles/Theme.Light.xaml",
                    UriKind.Relative)
            };

            Current.Resources.MergedDictionaries.Clear();
            Current.Resources.MergedDictionaries.Add(dict);
            Current.Resources.MergedDictionaries.Add(
                new ResourceDictionary { Source = new Uri("Styles/Controls.xaml", UriKind.Relative) });
        }
    }
}
