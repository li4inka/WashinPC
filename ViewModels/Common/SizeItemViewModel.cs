using WashinPC.Core;

namespace WashinPC.ViewModels.Common
{
    /// <summary>
    /// Базовая VM для элементов со размером (Очистка / Приватность)
    /// </summary>
    public abstract class SizeItemViewModel : BaseViewModel
    {
        private string _name;
        private long _totalBytes;
        private bool _isChecked = true;

        public string Name
        {
            get => _name;
            set
            {
                if (_name == value) return;
                _name = value;
                OnPropertyChanged();
            }
        }

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
