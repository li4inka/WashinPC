using System.Windows;
using System.Windows.Controls;
using WashinPC.Core;
using WashinPC.Views.Pages;

namespace WashinPC
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Title = Localizer.T("app.title");

            Sidebar.SelectedIndex = 0;
            LoadPage("Cleaner");

            DataContext = new AppInfo();
        }

        private void Sidebar_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListBoxItem item = Sidebar.SelectedItem as ListBoxItem;
            if (item != null)
                LoadPage(item.Tag.ToString());
        }

        private void LoadPage(string key)
        {
            if (key == "Cleaner")
                ContentHost.Content = new CleanerPage();
            else if (key == "Privacy")
                ContentHost.Content = new PrivacyPage();
            else if (key == "Registry")
                ContentHost.Content = new RegistryPage();
            else if (key == "Settings")
                ContentHost.Content = new SettingsPage();
            else
                ContentHost.Content = null;
        }
    }
}
