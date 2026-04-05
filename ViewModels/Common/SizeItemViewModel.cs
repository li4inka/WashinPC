using WashinPC.Core.Mvvm;
using WashinPC.Localization;

namespace WashinPC.ViewModels.Common
{
    public sealed class SizeItemViewModel : BaseViewModel
    {
        private bool _isChecked;
        private long _totalBytes;

        public LocalizedString Title { get; set; }

        public string Name => Title?.Value ?? string.Empty;

        public string SizeText =>
            Core.Utils.FileSizeFormatter.Format(_totalBytes);

        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                if (_isChecked == value) return;
                _isChecked = value;
                OnPropertyChanged();
            }
        }

        public long TotalBytes
        {
            get => _totalBytes;
            set
            {
                if (_totalBytes == value) return;
                _totalBytes = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SizeText));
            }
        }

        public void RefreshLocalization()
        {
            if (Title != null)
                Title.Refresh();

            OnPropertyChanged(nameof(Name));
        }
    }
}
