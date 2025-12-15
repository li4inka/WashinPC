using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace WashinPC.Views.Pages
{
    public partial class RegistryPage : UserControl
    {
        private readonly ObservableCollection<RegistryIssueView> _items =
            new ObservableCollection<RegistryIssueView>();

        private readonly RegistryScanner _scanner = new RegistryScanner();

        public RegistryPage()
        {
            InitializeComponent();
            List.ItemsSource = _items;
            UpdateSummary();
        }

        // ===== SCAN =====
        private void Scan_Click(object sender, RoutedEventArgs e)
        {
            _items.Clear();

            var issues = _scanner.ScanAll();

            foreach (var i in issues)
            {
                _items.Add(new RegistryIssueView
                {
                    IsSelected = true,
                    Category = i.Category,
                    KeyPath = i.KeyPath,
                    ValueName = i.ValueName,
                    Details = i.Details,
                    Source = i
                });
            }

            UpdateSummary();
        }

        // ===== FIX =====
        private void Fix_Click(object sender, RoutedEventArgs e)
        {
            var selected = _items
                .Where(i => i.IsSelected && i.Source != null)
                .ToList();

            if (selected.Count == 0)
            {
                MessageBox.Show("Ничего не выбрано");
                return;
            }

            var confirm = MessageBox.Show(
                "Исправить выбранные проблемы реестра?\n(Рекомендуется сделать резервную копию)",
                "WashinPC",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes)
                return;

            int fixedCount = 0;

            foreach (var item in selected)
            {
                try
                {
                    // ВАЖНО: Fix — это делегат внутри RegistryIssue
                    item.Source.Fix?.Invoke(false);
                    fixedCount++;
                }
                catch { }
            }

            MessageBox.Show(
                $"Исправлено записей: {fixedCount}",
                "WashinPC",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            Scan_Click(null, null);
        }

        private void UpdateSummary()
        {
            LblSummary.Text = $"Найдено записей: {_items.Count}";
        }
    }
}
