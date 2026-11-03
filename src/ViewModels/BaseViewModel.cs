using System;
using System.Threading;
using System.Threading.Tasks;
using WashinPC.Core.Logging;
using WashinPC.Localization;
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

                OnBusyChanged();
            }
        }

        protected bool CanStart => !AppUiState.Instance.IsBusy;
        protected async Task RunOperation(Func<CancellationToken, Task> operation, [CallerMemberName] string name = null)
        {
            var ui = AppUiState.Instance;
            if (!ui.TryBegin()) return;
            var id = Guid.NewGuid().ToString("N");
            var subsystem = GetType().Name.Replace("ViewModel", "").ToLowerInvariant();
            AppLogger.Info(subsystem, name + ".start", "Operation started.", id);
            IsBusy = true;
            try { await operation(ui.Token); }
            catch (OperationCanceledException) { SetStatus(Localizer.T("op.cancelled")); AppLogger.Warn(subsystem, name + ".cancelled", "Operation cancelled.", id); }
            catch (Exception ex)
            {
                AppLogger.Error(subsystem, name + ".failed", "Operation failed. exceptionType=" + ex.GetType().Name, operationId: id);
                SetStatus(Localizer.T("op.failed"));
                DialogService.ShowError(Localizer.T("op.failed"));
            }
            finally
            {
                if (ui.Token.IsCancellationRequested) AppLogger.Warn(subsystem, name + ".cancelled", "Cancellation requested; completed changes retained.", id);
                AppLogger.Info(subsystem, name + ".done", "Operation finished.", id);
                IsBusy = false; ui.End();
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
