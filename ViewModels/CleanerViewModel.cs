using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using WashinPC.Core;
using WashinPC.Core.Logging;
using WashinPC.Core.Mvvm;
using WashinPC.Core.Utils;
using WashinPC.Engines.Cleaner;
using WashinPC.Localization;
using WashinPC.Services;
using WashinPC.ViewModels.Common;

namespace WashinPC.ViewModels
{
    public sealed class CleanerViewModel : BaseViewModel
    {
        private readonly CleanerEngine _engine = new CleanerEngine();

        public ObservableCollection<SizeItemViewModel> Items { get; }
            = new ObservableCollection<SizeItemViewModel>();

        private int _totalFiles;
        private long _totalBytes;

        public string SummaryText =>
            string.Format(
                Loc.SummaryFiles.Value,
                _totalFiles,
                FileSizeFormatter.Format(_totalBytes));

        public bool IsAggressive
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

            Localizer.LanguageChanged += () =>
            {
                foreach (var item in Items)
                    item.RefreshLocalization();

                OnPropertyChanged(nameof(SummaryText));
            };
        }

        protected override void OnBusyChanged()
        {
            ScanCommand.RaiseCanExecuteChanged();
            CleanCommand.RaiseCanExecuteChanged();
            RecycleCommand.RaiseCanExecuteChanged();
        }

        private async Task ScanAsync()
        {
            var operationId = "cleaner-scan-" + System.Guid.NewGuid().ToString("N");
            AppLogger.Info("cleaner", "scan.start", "Starting cleaner scan.", operationId);

            try
            {
                SetBusyText(Loc.CleanerScanning.Value);
                SetStatus(Loc.CleanerSearching.Value);
                IsBusy = true;

                Items.Clear();

                int files = 0;
                long bytes = 0;

                var groups = await Task.Run(() =>
                    _engine.ScanDetailed(out files, out bytes));

                foreach (var g in groups)
                {
                    Items.Add(new SizeItemViewModel
                    {
                        Title = new LocalizedString(g.LocalizationKey),
                        TotalBytes = g.TotalBytes
                    });
                }

                _totalFiles = files;
                _totalBytes = bytes;
                OnPropertyChanged(nameof(SummaryText));
                AppLogger.Info("cleaner", "scan.done", "Cleaner scan completed. Files: " + files + ", Bytes: " + bytes, operationId);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task CleanAsync()
        {
            if (DialogService.ShowConfirm(Loc.CleanerConfirmClean.Value)
                != DialogResult.Yes)
                return;

            var operationId = "cleaner-clean-" + System.Guid.NewGuid().ToString("N");
            AppLogger.Info("cleaner", "clean.start", "Starting cleaner cleanup.", operationId);

            try
            {
                SetBusyText(Loc.StatusCleaning.Value);
                SetStatus(Loc.StatusCleaning.Value);
                IsBusy = true;

                var selectedKeys = Items
                    .Where(i => i.IsChecked)
                    .Select(i => i.Title?.Key)
                    .Where(k => !string.IsNullOrWhiteSpace(k))
                    .ToList();

                int deleted = await Task.Run(() =>
                    _engine.CleanSelectedGroups(selectedKeys));

                Items.Clear();
                _totalFiles = 0;
                _totalBytes = 0;
                OnPropertyChanged(nameof(SummaryText));
                SetStatus(string.Format(Loc.CleanerDeleted.Value, deleted));
                AppLogger.Info("cleaner", "clean.done", "Cleaner cleanup finished. Deleted: " + deleted, operationId);
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
                DialogService.ShowInfo(Loc.CleanerRecycleEmpty.Value);
                return;
            }

            if (DialogService.ShowConfirm(Loc.CleanerRecycleConfirm.Value)
                != DialogResult.Yes)
                return;

            try
            {
                SetBusyText(Loc.CleanerRecycleCleaning.Value);
                SetStatus(Loc.CleanerRecycleCleaning.Value);
                IsBusy = true;

                if (!Native.EmptyRecycleBinQuiet())
                {
                    DialogService.ShowWarning(Loc.CleanerRecycleFailed.Value);
                    AppLogger.Warn("cleaner", "recycle.fail", "Recycle bin cleanup failed.");
                    return;
                }

                SetStatus(Loc.CleanerRecycleDone.Value);
                AppLogger.Info("cleaner", "recycle.done", "Recycle bin cleaned.");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
