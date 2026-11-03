using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Win32;
using WashinPC.Core.Mvvm;
using WashinPC.Engines.Registry;
using WashinPC.Localization;
using WashinPC.Services;

namespace WashinPC.ViewModels
{
    internal sealed class BackupsViewModel : BaseViewModel, IDisposable
    {
        private BackupEntry _selected;
        private RestorePreview _preview;
        private bool _view32, _view64;
        private string _message;
        public ObservableCollection<BackupEntry> Entries { get; } = new ObservableCollection<BackupEntry>();
        public ObservableCollection<RestoreDifference> Differences { get; } = new ObservableCollection<RestoreDifference>();
        public AppUiState Ui => AppUiState.Instance;
        public string Title => Localizer.T("op.backups");
        public string DateLabel => Localizer.T("backup.date");
        public string TargetsLabel => Localizer.T("backup.targets");
        public string ViewLabel => Localizer.T("backup.view");
        public string StatusLabel => Localizer.T("backup.status");
        public string CurrentLabel => Localizer.T("backup.current");
        public string SavedLabel => Localizer.T("backup.saved");
        public string CompareLabel => Localizer.T("backup.compare");
        public string RestoreLabel => Localizer.T("backup.restore");
        public string FolderLabel => Localizer.T("backup.folder");
        public string RefreshLabel => Localizer.T("backup.refresh");
        public string Help => Localizer.T("backup.help");
        public string Message { get => _message; private set { _message = value; OnPropertyChanged(); } }
        public bool NeedsView => Selected != null && !Selected.IsService && !Selected.View.HasValue;
        public bool View32 { get => _view32; set { if (_view32 == value) return; _view32 = value; InvalidatePreview(); OnPropertyChanged(); } }
        public bool View64 { get => _view64; set { if (_view64 == value) return; _view64 = value; InvalidatePreview(); OnPropertyChanged(); } }
        public BackupEntry Selected
        {
            get => _selected;
            set { _selected = value; _view32 = _view64 = false; InvalidatePreview(); Message = value?.Error ?? ""; OnPropertyChanged(); OnPropertyChanged(nameof(NeedsView)); OnPropertyChanged(nameof(View32)); OnPropertyChanged(nameof(View64)); }
        }
        public RelayCommand RefreshCommand { get; }
        public RelayCommand CompareCommand { get; }
        public RelayCommand RestoreCommand { get; }
        public RelayCommand FolderCommand { get; }
        public BackupsViewModel()
        {
            RefreshCommand = new RelayCommand(async () => await RefreshAsync(), () => CanStart);
            CompareCommand = new RelayCommand(async () => await CompareAsync(), () => CanStart && Selected != null && Selected.Error == null && (!NeedsView || View32 || View64));
            RestoreCommand = new RelayCommand(async () => await RestoreAsync(), () => CanStart && _preview != null && _preview.Differences.Count > 0);
            FolderCommand = new RelayCommand(() =>
            {
                try { Process.Start(new ProcessStartInfo("explorer.exe", "\"" + RegistryBackup.GetBackupRoot() + "\"") { UseShellExecute = true }); }
                catch { Message = Localizer.T("op.failed"); }
            }, () => CanStart);
            Localizer.LanguageChanged += LanguageChanged;
        }
        internal Task RefreshAsync() => RunOperation(async token =>
        {
            Selected = null; Entries.Clear();
            var entries = await Task.Run(() => BackupCatalog.List());
            foreach (var entry in entries) Entries.Add(entry);
            Message = entries.Count == 0 ? Localizer.T("backup.empty") : "";
        });
        private Task CompareAsync() => RunOperation(async token =>
        {
            var entry = Selected;
            var view = NeedsView ? (View64 ? RegistryView.Registry64 : RegistryView.Registry32) : (RegistryView?)null;
            InvalidatePreview();
            try
            {
                _preview = await Task.Run(() => BackupCatalog.Preview(entry, view));
                foreach (var difference in _preview.Differences) Differences.Add(difference);
                Message = Differences.Count == 0 ? Localizer.T("backup.no_changes") : Localizer.T("backup.review", Differences.Count);
            }
            catch { Message = Localizer.T("backup.invalid"); }
        });
        private async Task RestoreAsync()
        {
            if (_preview == null || DialogService.ShowConfirm(Localizer.T("backup.confirm"), Title) != DialogResult.Yes) return;
            var preview = _preview;
            await RunOperation(async token =>
            {
                try
                {
                    var result = await Task.Run(() => BackupCatalog.Restore(preview, token));
                    FileMaintenanceViewModel.ShowResult(result);
                    Message = Ui.StatusText;
                }
                catch (InvalidOperationException) { Message = Localizer.T("op.changed"); }
                catch { Message = Localizer.T("op.failed"); }
                finally { InvalidatePreview(); }
            });
        }
        private void InvalidatePreview()
        {
            _preview = null; Differences.Clear(); RestoreCommand?.RaiseCanExecuteChanged(); CompareCommand?.RaiseCanExecuteChanged();
        }
        protected override void OnBusyChanged()
        {
            RefreshCommand?.RaiseCanExecuteChanged(); CompareCommand?.RaiseCanExecuteChanged(); RestoreCommand?.RaiseCanExecuteChanged(); FolderCommand?.RaiseCanExecuteChanged();
        }
        private void LanguageChanged() { InvalidatePreview(); Message = ""; OnPropertyChanged(null); }
        public void Dispose() { Localizer.LanguageChanged -= LanguageChanged; }
    }
}
