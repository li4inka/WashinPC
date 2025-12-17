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

        public class Issue
        {
            public bool IsSelected { get; set; }
            public string Category { get; set; }
            public string KeyPath { get; set; }
            public string ValueName { get; set; }
            public string Details { get; set; }
            public RegistryIssue Source { get; set; }

            public Issue()
            {
                IsSelected = true;
            }
        }

        public ObservableCollection<Issue> Items { get; private set; }

        private string _summaryText = "Найдено записей: 0";
        public string SummaryText
        {
            get { return _summaryText; }
            private set
            {
                _summaryText = value;
                OnPropertyChanged();
            }
        }

        public bool CreateBackup { get; set; }

        public ICommand ScanCommand { get; private set; }
        public ICommand FixCommand { get; private set; }

        public RegistryViewModel()
        {
            Items = new ObservableCollection<Issue>();
            CreateBackup = true;

            ScanCommand = new RelayCommand(
                async () => await ScanAsync(),
                () => !IsBusy);

            FixCommand = new RelayCommand(
                Fix,
                () => !IsBusy && Items.Any(i => i.IsSelected));
        }

        protected override void OnBusyChanged()
        {
            (ScanCommand as RelayCommand).RaiseCanExecuteChanged();
            (FixCommand as RelayCommand).RaiseCanExecuteChanged();
        }

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

                SummaryText = string.Format("Найдено записей: {0}", Items.Count);
                SetStatus(string.Format("Найдено проблем реестра: {0}", Items.Count));
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
                            "Не удалось создать резервную копию реестра.");
                    }
                }

                int fixedCount = 0;

                foreach (var item in selected)
                {
                    try
                    {
                        if (item.Source.Fix != null &&
                            item.Source.Fix(false) == RegistryFixResult.Fixed)
                            fixedCount++;
                    }
                    catch { }
                }

                SetStatus(string.Format("Исправлено проблем реестра: {0}", fixedCount));
            }
            finally
            {
                IsBusy = false;
            }

            ScanAsync();
        }
    }
}
