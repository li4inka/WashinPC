using System;
using System.Windows;
using System.Windows.Controls;
using WashinPC.Services;
using WashinPC.ViewModels;

namespace WashinPC
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            DataContext = new MainViewModel();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            NavigationService.Initialize(ContentHost);

            Sidebar.SelectedIndex = 0;
            NavigationService.Navigate(PageType.Cleaner);
        }

        private void Sidebar_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Sidebar.SelectedItem is ListBoxItem item &&
                Enum.TryParse(item.Tag?.ToString(), out PageType page))
            {
                NavigationService.Navigate(page);
            }
        }
    }
}
