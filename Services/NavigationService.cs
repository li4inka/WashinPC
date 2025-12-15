using System;
using System.Collections.Generic;
using System.Windows.Controls;
using WashinPC.Views.Pages;

namespace WashinPC.Services
{
    public static class NavigationService
    {
        private static ContentControl _host;

        private static readonly Dictionary<PageType, Func<UserControl>> _pages =
            new Dictionary<PageType, Func<UserControl>>
            {
                { PageType.Cleaner, () => new CleanerPage() },
                { PageType.Privacy, () => new PrivacyPage() },
                { PageType.Registry, () => new RegistryPage() },
                { PageType.Settings, () => new SettingsPage() }
            };

        public static void Initialize(ContentControl host)
        {
            _host = host;
        }

        public static void Navigate(PageType page)
        {
            if (_host == null)
                throw new InvalidOperationException("NavigationService not initialized");

            if (_pages.TryGetValue(page, out var factory))
                _host.Content = factory();
        }
    }
}
