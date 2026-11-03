using WashinPC.Engines.Operations;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using WashinPC.Core;
using WashinPC.Core.Logging;
using WashinPC.Core.Mvvm;
using WashinPC.Engines.Registry;
using WashinPC.Localization;
using WashinPC.Services;

namespace WashinPC.ViewModels
{
    public sealed class RegistryIssueView : BaseViewModel
    {
        private bool _isSelected;

        public RegistryIssue Model { get; }

        public RegistryIssueView(RegistryIssue model)
        {
            Model = model ?? throw new ArgumentNullException(nameof(model));
            _isSelected = model.CanFix;
        }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (!Model.CanFix && value) return;
                if (_isSelected == value) return;
                _isSelected = value;
                OnPropertyChanged();
            }
        }

        public bool CanFix => Model.CanFix;
        public string Category => Model.Category;
        public string KeyPath => Model.KeyPath;
        public string Details => Model.Details;


        public void RefreshText()
        {
            Model.RefreshText();
            OnPropertyChanged(nameof(Category));
            OnPropertyChanged(nameof(Details));
        }
    }

    public sealed class RegistryViewModel : BaseViewModel
    {
        private readonly RegistryScanner _scanner = new RegistryScanner();

        public ObservableCollection<RegistryCategoryView> Categories { get; }
            = new ObservableCollection<RegistryCategoryView>();

        public ObservableCollection<RegistryIssueView> Items { get; }
            = new ObservableCollection<RegistryIssueView>();

        private string _summaryText;
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

        /// <summary>
        /// Три-состояния для "выделить всё" в шапке таблицы.
        /// </summary>
        private bool? _allSelectedState = true;
        public bool? AllSelectedState
        {
            get => _allSelectedState;
            set
            {
                if (_allSelectedState == value) return;
                _allSelectedState = value;
                OnPropertyChanged();

                if (value == null) return;

                foreach (var it in Items)
                    it.IsSelected = value.Value;

                UpdateAllSelectedState();
            }
        }

        public ICommand ScanCommand { get; }
        public ICommand FixCommand { get; }

        public RegistryViewModel()
        {
            ScanCommand = new RelayCommand(async () => await ScanAsync(), () => CanStart);
            FixCommand = new RelayCommand(async () => await FixAsync(), () => CanStart && Items.Any(x => x.IsSelected && x.CanFix));

            BuildCategories();
            AppSettings.SettingChanged += OnSettingChanged;

            // обновляем локализацию списка найденных проблем и заголовков групп
            Localizer.LanguageChanged += () =>
            {
                foreach (var c in Categories)
                {
                    // в категории уже подписка и refresh, но оставим безопасно
                    // на случай если где-то были созданы иначе
                }

                foreach (var it in Items)
                    it.RefreshText();

                UpdateSummary();
            };

            UpdateSummary();
        }

        private void BuildCategories()
        {
            Categories.Clear();
            bool expertMode = AppSettings.RegistryExpertMode;

            // ключи должны совпадать с тем, что кладёт RegistryScanner в issue.CategoryKey
            Add("registry.cat.uninstall", RegistryRiskLevel.Advanced, false, expertMode);
            Add("registry.cat.shared_dlls", RegistryRiskLevel.Safe, true, true);
            Add("registry.cat.unused_file_ext", RegistryRiskLevel.Advanced, false, expertMode);
            Add("registry.cat.help_files", RegistryRiskLevel.Safe, true, true);
            Add("registry.cat.obsolete_software", RegistryRiskLevel.Advanced, false, expertMode);

            Add("registry.cat.applications", RegistryRiskLevel.Advanced, false, expertMode);
            Add("registry.cat.type_libraries", RegistryRiskLevel.Advanced, false, expertMode);
            Add("registry.cat.installer_refs", RegistryRiskLevel.Advanced, false, expertMode);
            Add("registry.cat.startup", RegistryRiskLevel.Advanced, false, expertMode);

            Add("registry.cat.active_x", RegistryRiskLevel.Dangerous, false, expertMode);
            Add("registry.cat.fonts", RegistryRiskLevel.Dangerous, false, expertMode);
            Add("registry.cat.mui_cache", RegistryRiskLevel.Dangerous, false, expertMode);
            Add("registry.cat.start_menu", RegistryRiskLevel.Dangerous, false, expertMode);
            Add("registry.cat.sound_events", RegistryRiskLevel.Dangerous, false, expertMode);

            // если у тебя есть ещё категории как в CCleaner — добавляй сюда по этому же принципу.
        }
        private void ShowCategoryInfo(string categoryKey)
        {
            DialogService.ShowWarning(
                Localizer.T("registry.cat.info." + categoryKey), Localizer.T("registry.info.title")
                
            );
        }

        private void Add(string key, RegistryRiskLevel risk, bool enabled, bool isAvailable)
        {
            Categories.Add(new RegistryCategoryView(
                key,
                risk,
                enabled,
                isAvailable,
                ShowCategoryInfo
            ));
        }

        private Task ScanAsync()
        {
            var enabled = new HashSet<string>(Categories.Where(x => x.IsAvailable && x.IsChecked).Select(x => x.CategoryKey));
            return RunOperation(async token =>
            {
                SetStatus(() => Localizer.T("registry.scanning"));
                var found = await Task.Run(() => _scanner.ScanAll(token, enabled).ToList());
                Items.Clear();
                foreach (var issue in found)
                {
                    var item = new RegistryIssueView(issue);
                    item.PropertyChanged += (sender, args) => { if (args.PropertyName == nameof(RegistryIssueView.IsSelected)) { UpdateAllSelectedState(); (FixCommand as RelayCommand)?.RaiseCanExecuteChanged(); } };
                    Items.Add(item);
                }
                UpdateAllSelectedState(); UpdateSummary(); SetStatus(() => Localizer.T("summary.registry", Items.Count));
            });
        }
        private async Task FixAsync()
        {
            var selected = Items.Where(x => x.IsSelected && x.CanFix).ToArray();
            if (selected.Length == 0 || DialogService.ShowConfirm(Loc.RegistryConfirmFix.Value) != DialogResult.Yes) return;
            await RunOperation(async token =>
            {
                SetStatus(() => Localizer.T("registry.fixing"));
                string error = "";
                var restore = await Task.Run(() => SystemRestorePoint.TryCreate("WashinPC Registry Cleanup", out error));
                AppLogger.Info("registry", restore ? "restorepoint.created" : "restorepoint.failed", restore ? "Restore point created." : "Restore point unavailable.");
                if (token.IsCancellationRequested) { SetStatus(() => Localizer.T("op.cancelled")); return; }
                if (!restore && DialogService.ShowConfirm(Localizer.T("registry.restorepoint.failed.confirm", error)) != DialogResult.Yes) return;
                var context = new RegistryFixContext { AllowMutation = true, CreateBackup = true, HasRestorePoint = restore };
                var result = await Task.Run(() =>
                {
                    var outcome = new OperationResult();
                    foreach (var item in selected)
                    {
                        var record = new ItemResult { Path = item.KeyPath }; outcome.Items.Add(record);
                        if (token.IsCancellationRequested) { outcome.Cancelled = true; record.Status = ItemStatus.Cancelled; record.Reason = "op.cancelled"; continue; }
                        try
                        {
                            var fixedResult = item.Model.Fix(context);
                            record.Status = fixedResult == RegistryFixResult.Fixed ? ItemStatus.Applied : fixedResult == RegistryFixResult.Failed ? ItemStatus.Failed : ItemStatus.Skipped;
                            record.Reason = fixedResult == RegistryFixResult.Fixed ? "op.applied" : fixedResult == RegistryFixResult.Failed ? "op.backup_or_write_failed" : "op.changed";
                        }
                        catch { record.Status = ItemStatus.Failed; record.Reason = "op.failed"; }
                    }
                    outcome.Backups.AddRange(context.Backups); return outcome;
                });
                for (var i = 0; i < selected.Length; i++) if (result.Items[i].Status == ItemStatus.Applied) Items.Remove(selected[i]);
                UpdateAllSelectedState(); UpdateSummary(); FileMaintenanceViewModel.ShowResult(result);
            });
        }
        protected override void OnBusyChanged()
        {
            (ScanCommand as RelayCommand)?.RaiseCanExecuteChanged(); (FixCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        private void UpdateAllSelectedState()
        {
            if (Items.Count == 0)
            {
                _allSelectedState = false;
                OnPropertyChanged(nameof(AllSelectedState));
                return;
            }

            var all = Items.Where(i => i.CanFix).Any() && Items.Where(i => i.CanFix).All(i => i.IsSelected);
            var none = Items.All(i => !i.IsSelected);

            bool? state = all ? true : (none ? (bool?)false : null);

            if (_allSelectedState != state)
            {
                _allSelectedState = state;
                OnPropertyChanged(nameof(AllSelectedState));
            }
        }

        private void UpdateSummary()
        {
            SummaryText = Localizer.T("summary.registry", Items.Count);
        }

        private void OnSettingChanged(string settingName)
        {
            if (!string.Equals(settingName, "RegistryExpertMode", StringComparison.Ordinal))
                return;

            if (!CanStart) return;
            BuildCategories();
            Items.Clear();
            UpdateAllSelectedState();
            UpdateSummary();
        }
    }
}


