using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using WashinPC.Engines.Privacy;
using WashinPC.Services;
using WashinPC.UI;
using WashinPC.UI.Dialogs;

namespace WashinPC.ViewModels.Pages
{
    public class PrivacyViewModel : BaseViewModel
    {
        private readonly PrivacyEngine _engine = new PrivacyEngine();

        // ===================== DATA =====================

        public ObservableCollection<CleanerItemView> Items { get; } =
            new ObservableCollection<CleanerItemView>();

        private string _summaryText = "Файлов: 0, Размер: 0 MB";
        public string SummaryText
        {
            get => _summaryText;
            private set
            {
                _summaryText = value;
                OnPropertyChanged();
            }
        }

        // ===================== COMMANDS =====================

        public ICommand ScanCommand { get; }
        public ICommand CleanCommand { get; }

        // ===================== CTOR =====================

        public PrivacyViewModel()
        {
            ScanCommand = new RelayCommand(
                async () => await ScanAsync(),
                () => !IsBusy);

            CleanCommand = new RelayCommand(
                () => Clean(),
                () => !IsBusy && Items.Any(i => i.IsChecked));
        }

        protected override void OnBusyChanged()
        {
            (ScanCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (CleanCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        // ===================== LOGIC =====================

        private async Task ScanAsync()
        {
            try
            {
                SetBusyText("Сканирование данных приватности…");
                SetStatus("Поиск истории, кэша и cookies…");
                IsBusy = true;

                Items.Clear();

                var result = await Task.Run(() =>
                {
                    int totalFiles;
                    long totalBytes;
                    var groups = _engine.Scan(out totalFiles, out totalBytes);
                    return (groups, totalFiles, totalBytes);
                });

                foreach (var g in result.groups)
                {
                    Items.Add(new CleanerItemView
                    {
                        Name = g.GroupName,
                        TotalBytes = g.TotalBytes,
                        Files = new ObservableCollection<string>(g.Files)
                    });
                }

                UpdateSummary(result.totalFiles, result.totalBytes);
                SetStatus($"Найдено файлов приватности: {result.totalFiles}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void Clean()
        {
            var files = Items
                .Where(i => i.IsChecked)
                .SelectMany(i => i.Files)
                .Distinct()
                .ToList();

            if (files.Count == 0)
            {
                DialogService.ShowInfo("Нет выбранных данных для очистки.");
                return;
            }

            if (DialogService.ShowConfirm(
                    "Очистить выбранные данные приватности?\nИстория и cookies будут удалены.")
                != DialogResult.Yes)
                return;

            try
            {
                SetBusyText("Очистка данных приватности…");
                SetStatus("Удаление истории и cookies…");
                IsBusy = true;

                int deleted = _engine.CleanSelected(files, null);

                SetStatus($"Удалено файлов приватности: {deleted}");
            }
            finally
            {
                IsBusy = false;
            }

            _ = ScanAsync();
        }

        private void UpdateSummary(int files, long bytes)
        {
            SummaryText =
                $"Файлов: {files}, Размер: {(bytes / (1024.0 * 1024)):0.00} MB";
        }
    }
}
