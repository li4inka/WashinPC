using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace WashinPC.Views.Pages
{
    public partial class PrivacyPage : UserControl
    {
        private readonly PrivacyEngine _engine = new PrivacyEngine();

        private readonly ObservableCollection<CleanerItemView> _items =
            new ObservableCollection<CleanerItemView>();

        public PrivacyPage()
        {
            InitializeComponent();
            List.ItemsSource = _items;
            UpdateSummary(0, 0);
        }

        private void Scan_Click(object sender, RoutedEventArgs e)
        {
            _items.Clear();

            int totalFiles;
            long totalBytes;

            List<GroupResult> groups = _engine.Scan(out totalFiles, out totalBytes);

            foreach (GroupResult g in groups)
            {
                _items.Add(new CleanerItemView
                {
                    Name = g.GroupName,
                    TotalBytes = g.TotalBytes,
                    Files = new ObservableCollection<string>(g.Files)
                });
            }

            UpdateSummary(totalFiles, totalBytes);
        }

        private void Clean_Click(object sender, RoutedEventArgs e)
        {
            List<string> files = _items
                .Where(i => i.IsChecked)
                .SelectMany(i => i.Files)
                .Distinct()
                .ToList();

            if (files.Count == 0)
                return;

            MessageBoxResult confirm = MessageBox.Show(
                "Очистить выбранные элементы?",
                "WashinPC",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes)
                return;

            _engine.CleanSelected(files, new List<string>());

            Scan_Click(null, null);
        }

        private void UpdateSummary(int files, long bytes)
        {
            LblSummary.Text =
                $"Файлов: {files}, Размер: {(bytes / (1024.0 * 1024)):0.00} МБ";
        }
    }
}
