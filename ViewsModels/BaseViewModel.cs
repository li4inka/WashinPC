using System.ComponentModel;
using System.Runtime.CompilerServices;
using WashinPC.Services;

namespace WashinPC.ViewModels
{
    public abstract class BaseViewModel : INotifyPropertyChanged
    {
        private bool _isBusy;

        public bool IsBusy
        {
            get => _isBusy;
            protected set
            {
                if (_isBusy == value) return;
                _isBusy = value;
                OnPropertyChanged();
                AppUiState.Instance.IsBusy = value;
                OnBusyChanged();
            }
        }

        protected void SetBusyText(string text)
        {
            if (!string.IsNullOrWhiteSpace(text))
                AppUiState.Instance.BusyText = text;
        }

        protected void SetStatus(string text)
        {
            AppUiState.Instance.StatusText = text ?? "";
        }

        protected virtual void OnBusyChanged() { }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string p = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
    }
}
