using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using WashinPC.Core;
using WashinPC.Engines.Cleaner;
using WashinPC.Services;
using WashinPC.UI;
using WashinPC.UI.Dialogs;
using WashinPC.ViewModels.Common;

namespace WashinPC.ViewModels
{
    public class CleanerItemView : SizeItemViewModel
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

    public class CleanerViewModel : BaseViewModel
    {
        private readonly CleanerEngine _engine = new CleanerEngine();

        public ObservableCollection<CleanerItemView> Items { get; }
            = new ObservableCollection<CleanerItemView>();

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

        public bool AggressiveMode
        {
            get => _engine.AggressiveMode;
            set
            {
                if (_engine.AggressiveMode == value) return;
                _engine.AggressiveMode = value;
                _engine.InitRules();
                OnPropertyChanged();
            }
        }

        // ================= COMMANDS =================

        public RelayCommand ScanCommand { get; }
        public RelayCommand CleanCommand { get; }
        public RelayCommand RecycleCommand { get; }

        public CleanerViewModel()
        {
            ScanCommand = new RelayCommand(
                async () => await ScanAsync(),
                () => !IsBusy);

            CleanCommand = new RelayCommand(
                async () => await CleanAsync(),
                () => !IsBusy && Items.Any(i => i.IsChecked));

            RecycleCommand = new RelayCommand(
                EmptyRecycleBin,
                () => !IsBusy);
        }

        protected override void OnBusyChanged()
        {
            ScanCommand.RaiseCanExecuteChanged();
            CleanCommand.RaiseCanExecuteChanged();
            RecycleCommand.RaiseCanExecuteChanged();
        }

        // ================= LOGIC =================

        private async Task ScanAsync()
        {
            try
            {
                SetBusyText("Сканирование очистки…");
                SetStatus("Поиск временных файлов…");
                IsBusy = true;

                Items.Clear();

                int totalFiles = 0;
                long totalBytes = 0;

                var groups = await Task.Run(() =>
                    _engine.ScanDetailed(out totalFiles, out totalBytes));


                foreach (var g in groups)
                {
                    Items.Add(new CleanerItemView
                    {
                        Name = g.GroupName,
                        FileCount = g.Files.Count,
                        TotalBytes = g.TotalBytes
                    });
                }

                SummaryText =
                    $"Файлов: {totalFiles}, Размер: {FileSizeFormatter.Format(totalBytes)}";

                SetStatus($"Найдено файлов: {totalFiles}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task CleanAsync()
        {
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
                await Task.Run(() => _engine.Clean(out deleted));


                Items.Clear();
                SummaryText = "Файлов: 0, Размер: 0 MB";

                SetStatus($"Удалено файлов: {deleted}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void EmptyRecycleBin()
        {
            if (Native.IsRecycleBinEmpty())
            {
                DialogService.ShowInfo("Корзина уже пуста.");
                return;
            }

            if (DialogService.ShowConfirm(
                    "Очистить корзину?\nВсе файлы будут удалены без возможности восстановления.")
                != DialogResult.Yes)
                return;

            try
            {
                SetBusyText("Очистка корзины…");
                SetStatus("Удаление файлов из корзины…");
                IsBusy = true;

                if (!Native.EmptyRecycleBinQuiet())
                {
                    DialogService.ShowWarning(
                        "Не удалось очистить корзину.\nПопробуйте запустить программу от имени администратора.");
                    return;
                }

                SetStatus("Корзина успешно очищена");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
