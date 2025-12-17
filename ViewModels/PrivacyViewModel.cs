using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using WashinPC.Core;
using WashinPC.Engines.Privacy;
using WashinPC.UI;
using WashinPC.ViewModels.Common;

namespace WashinPC.ViewModels
{
    public class PrivacyItemView : SizeItemViewModel
    {
        private int _fileCount;

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

        public RelayCommand ScanCommand { get; }
        public RelayCommand CleanCommand { get; }

        public PrivacyViewModel()
        {
            ScanCommand = new RelayCommand(
                async () => await ScanAsync(),
                () => !IsBusy);

            CleanCommand = new RelayCommand(
                async () => await CleanAsync(),
                () => !IsBusy && Items.Any(i => i.IsChecked));
        }

        protected override void OnBusyChanged()
        {
            ScanCommand.RaiseCanExecuteChanged();
            CleanCommand.RaiseCanExecuteChanged();
        }

        // ================= LOGIC =================

        private async Task ScanAsync()
        {
            try
            {
                SetBusyText("Сканирование приватности…");
                SetStatus("Поиск приватных данных…");
                IsBusy = true;

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
            finally
            {
                IsBusy = false;
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
                SetBusyText("Очистка приватных данных…");
                SetStatus("Удаление файлов…");
                IsBusy = true;

                await Task.Run(() =>
                    _engine.CleanSelected(selectedFiles, null));

                Items.Clear();
                SummaryText = "Файлов: 0, Размер: 0 MB";
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
