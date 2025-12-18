using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using WashinPC.Core;
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

        // ===== OPTIONS =====

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

        // ===== COMMANDS =====

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
                OnPropertyChanged(nameof(SummaryText));
            };
        }

        protected override void OnBusyChanged()
        {
            ScanCommand.RaiseCanExecuteChanged();
            CleanCommand.RaiseCanExecuteChanged();
            RecycleCommand.RaiseCanExecuteChanged();
        }

        // ===== LOGIC =====

        private async Task ScanAsync()
        {
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

            try
            {
                SetBusyText(Loc.StatusCleaning.Value);
                SetStatus(Loc.StatusCleaning.Value);
                IsBusy = true;

                int deleted;
                await Task.Run(() => _engine.Clean(out deleted));

                Items.Clear();
                _totalFiles = 0;
                _totalBytes = 0;
                OnPropertyChanged(nameof(SummaryText));
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
                    return;
                }

                SetStatus(Loc.CleanerRecycleDone.Value);
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
