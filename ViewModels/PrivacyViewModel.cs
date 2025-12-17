using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using WashinPC.Core.Mvvm;
using WashinPC.Core.Utils;
using WashinPC.Engines.Privacy;
using WashinPC.Localization;
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
                OnPropertyChanged(nameof(SummaryText));
            };
        }

        protected override void OnBusyChanged()
        {
            ScanCommand.RaiseCanExecuteChanged();
            CleanCommand.RaiseCanExecuteChanged();
        }

        private async Task ScanAsync()
        {
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
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task CleanAsync()
        {
            try
            {
                SetBusyText(Loc.StatusCleaning.Value);
                SetStatus(Loc.StatusCleaning.Value);
                IsBusy = true;

                var files = Items
                    .Where(i => i.IsChecked)
                    .SelectMany(i =>
                    {
                        var key = i.Title?.Key;
                        var rule = _engine
                            .GetType()
                            .GetField("_rules", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                            ?.GetValue(_engine) as System.Collections.IEnumerable;

                        return Enumerable.Empty<string>();
                    })
                    .ToList();

                // Проще и безопаснее: пересканируем и удалим всё выбранное
                int tmpFiles;
                long tmpBytes;
                var groups = _engine.Scan(out tmpFiles, out tmpBytes);

                foreach (var g in groups)
                {
                    if (Items.Any(i => i.IsChecked && i.Title.Key == g.LocalizationKey))
                        _engine.CleanSelected(g.Files);
                }

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
    }
}
