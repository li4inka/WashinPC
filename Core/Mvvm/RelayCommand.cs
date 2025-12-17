using System;
using System.Windows.Input;

namespace WashinPC.Core.Mvvm
{
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _can;

        public RelayCommand(Action execute, Func<bool> can = null)
        {
            _execute = execute;
            _can = can;
        }

        public bool CanExecute(object parameter) => _can?.Invoke() ?? true;
        public void Execute(object parameter) => _execute();

        public event EventHandler CanExecuteChanged;

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

    }
}
