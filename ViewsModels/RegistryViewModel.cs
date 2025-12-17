using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using WashinPC.Engines.Registry;
using WashinPC.Services;
using WashinPC.UI;
using WashinPC.UI.Dialogs;

namespace WashinPC.ViewModels
{
    public class RegistryViewModel : BaseViewModel
    {
        private readonly RegistryScanner _scanner = new RegistryScanner();

        // ===================== VIEW ITEM =====================

        public class Issue
        {
            public bool IsSelected { get; set; } = true;
            public string Category { get; set; }
            public string KeyPath { get; set; }
            public string ValueName { get; set; }
            public string Details { get; set; }

            public RegistryIssue Source { get; set; }
        }

        // ===================== DATA =====================

        public ObservableCollection<Issue> Items { get; }
            = new ObservableCollection<Issue>();

        private string _summaryText = "Найдено записей: 0";
        public string SummaryText
        {
            get => _summaryText;
            private set
            {
                _summaryText = value;
                OnPropertyChanged();
            }
        }

        public bool CreateBackup { get; set; } = true;

        // ===================== COMMANDS =====================

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
        }

        protected override void OnBusyChanged()
        {
            (ScanCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (FixCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        // ===================== LOGIC =====================

        private async Task ScanAsync()
        {
            try
            {
                SetBusyText("Сканирование реестра…");
                SetStatus("Поиск проблемных записей реестра…");
                IsBusy = true;

                Items.Clear();

                var issues = await Task.Run(() => _scanner.ScanAll());

                foreach (var i in issues)
                {
                    Items.Add(new Issue
                    {
                        Category = i.Category,
                        KeyPath = i.KeyPath,
                        ValueName = i.ValueName,
                        Details = i.Details,
                        Source = i
                    });
                }

                SummaryText = $"Найдено записей: {Items.Count}";
                SetStatus($"Найдено проблем реестра: {Items.Count}");
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
                DialogService.ShowInfo("Нет выбранных проблем для исправления.");
                return;
            }

            if (DialogService.ShowConfirm(
                    "Исправить выбранные проблемы реестра?\nРекомендуется создать резервные копии.")
                != DialogResult.Yes)
                return;

            try
            {
                SetBusyText("Исправление реестра…");
                SetStatus("Применение исправлений…");
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
                                var regFile = RegistryBackup.ExportKey(
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
                    SetStatus($"Исправлено: {fixedCount}. Бэкапов: {backupCount}");
                else
                    SetStatus($"Исправлено проблем реестра: {fixedCount}");
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
