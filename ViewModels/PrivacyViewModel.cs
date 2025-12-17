using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using WashinPC.Core;
using WashinPC.Engines.Privacy;
using WashinPC.UI;

namespace WashinPC.ViewModels
{
    public class PrivacyItemView : INotifyPropertyChanged
    {
        private string _name;
        private int _fileCount;
        private long _totalBytes;
        private bool _isChecked = true;

        public string Name
        {
            get => _name;
            set
            {
                if (_name == value) return;
                _name = value;
                OnPropertyChanged();
            }
        }

        public int FileCount
        {
            get => _fileCount;
            set
            {
                if (_fileCount == value) return;
                _fileCount = value;
                OnPropertyChanged();
            }
        }

        public long TotalBytes
        {
            get => _totalBytes;
            set
            {
                if (_totalBytes == value) return;
                _totalBytes = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SizeText));
            }
        }

        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                if (_isChecked == value) return;
                _isChecked = value;
                OnPropertyChanged();
            }
        }

        public string SizeText =>
            FileSizeFormatter.Format(TotalBytes);

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string prop = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }

    public class PrivacyViewModel : BaseViewModel
    {
        private readonly PrivacyEngine _engine = new PrivacyEngine();

        public ObservableCollection<PrivacyItemView> Items { get; }
            = new ObservableCollection<PrivacyItemView>();

        private string _summaryText = "Файлов: 0, Размер: 0 MB";
        public string SummaryText
        {
            get => _summaryText;
            set
            {
                if (_summaryText == value) return;
                _summaryText = value;
                OnPropertyChanged();
            }
        }

        public RelayCommand ScanCommand => new RelayCommand(async () => await ScanAsync());
        public RelayCommand CleanCommand => new RelayCommand(async () => await CleanAsync());

        private async Task ScanAsync()
        {
            try
            {
                Items.Clear();

                int totalFiles = 0;
                long totalBytes = 0;

                var groups = await Task.Run(() =>
                    _engine.Scan(out totalFiles, out totalBytes));

                foreach (var g in groups)
                {
                    Items.Add(new PrivacyItemView
                    {
                        Name = g.GroupName,
                        FileCount = g.Files.Count,
                        TotalBytes = g.TotalBytes
                    });
                }

                SummaryText =
                    $"Файлов: {totalFiles}, Размер: {FileSizeFormatter.Format(totalBytes)}";
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Ошибка сканирования",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async Task CleanAsync()
        {
            var selectedFiles = Items
                .Where(i => i.IsChecked)
                .SelectMany(i =>
                {
                    var rule = _engine.Rules.FirstOrDefault(r => r.Name == i.Name);
                    return rule != null ? rule.FilesProvider() : new string[0];
                })
                .ToList();

            if (selectedFiles.Count == 0)
                return;

            try
            {
                var log = new System.Collections.Generic.List<string>();

                await Task.Run(() =>
                    _engine.CleanSelected(selectedFiles, log));

                Items.Clear();
                SummaryText = "Файлов: 0, Размер: 0 MB";
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка очистки");
            }
        }
    }
}
