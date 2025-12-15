using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace WashinPC.Views.Pages
{
    public partial class CleanerPage : UserControl
    {
        private CleanerEngine _engine = new CleanerEngine();

        private ObservableCollection<CleanerItemView> _items =
            new ObservableCollection<CleanerItemView>();

        public CleanerPage()
        {
            InitializeComponent();          // ✅ теперь существует
            Cards.ItemsSource = _items;     // ✅ Cards существует
            UpdateSummary(0, 0);
        }

        private async void Scan_Click(object sender, RoutedEventArgs e)
        {
            _items.Clear();

            await Task.Run(() =>
            {
                int totalFiles;
                long totalBytes;

                var groups = _engine.ScanDetailed(out totalFiles, out totalBytes);

                Dispatcher.Invoke(() =>
                {
                    foreach (var g in groups)
                    {
                        _items.Add(new CleanerItemView
                        {
                            Name = g.GroupName,
                            TotalBytes = g.TotalBytes,
                            Files = new ObservableCollection<string>(g.Files)
                        });
                    }

                    UpdateSummary(totalFiles, totalBytes);
                });
            });
        }

        private void Clean_Click(object sender, RoutedEventArgs e)
        {
            var files = _items
                .Where(i => i.IsChecked)
                .SelectMany(i => i.Files)
                .Distinct()
                .ToList();

            if (files.Count == 0)
                return;

            foreach (var f in files)
                Utils.SafeDeleteFile(f, null);

            Scan_Click(null, null);
        }

        private void Recycle_Click(object sender, RoutedEventArgs e)
        {
            Native.EmptyRecycleBinQuiet();
        }

        private void AggressiveChanged(object sender, RoutedEventArgs e)
        {
            _engine.AggressiveMode = ChkAggressive.IsChecked == true;
            _engine.InitRules();
        }

        private void UpdateSummary(int files, long bytes)
        {
            LblSummary.Text = $"Файлов: {files}, Размер: {bytes / (1024 * 1024)} MB";
        }
    }
}
