using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using WashinPC.Engines.ContextMenu;
using WashinPC.Localization;

namespace WashinPC.ViewModels
{
    public sealed class ContextMenuItemViewModel : INotifyPropertyChanged
    {
        private bool _disable;

        public ContextMenuEntry Entry { get; private set; }

        public string Name { get { return Entry.Name; } }
        public string Target { get { return Entry.Target.ToString(); } }
        public string Kind { get { return Entry.Kind.ToString(); } }
        public string Scope { get { return Entry.Scope.ToString(); } }
        public string RegistryPath { get { return Entry.RegistryPath; } }
        public string Source { get { return Entry.Source; } }

        public bool IsEnabled { get { return Entry.IsEnabled; } }

        // tick = disable
        public bool Disable
        {
            get { return _disable; }
            set
            {
                if (_disable == value) return;
                _disable = value;
                OnPropertyChanged("Disable");
            }
        }

        public ContextMenuItemViewModel(ContextMenuEntry e)
        {
            Entry = e;
            Disable = !e.IsEnabled;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string p)
        {
            var h = PropertyChanged;
            if (h != null) h(this, new PropertyChangedEventArgs(p));
        }
    }

    public sealed class ContextMenuViewModel : BaseViewModel
    {
        public ObservableCollection<ContextMenuItemViewModel> Items { get; private set; }

        private string _summary;
        public string Summary
        {
            get { return _summary; }
            private set
            {
                _summary = value ?? "";
                OnPropertyChanged("Summary");
            }
        }

        public ICommand ScanCommand { get; private set; }
        public ICommand ApplyCommand { get; private set; }

        public ContextMenuViewModel()
        {
            Items = new ObservableCollection<ContextMenuItemViewModel>();

            ScanCommand = new DelegateCommand(Scan, () => !IsBusy);
            ApplyCommand = new DelegateCommand(Apply, () => !IsBusy && Items.Count > 0);

            Localizer.LanguageChanged += OnLanguageChanged;

            Scan(); // сразу заполнить как в остальных инструментах
        }

        private void OnLanguageChanged()
        {
            UpdateSummary();
        }

        private void Scan()
        {
            try
            {
                IsBusy = true;
                SetStatus(Loc.StatusSearching.Value);

                Items.Clear();

                var entries = ContextMenuScanner.Scan();
                for (int i = 0; i < entries.Count; i++)
                    Items.Add(new ContextMenuItemViewModel(entries[i]));

                UpdateSummary();
                SetStatus("");
            }
            finally
            {
                IsBusy = false;
                RaiseCommands();
            }
        }

        private void Apply()
        {
            try
            {
                IsBusy = true;
                SetStatus(Loc.StatusCleaning.Value);

                // disable requested: checkbox ON and currently enabled
                var toDisable = Items
                    .Where(x => x.Disable && x.Entry.IsEnabled)
                    .Select(x => x.Entry)
                    .ToList();

                // enable requested: checkbox OFF and currently disabled
                var toEnable = Items
                    .Where(x => !x.Disable && !x.Entry.IsEnabled)
                    .Select(x => x.Entry)
                    .ToList();

                int changed = 0;
                changed += ContextMenuApplier.ApplyDisable(toDisable);
                changed += ContextMenuApplier.ApplyEnable(toEnable);

                // refresh
                Items.Clear();
                var entries = ContextMenuScanner.Scan();
                for (int i = 0; i < entries.Count; i++)
                    Items.Add(new ContextMenuItemViewModel(entries[i]));

                UpdateSummary();

                // raw text because in your Localizer dictionary this key may not exist yet
                // (we will add below)
                SetStatus(Localizer.T("contextmenu.applied", changed.ToString()));
                SetStatus("");
            }
            finally
            {
                IsBusy = false;
                RaiseCommands();
            }
        }

        private void UpdateSummary()
        {
            Summary = string.Format(Localizer.T("contextmenu.summary"), Items.Count);
        }

        private void RaiseCommands()
        {
            var d1 = ScanCommand as DelegateCommand;
            if (d1 != null) d1.RaiseCanExecuteChanged();

            var d2 = ApplyCommand as DelegateCommand;
            if (d2 != null) d2.RaiseCanExecuteChanged();
        }

        private sealed class DelegateCommand : ICommand
        {
            private readonly Action _execute;
            private readonly Func<bool> _canExecute;

            public DelegateCommand(Action execute, Func<bool> canExecute)
            {
                _execute = execute;
                _canExecute = canExecute;
            }

            public bool CanExecute(object parameter)
            {
                return _canExecute == null ? true : _canExecute();
            }

            public void Execute(object parameter)
            {
                if (_execute != null) _execute();
            }

            public event EventHandler CanExecuteChanged;
            public void RaiseCanExecuteChanged()
            {
                var h = CanExecuteChanged;
                if (h != null) h(this, EventArgs.Empty);
            }
        }
    }
}
