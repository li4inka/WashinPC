using System;
using System.Threading;
using System.ComponentModel;
using WashinPC.Core.Mvvm;
using WashinPC.Localization;
using System.Runtime.CompilerServices;

namespace WashinPC.Services
{
    public sealed class AppUiState : INotifyPropertyChanged
    {
        public static AppUiState Instance { get; } = new AppUiState();

        private CancellationTokenSource _cancellation;
        public RelayCommand CancelCommand { get; }
        public string CancelLabel => Localizer.T("op.cancel");
        public bool IsIdle => !IsBusy;
        internal CancellationToken Token => _cancellation?.Token ?? CancellationToken.None;
        internal bool TryBegin()
        {
            if (IsBusy) return false;
            _cancellation = new CancellationTokenSource(); IsBusy = true; return true;
        }
        internal void End()
        {
            IsBusy = false; _cancellation?.Dispose(); _cancellation = null;
        }
        public void Cancel() { _cancellation?.Cancel(); }
        private string _details = "";
        public string Details { get => _details; set { _details = value; OnPropertyChanged(); } }
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
            set { _isBusy = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsIdle)); CancelCommand?.RaiseCanExecuteChanged(); System.Windows.Input.CommandManager.InvalidateRequerySuggested(); }
        }

        public string BusyText
        {
            get => _busyText;
            set { _busyText = value; OnPropertyChanged(); }
        }

        private AppUiState()
        {
            CancelCommand = new RelayCommand(Cancel, () => IsBusy);
            Localizer.LanguageChanged += () => OnPropertyChanged(nameof(CancelLabel));
            StatusText = "";
            BusyText = "";
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string prop = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }
}
