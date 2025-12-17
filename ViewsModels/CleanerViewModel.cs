using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using WashinPC.Core;
using WashinPC.Engines.Cleaner;
using WashinPC.Services;
using WashinPC.UI;
using WashinPC.UI.Dialogs;
using WashinPC.ViewModels;

namespace WashinPC.ViewsModels.Pages
{
    public class CleanerItemView : INotifyPropertyChanged
    {
        private string _name;
        private int _fileCount;
        private long _totalBytes;
        private bool _isChecked = true;

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayText)); }
        }

        public int FileCount
        {
            get => _fileCount;
            set { _fileCount = value; OnPropertyChanged(); }
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
                OnPropertyChanged(nameof(DisplayText));
            }
        }

        public bool IsChecked
        {
            get => _isChecked;
            set { _isChecked = value; OnPropertyChanged(); }
        }

        public string SizeText =>
            FileSizeFormatter.Format(TotalBytes);

        public string DisplayText =>
            $"{Name} — {SizeText}";

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string prop = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
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
            set { _summaryText = value; OnPropertyChanged(); }
        }

        public bool AggressiveMode
        {
            get => _engine.AggressiveMode;
            set
            {
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
                () => EmptyRecycleBin(),
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
            // 1️⃣ Корзина пуста — сразу выходим
            if (Native.IsRecycleBinEmpty())
            {
                DialogService.ShowInfo("Корзина уже пуста.");
                return;
            }

            // 2️⃣ Подтверждение
            if (DialogService.ShowConfirm(
                    "Очистить корзину?\nВсе файлы будут удалены без возможности восстановления.")
                != DialogResult.Yes)
                return;

            try
            {
                SetBusyText("Очистка корзины…");
                SetStatus("Удаление файлов из корзины…");
                IsBusy = true;

                bool result = Native.EmptyRecycleBinQuiet();

                if (!result)
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
