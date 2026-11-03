using System;
using System.Windows.Input;

namespace WashinPC.Core.Mvvm
{
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _can;
        private readonly System.Windows.Threading.Dispatcher _dispatcher = System.Windows.Threading.Dispatcher.CurrentDispatcher;

        public RelayCommand(Action execute, Func<bool> can = null)
        {
            _execute = execute;
            _can = can;
        }

        public bool CanExecute(object parameter) => _can?.Invoke() ?? true;
        public void Execute(object parameter) { if (CanExecute(parameter)) _execute(); }

        private event EventHandler Changed;
        public event EventHandler CanExecuteChanged
        {
            add { Changed += value; CommandManager.RequerySuggested += value; }
            remove { Changed -= value; CommandManager.RequerySuggested -= value; }
        }

        public void RaiseCanExecuteChanged()
        {
            if (_dispatcher.HasShutdownStarted) return;
            if (!_dispatcher.CheckAccess())
            {
                _dispatcher.BeginInvoke(new Action(RaiseCanExecuteChanged));
                return;
            }
            Changed?.Invoke(this, EventArgs.Empty);
        }

    }
}
