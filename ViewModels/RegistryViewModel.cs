using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using WashinPC.Core;
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
            _isSelected = true;
        }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected == value) return;
                _isSelected = value;
                OnPropertyChanged();
            }
        }

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

        private bool _createBackup = true;
        public bool CreateBackup
        {
            get => _createBackup;
            set
            {
                if (_createBackup == value) return;
                _createBackup = value;
                OnPropertyChanged();
            }
        }

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
            ScanCommand = new RelayCommand(async () => await ScanAsync(), () => !IsBusy);
            FixCommand = new RelayCommand(async () => await FixAsync(), () => !IsBusy);

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
            Add("registry.cat.uninstall", RegistryRiskLevel.Safe, true, true);
            Add("registry.cat.shared_dlls", RegistryRiskLevel.Safe, true, true);
            Add("registry.cat.unused_file_ext", RegistryRiskLevel.Safe, true, true);
            Add("registry.cat.help_files", RegistryRiskLevel.Safe, true, true);
            Add("registry.cat.obsolete_software", RegistryRiskLevel.Safe, true, true);

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

        private async Task ScanAsync()
        {
            try
            {
                IsBusy = true;
                SetStatus(Localizer.T("registry.scanning"));

                Items.Clear();

                // 1) сканируем всё, что умеет сканер
                var all = await Task.Run(() => _scanner.ScanAll().ToList());

                // 2) фильтруем по выбранным категориям (чтобы выбор реально влиял на результат)
                var enabledKeys = new HashSet<string>(
                    Categories
                        .Where(c => c.IsAvailable && c.IsChecked)
                        .Select(c => c.CategoryKey)
                );

                var filtered = all.Where(i => enabledKeys.Contains(i.CategoryKey)).ToList();

                foreach (var issue in filtered)
                {
                    issue.RefreshText(); // подтянуть тексты под текущий язык
                    Items.Add(new RegistryIssueView(issue));
                }

                UpdateAllSelectedState();
                UpdateSummary();
            }
            finally
            {
                IsBusy = false;
                SetStatus(string.Empty);
            }
        }

        private async Task FixAsync()
        {
            try
            {
                IsBusy = true;

                var selected = Items.Where(i => i.IsSelected).ToList();
                if (selected.Count == 0)
                {
                    SetStatus(Localizer.T("registry.nothing_selected"));
                    return;
                }

                SetStatus(Localizer.T("registry.fixing"));

                string restoreError = string.Empty;
                bool hasRestorePoint = await Task.Run(() =>
                {
                    string err;
                    var ok = SystemRestorePoint.TryCreate(
                        "WashinPC Registry Cleanup",
                        out err);
                    restoreError = err;
                    return ok;
                });

                if (!hasRestorePoint)
                {
                    var confirm = DialogService.ShowConfirm(
                        Localizer.T("registry.restorepoint.failed.confirm", restoreError),
                        Localizer.T("registry.info.title"));

                    if (confirm != DialogResult.Yes)
                    {
                        SetStatus(Localizer.T("registry.restorepoint.cancelled"));
                        return;
                    }
                }

                var context = new RegistryFixContext
                {
                    AllowMutation = true,
                    CreateBackup = CreateBackup,
                    HasRestorePoint = hasRestorePoint
                };

                var session = new RegistryFixSessionResult();

                await Task.Run(() =>
                {
                    foreach (var item in selected)
                    {
                        try
                        {
                            var res = item.Model.Fix?.Invoke(context) ?? RegistryFixResult.Failed;
                            if (res == RegistryFixResult.Fixed) session.FixedCount++;
                            else if (res == RegistryFixResult.Skipped || res == RegistryFixResult.None) session.SkippedCount++;
                            else session.FailedCount++;
                        }
                        catch
                        {
                            session.FailedCount++;
                        }
                    }
                });

                session.BackupCount = context.Backups.Count;

                var statusText = Localizer.T(
                    "registry.fixed.summary",
                    session.FixedCount,
                    session.SkippedCount,
                    session.FailedCount);

                SetStatus(statusText);

                // после фикса перезапускаем скан (чтобы обновить список)
                await ScanAsync();
                SetStatus(statusText);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void UpdateAllSelectedState()
        {
            if (Items.Count == 0)
            {
                _allSelectedState = false;
                OnPropertyChanged(nameof(AllSelectedState));
                return;
            }

            var all = Items.All(i => i.IsSelected);
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

            BuildCategories();
            Items.Clear();
            UpdateAllSelectedState();
            UpdateSummary();
        }
    }
}
