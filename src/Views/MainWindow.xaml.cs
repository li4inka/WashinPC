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

            PageViewport.SizeChanged += (sender, args) => {
                ContentHost.Width = Math.Max(760, PageViewport.ActualWidth - 1);
                ContentHost.Height = Math.Max(480, PageViewport.ActualHeight - 1);
            };
            DataContext = new MainViewModel();
            var area = SystemParameters.WorkArea;
            MinWidth = Math.Min(MinWidth, area.Width); MinHeight = Math.Min(MinHeight, area.Height);
            Width = Math.Min(Width, area.Width); Height = Math.Min(Height, area.Height);
            Closing += (sender, args) => { if (AppUiState.Instance.IsBusy) { args.Cancel = true; AppUiState.Instance.Cancel(); } };
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
