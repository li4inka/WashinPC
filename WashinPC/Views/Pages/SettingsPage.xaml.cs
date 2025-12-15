using System.Windows;
using System.Windows.Controls;
using WashinPC.Core;


namespace WashinPC.Views.Pages
{
    public partial class SettingsPage : UserControl
    {
        private bool _init;

        public SettingsPage()
        {
            InitializeComponent();

            _init = true;

            // Language
            CmbLanguage.SelectedIndex = (Localizer.Current == Lang.RU) ? 0 : 1; // :contentReference[oaicite:2]{index=2}

            _init = false;
        }

        private void LanguageChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_init) return;

            Localizer.Current = (CmbLanguage.SelectedIndex == 0) ? Lang.RU : Lang.EN; // :contentReference[oaicite:4]{index=4}
            RestartApp();
        }

        private static void RestartApp()
        {
            // WPF вариант "Application.Restart()" из WinForms:
            var exe = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            System.Diagnostics.Process.Start(exe);
            Application.Current.Shutdown();
        }
    }
}
