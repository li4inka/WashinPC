using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WashinPC.Core;
using WashinPC.Core.Mvvm;
using WashinPC.Core.Utils;
using WashinPC.Engines.Cleaner;
using WashinPC.Engines.Privacy;
using WashinPC.Engines.Operations;
using WashinPC.Localization;
using WashinPC.Services;
using WashinPC.ViewModels.Common;

namespace WashinPC.ViewModels
{
    public abstract class FileMaintenanceViewModel : BaseViewModel
    {
        private readonly bool _privacy;
        private readonly CleanerEngine _cleaner = new CleanerEngine();
        private readonly PrivacyEngine _browser = new PrivacyEngine();
        private FileScanPlan _plan = new FileScanPlan();
        public ObservableCollection<SizeItemViewModel> Items { get; } = new ObservableCollection<SizeItemViewModel>();
        public RelayCommand ScanCommand { get; }
        public RelayCommand CleanCommand { get; }
        public RelayCommand RecycleCommand { get; }
        public string SummaryText => Localizer.T("summary.files", _plan.Files.Count, FileSizeFormatter.Format(_plan.Files.Sum(x => x.Bytes)));
        protected FileMaintenanceViewModel(bool privacy)
        {
            _privacy = privacy;
            ScanCommand = new RelayCommand(async () => await ScanAsync(), () => CanStart);
            CleanCommand = new RelayCommand(async () => await CleanAsync(), () => CanStart && Items.Any(x => x.IsChecked));
            RecycleCommand = new RelayCommand(async () => await RecycleAsync(), () => CanStart);
            Items.CollectionChanged += (s, e) =>
            {
                if (e.NewItems != null) foreach (SizeItemViewModel item in e.NewItems)
                    item.PropertyChanged += (sender, args) => CleanCommand.RaiseCanExecuteChanged();
                CleanCommand.RaiseCanExecuteChanged();
            };
            Localizer.LanguageChanged += () => { foreach (var item in Items) item.RefreshLocalization(); OnPropertyChanged(nameof(SummaryText)); };
        }
        protected override void OnBusyChanged()
        {
            ScanCommand?.RaiseCanExecuteChanged(); CleanCommand?.RaiseCanExecuteChanged(); RecycleCommand?.RaiseCanExecuteChanged();
        }
        private OperationProgress Progress()
        {
            var progress = new Progress<int>(count => SetStatus(Localizer.T("op.progress", count)));
            return new OperationProgress(count => ((IProgress<int>)progress).Report(count));
        }
        private void Populate()
        {
            var selection = Items.ToDictionary(x => x.Title.Key, x => x.IsChecked);
            Items.Clear();
            foreach (var group in _plan.Files.GroupBy(x => x.Group))
                Items.Add(new SizeItemViewModel { Title = new LocalizedString(group.Key), TotalBytes = group.Sum(x => x.Bytes),
                    IsChecked = selection.ContainsKey(group.Key) && selection[group.Key] });
            OnPropertyChanged(nameof(SummaryText));
        }
        internal void SetPlan(FileScanPlan plan) { _plan = plan; Populate(); }
        private Task ScanAsync() => RunOperation(async token =>
        {
            SetStatus(Localizer.T("status.scanning"));
            var progress = Progress();
            SetPlan(await Task.Run(() => _privacy ? _browser.ScanPlan(token, progress) : _cleaner.ScanPlan(token, progress)));
            SetStatus(Localizer.T(_plan.Cancelled ? "op.cancelled" : "op.scan_result", _plan.Files.Count, _plan.Notices.Count));
            if (_plan.Notices.Count > 0) AppUiState.Instance.Details = Describe(_plan.Notices);
            else AppUiState.Instance.Details = "";
        });
        private async Task CleanAsync()
        {
            if (DialogService.ShowConfirm(Localizer.T(_privacy ? "op.privacy_confirm" : "op.delete_confirm")) != DialogResult.Yes) return;
            var selected = Items.Where(x => x.IsChecked).Select(x => x.Title.Key).ToArray();
            await RunOperation(async token =>
            {
                var progress = Progress();
                var result = await Task.Run(() => FileOperations.Delete(_plan, selected, token, progress));
                Populate();
                ShowResult(result);
            });
        }
        internal static string Describe(System.Collections.Generic.IEnumerable<ItemResult> items)
            => string.Join("\n", items.GroupBy(x => x.Reason ?? "op.failed").Select(g => Localizer.T(g.Key) + ": " + g.Count()));
        internal static void ShowResult(OperationResult result)
        {
            AppUiState.Instance.StatusText = Localizer.T("op.result", result.Applied, result.Skipped, result.Failed, FileSizeFormatter.Format(result.DeletedBytes))
                + (result.Cancelled ? " " + Localizer.T("op.cancelled") : "");
            AppUiState.Instance.Details = Describe(result.Items) + (result.Backups.Count > 0 ? "\n" + Localizer.T("op.backups_count", result.Backups.Count) : "");
            Core.Logging.AppLogger.Info("operation", "done", "Applied=" + result.Applied + "; skipped=" + result.Skipped + "; failed=" + result.Failed + "; bytes=" + result.DeletedBytes);
        }
        private async Task RecycleAsync()
        {
            if (DialogService.ShowConfirm(Loc.CleanerRecycleConfirm.Value) != DialogResult.Yes) return;
            await RunOperation(async token =>
            {
                if (token.IsCancellationRequested) return;
                var ok = await Task.Run(() => Native.EmptyRecycleBinQuiet());
                SetStatus(ok ? Loc.CleanerRecycleDone.Value : Loc.CleanerRecycleFailed.Value);
            });
        }
    }
}
