using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using WashinPC.Engines.Cleaner;
using WashinPC.Services;
using WashinPC.UI;
using WashinPC.UI.Dialogs;

namespace WashinPC.ViewModels.Pages
{
    public class CleanerViewModel : BaseViewModel
    {
        private readonly CleanerEngine _engine = new CleanerEngine();

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

        private bool _isAggressive;
        public bool IsAggressive
        {
            get => _isAggressive;
            set
            {
                if (_isAggressive == value) return;
                _isAggressive = value;

                _engine.AggressiveMode = value;
                _engine.InitRules();

                OnPropertyChanged();
            }
        }

        // ===================== COMMANDS =====================

        public ICommand ScanCommand { get; }
        public ICommand CleanCommand { get; }
        public ICommand RecycleCommand { get; }

        // ===================== CTOR =====================

        public CleanerViewModel()
        {
            ScanCommand = new RelayCommand(
                async () => await ScanAsync(),
                () => !IsBusy);

            CleanCommand = new RelayCommand(
                () => Clean(),
                () => !IsBusy && Items.Any(i => i.IsChecked));

            RecycleCommand = new RelayCommand(
                () => Native.EmptyRecycleBinQuiet(),
                () => !IsBusy);
        }

        protected override void OnBusyChanged()
        {
            (ScanCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (CleanCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (RecycleCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        // ===================== LOGIC =====================

        private async Task ScanAsync()
        {
            try
            {
                SetBusyText("Сканирование очистки…");
                SetStatus("Сканирование временных файлов…");
                IsBusy = true;

                Items.Clear();

                var result = await Task.Run(() =>
                {
                    int totalFiles;
                    long totalBytes;
                    var groups = _engine.ScanDetailed(out totalFiles, out totalBytes);
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
                SetStatus($"Найдено файлов: {result.totalFiles}");
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
                DialogService.ShowInfo("Нет выбранных элементов для очистки.");
                return;
            }

            if (DialogService.ShowConfirm(
                    "Очистить выбранные файлы без возможности восстановления?")
                != DialogResult.Yes)
                return;

            try
            {
                SetBusyText("Очистка файлов…");
                SetStatus("Удаление временных файлов…");
                IsBusy = true;

                int deleted = 0;
                foreach (var f in files)
                    if (Utils.SafeDeleteFile(f, null))
                        deleted++;

                SetStatus($"Удалено файлов: {deleted}");
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
