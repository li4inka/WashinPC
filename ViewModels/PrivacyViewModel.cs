using System.Collections.Specialized;
using System.Collections.Generic;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using WashinPC.Core.Logging;
using WashinPC.Core.Mvvm;
using WashinPC.Core.Utils;
using WashinPC.Engines.Privacy;
using WashinPC.Localization;
using WashinPC.Services;
using WashinPC.ViewModels.Common;

namespace WashinPC.ViewModels
{
    public sealed class PrivacyViewModel : BaseViewModel
    {
        private readonly PrivacyEngine _engine = new PrivacyEngine();

        public ObservableCollection<SizeItemViewModel> Items { get; }
            = new ObservableCollection<SizeItemViewModel>();

        private int _totalFiles;
        private long _totalBytes;

        public string SummaryText =>
            string.Format(
                Loc.SummaryFiles.Value,
                _totalFiles,
                FileSizeFormatter.Format(_totalBytes));

        public RelayCommand ScanCommand { get; }
        public RelayCommand CleanCommand { get; }

        public PrivacyViewModel()
        {
            ScanCommand = new RelayCommand(async () => await ScanAsync(), () => !IsBusy);
            CleanCommand = new RelayCommand(async () => await CleanAsync(), () => !IsBusy && Items.Any(i => i.IsChecked));

            Localizer.LanguageChanged += () =>
            {
                foreach (var item in Items)
                    item.RefreshLocalization();

                OnPropertyChanged(nameof(SummaryText));
            };

            Items.CollectionChanged += OnItemsCollectionChanged;
        }

        protected override void OnBusyChanged()
        {
            ScanCommand.RaiseCanExecuteChanged();
            CleanCommand.RaiseCanExecuteChanged();
        }

        private async Task ScanAsync()
        {
            var operationId = "privacy-scan-" + System.Guid.NewGuid().ToString("N");
            AppLogger.Info("privacy", "scan.start", "Starting privacy scan.", operationId);

            try
            {
                SetBusyText(Loc.PrivacyScanning.Value);
                SetStatus(Loc.PrivacySearching.Value);
                IsBusy = true;

                Items.Clear();

                int files = 0;
                long bytes = 0;

                var groups = await Task.Run(() =>
                    _engine.Scan(out files, out bytes));

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
                AppLogger.Info("privacy", "scan.done", "Privacy scan completed. Files: " + files + ", Bytes: " + bytes, operationId);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task CleanAsync()
        {
            if (DialogService.ShowConfirm(Loc.PrivacyConfirmClean.Value) != DialogResult.Yes)
                return;

            var operationId = "privacy-clean-" + System.Guid.NewGuid().ToString("N");
            AppLogger.Info("privacy", "clean.start", "Starting privacy cleanup.", operationId);

            var selectedKeys = Items
                .Where(i => i.IsChecked)
                .Select(i => i.Title?.Key)
                .Where(k => !string.IsNullOrWhiteSpace(k))
                .ToList();

            bool completed = false;
            bool hasWarnings = false;

            try
            {
                SetBusyText(Loc.StatusCleaning.Value);
                SetStatus(Loc.StatusCleaning.Value);
                IsBusy = true;

                var plannedToDelete = await Task.Run(() =>
                    EstimatePrivacyCandidateCount(selectedKeys));

                var deleted = await Task.Run(() => _engine.CleanSelectedGroups(selectedKeys));

                Items.Clear();
                _totalFiles = 0;
                _totalBytes = 0;
                OnPropertyChanged(nameof(SummaryText));
                hasWarnings = deleted < plannedToDelete;
                completed = true;
                AppLogger.Info("privacy", "clean.done", "Privacy cleanup finished. Deleted: " + deleted, operationId);
            }
            finally
            {
                IsBusy = false;
            }

            if (completed)
            {
                if (hasWarnings)
                    DialogService.ShowWarning(Loc.OperationDoneWithWarnings.Value);
                else
                    DialogService.ShowInfo(Loc.OperationDone.Value);
            }
        }

        private void OnItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (var oldItem in e.OldItems.OfType<SizeItemViewModel>())
                    oldItem.PropertyChanged -= OnPrivacyItemPropertyChanged;
            }

            if (e.NewItems != null)
            {
                foreach (var newItem in e.NewItems.OfType<SizeItemViewModel>())
                    newItem.PropertyChanged += OnPrivacyItemPropertyChanged;
            }

            CleanCommand.RaiseCanExecuteChanged();
        }

        private void OnPrivacyItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SizeItemViewModel.IsChecked))
                CleanCommand.RaiseCanExecuteChanged();
        }

        private int EstimatePrivacyCandidateCount(ICollection<string> selectedKeys)
        {
            var keySet = new HashSet<string>(
                selectedKeys ?? new string[0],
                System.StringComparer.OrdinalIgnoreCase);

            if (keySet.Count == 0)
                return 0;

            int totalFiles;
            long totalBytes;
            var groups = _engine.Scan(out totalFiles, out totalBytes);

            return groups
                .Where(g => keySet.Contains(g.LocalizationKey))
                .SelectMany(g => g.Files ?? Enumerable.Empty<string>())
                .Where(f => !string.IsNullOrWhiteSpace(f))
                .Distinct(System.StringComparer.OrdinalIgnoreCase)
                .Count();
        }
    }
}
