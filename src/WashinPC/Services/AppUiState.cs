using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WashinPC.Services
{
    public sealed class AppUiState : INotifyPropertyChanged
    {
        public static AppUiState Instance { get; } = new AppUiState();

        private string _statusText;
        private bool _isBusy;
        private string _busyText;

        public string StatusText
        {
            get => _statusText;
            set { _statusText = value; OnPropertyChanged(); }
        }

        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); }
        }

        public string BusyText
        {
            get => _busyText;
            set { _busyText = value; OnPropertyChanged(); }
        }

        private AppUiState()
        {
            StatusText = "";
            BusyText = "";
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string prop = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }
}
