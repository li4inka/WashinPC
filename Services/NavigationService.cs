using System;
using System.Collections.Generic;
using System.Windows.Controls;
using WashinPC.ViewModels;
using WashinPC.Views.Pages;

namespace WashinPC.Services
{
    public static class NavigationService
    {
        private static ContentControl _host;

        private static readonly Dictionary<PageType, UserControl> _pages
            = new Dictionary<PageType, UserControl>();

        private static readonly Dictionary<PageType, object> _viewModels
            = new Dictionary<PageType, object>();

        // вызывается из MainWindow.xaml.cs
        public static void Initialize(ContentControl host)
        {
            _host = host;
        }

        // вызывается из MainWindow.xaml.cs
        public static void Navigate(PageType pageType)
        {
            if (_host == null)
                throw new InvalidOperationException("NavigationService is not initialized.");

            if (!_pages.ContainsKey(pageType))
            {
                var page = CreatePage(pageType);
                var vm = CreateViewModel(pageType);

                page.DataContext = vm;

                _pages[pageType] = page;
                _viewModels[pageType] = vm;
            }

            _host.Content = _pages[pageType];
        }

        private static UserControl CreatePage(PageType type)
        {
            switch (type)
            {
                case PageType.Cleaner:
                    return new CleanerPage();

                case PageType.Privacy:
                    return new PrivacyPage();

                case PageType.Registry:
                    return new RegistryPage();

                case PageType.Settings:
                    return new SettingsPage();

                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        private static object CreateViewModel(PageType type)
        {
            switch (type)
            {
                case PageType.Cleaner:
                    return new CleanerViewModel();

                case PageType.Privacy:
                    return new PrivacyViewModel();

                case PageType.Registry:
                    return new RegistryViewModel();

                case PageType.Settings:
                    return new SettingsViewModel();

                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
    }
}
