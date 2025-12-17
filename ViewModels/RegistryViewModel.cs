using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using WashinPC.Core.Mvvm;
using WashinPC.Engines.Registry;
using WashinPC.Localization;
using WashinPC.Services;

namespace WashinPC.ViewModels
{
    public class RegistryViewModel : BaseViewModel
    {
        private readonly RegistryScanner _scanner = new RegistryScanner();

        public class Issue
        {
            public bool IsSelected { get; set; } = true;

            public string Category { get; private set; }
            public string KeyPath { get; set; }
            public string ValueName { get; set; }
            public string Details { get; private set; }

            public RegistryIssue Source { get; set; }

            public void RefreshText()
            {
                if (Source == null)
                    return;

                Source.RefreshText();
                Category = Source.Category;
                Details = Source.Details;
            }
        }

        public ObservableCollection<Issue> Items { get; }
            = new ObservableCollection<Issue>();

        private int _issuesCount;

        public string SummaryText =>
            string.Format(Loc.SummaryRegistry.Value, _issuesCount);

        public bool CreateBackup { get; set; } = true;

        public ICommand ScanCommand { get; }
        public ICommand FixCommand { get; }

        public RegistryViewModel()
        {
            ScanCommand = new RelayCommand(
                async () => await ScanAsync(),
                () => !IsBusy);

            FixCommand = new RelayCommand(
                Fix,
                () => !IsBusy && Items.Any(i => i.IsSelected));

            // мгновенное обновление Category/Details/Summary при смене языка
            Localizer.LanguageChanged += OnLanguageChanged;
        }

        private void OnLanguageChanged()
        {
            foreach (var i in Items)
                i.RefreshText();

            // заставим DataGrid обновить строки (Items не INotifyPropertyChanged)
            var snap = Items.ToList();
            Items.Clear();
            foreach (var x in snap)
                Items.Add(x);

            OnPropertyChanged(nameof(SummaryText));
        }

        protected override void OnBusyChanged()
        {
            (ScanCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (FixCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        private async Task ScanAsync()
        {
            try
            {
                SetBusyText(Loc.RegistryScanning.Value);
                SetStatus(Loc.RegistrySearching.Value);
                IsBusy = true;

                Items.Clear();

                var issues = await Task.Run(() => _scanner.ScanAll());

                foreach (var i in issues)
                {
                    var vm = new Issue
                    {
                        KeyPath = i.KeyPath,
                        ValueName = i.ValueName,
                        Source = i
                    };
                    vm.RefreshText();

                    Items.Add(vm);
                }

                _issuesCount = Items.Count;
                OnPropertyChanged(nameof(SummaryText));

                SetStatus(string.Format(Loc.SummaryRegistry.Value, _issuesCount));
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void Fix()
        {
            var selected = Items
                .Where(i => i.IsSelected && i.Source != null)
                .ToList();

            if (selected.Count == 0)
            {
                DialogService.ShowInfo(Loc.RegistryNoSelection.Value);
                return;
            }

            if (DialogService.ShowConfirm(Loc.RegistryConfirmFix.Value) != DialogResult.Yes)
                return;

            try
            {
                SetBusyText(Loc.StatusCleaning.Value);
                SetStatus(Loc.StatusCleaning.Value);
                IsBusy = true;

                int fixedCount = 0;
                int backupCount = 0;

                foreach (var item in selected)
                {
                    try
                    {
                        if (CreateBackup)
                        {
                            var subKeyPath = ExtractSubKeyPath(item.KeyPath);
                            if (!string.IsNullOrWhiteSpace(subKeyPath))
                            {
                                var regFile = Engines.Registry.RegistryBackup.ExportKey(
                                    item.Source.Hive,
                                    item.Source.View,
                                    subKeyPath);

                                if (!string.IsNullOrWhiteSpace(regFile))
                                    backupCount++;
                            }
                        }

                        if (item.Source.Fix != null &&
                            item.Source.Fix(false) == RegistryFixResult.Fixed)
                        {
                            fixedCount++;
                        }
                    }
                    catch
                    {
                        // как CCleaner — идём дальше
                    }
                }

                if (CreateBackup && backupCount > 0)
                    SetStatus(string.Format(
                        Loc.RegistryFixedWithBackup.Value,
                        fixedCount,
                        backupCount));
                else
                    SetStatus(string.Format(
                        Loc.RegistryFixed.Value,
                        fixedCount));
            }
            finally
            {
                IsBusy = false;
            }

            _ = ScanAsync();
        }

        private static string ExtractSubKeyPath(string keyPathUi)
        {
            if (string.IsNullOrWhiteSpace(keyPathUi))
                return null;

            if (keyPathUi.StartsWith("HKLM\\"))
                return keyPathUi.Substring("HKLM\\".Length);

            if (keyPathUi.StartsWith("HKCU\\"))
                return keyPathUi.Substring("HKCU\\".Length);

            return null;
        }
    }
}
