using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using WashinPC.Engines.Registry;
using WashinPC.Services;
using WashinPC.UI;
using WashinPC.UI.Dialogs;

namespace WashinPC.ViewModels.Pages
{
    public class RegistryViewModel : BaseViewModel
    {
        private readonly RegistryScanner _scanner = new RegistryScanner();

        // ===================== DATA =====================

        public ObservableCollection<RegistryIssueView> Items { get; } =
            new ObservableCollection<RegistryIssueView>();

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

        // ===================== COMMANDS =====================

        public ICommand ScanCommand { get; }
        public ICommand FixCommand { get; }

        // ===================== CTOR =====================

        public RegistryViewModel()
        {
            ScanCommand = new RelayCommand(
                async () => await ScanAsync(),
                () => !IsBusy);

            FixCommand = new RelayCommand(
                () => Fix(),
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
                    Items.Add(new RegistryIssueView
                    {
                        IsSelected = true,
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
                    "Исправить выбранные проблемы реестра?\nРекомендуется создать резервную копию.")
                != DialogResult.Yes)
                return;

            try
            {
                SetBusyText("Исправление реестра…");
                SetStatus("Применение исправлений…");
                IsBusy = true;

                // ===== BACKUP =====
                if (CreateBackup)
                {
                    try
                    {
                        RegistryBackup.CreateHKLMBackup();
                        SetStatus("Резервная копия реестра создана");
                    }
                    catch
                    {
                        DialogService.ShowWarning(
                            "Не удалось создать резервную копию реестра. Исправление будет продолжено.");
                    }
                }

                int fixedCount = 0;

                foreach (var item in selected)
                {
                    try
                    {
                        var result = item.Source.Fix?.Invoke(false);
                        if (result == RegistryFixResult.Fixed)
                            fixedCount++;
                    }
                    catch
                    {
                        // как CCleaner — просто идём дальше
                    }
                }

                SetStatus($"Исправлено проблем реестра: {fixedCount}");
            }
            finally
            {
                IsBusy = false;
            }

            _ = ScanAsync();
        }
    }
}
