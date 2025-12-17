using WashinPC.Core;
using WashinPC.ViewModels;

namespace WashinPC.ViewModels.Common
{
    /// <summary>
    /// Базовая VM для элементов со размером (Очистка / Приватность)
    /// </summary>
    public abstract class SizeItemViewModel : BaseViewModel
    {
        private long _totalBytes;
        private bool _isChecked = true;

        public string Name { get; set; }

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

        public string SizeText =>
            FileSizeFormatter.Format(TotalBytes);
    }
}
