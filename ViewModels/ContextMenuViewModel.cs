using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using WashinPC.Core.Logging;
using WashinPC.Engines.ContextMenu;
using WashinPC.Localization;
using WashinPC.Services;

namespace WashinPC.ViewModels
{
    public sealed class ContextMenuItemViewModel : INotifyPropertyChanged
    {
        private bool _disable;
        private bool _delete;

        public ContextMenuEntry Entry { get; private set; }

        public string Name { get { return Entry.Name; } }
        public string Target
        {
            get
            {
                switch (Entry.Target)
                {
                    case ContextMenuTarget.AnyFile: return Localizer.T("contextmenu.target.any_file");
                    case ContextMenuTarget.Shortcut: return Localizer.T("contextmenu.target.shortcut");
                    case ContextMenuTarget.Folder: return Localizer.T("contextmenu.target.folder");
                    case ContextMenuTarget.DirectoryBackground: return Localizer.T("contextmenu.target.folder_background");
                    case ContextMenuTarget.DesktopBackground: return Localizer.T("contextmenu.target.desktop");
                    case ContextMenuTarget.Drive: return Localizer.T("contextmenu.target.drive");
                    default: return Entry.Target.ToString();
                }
            }
        }

        public string Kind
        {
            get
            {
                return Entry.Kind == ContextMenuKind.ShellExHandler
                    ? Localizer.T("contextmenu.kind.shellex")
                    : Localizer.T("contextmenu.kind.shellverb");
            }
        }

        public string Scope
        {
            get
            {
                return Entry.Scope == ContextMenuScope.LocalMachine
                    ? Localizer.T("contextmenu.scope.hklm")
                    : Localizer.T("contextmenu.scope.hkcu");
            }
        }

        public string RegistryPath { get { return Entry.RegistryPath; } }
        public string Source { get { return Entry.Source; } }

        public bool IsEnabled { get { return Entry.IsEnabled; } }

        public bool IsToggleSupported
        {
            get
            {
                return Entry.Kind == ContextMenuKind.ShellExHandler ||
                       Entry.Kind == ContextMenuKind.ShellVerb;
            }
        }

        public bool IsDeleteSupported
        {
            get { return !string.IsNullOrWhiteSpace(Entry.RelativeKeyPath); }
        }

        public string ToggleDisabledReason
        {
            get
            {
                return IsToggleSupported
                    ? string.Empty
                    : Localizer.T("contextmenu.toggle.unsupported");
            }
        }

        public string DeleteDisabledReason
        {
            get
            {
                return IsDeleteSupported
                    ? string.Empty
                    : Localizer.T("contextmenu.delete.unsupported");
            }
        }

        public bool Disable
        {
            get { return _disable; }
            set
            {
                if (!IsToggleSupported) return;
                if (_disable == value) return;
                _disable = value;

                if (_disable && _delete)
                {
                    _delete = false;
                    OnPropertyChanged("Delete");
                }

                OnPropertyChanged("Disable");
            }
        }

        public bool Delete
        {
            get { return _delete; }
            set
            {
                if (!IsDeleteSupported) return;
                if (_delete == value) return;
                _delete = value;

                if (_delete && _disable)
                {
                    _disable = false;
                    OnPropertyChanged("Disable");
                }

                OnPropertyChanged("Delete");
            }
        }

        public ContextMenuItemViewModel(ContextMenuEntry e)
        {
            Entry = e;
            _disable = !e.IsEnabled;
            _delete = false;
        }

        public void RefreshLocalization()
        {
            OnPropertyChanged("Target");
            OnPropertyChanged("Kind");
            OnPropertyChanged("Scope");
            OnPropertyChanged("ToggleDisabledReason");
            OnPropertyChanged("DeleteDisabledReason");
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

            Scan();
        }

        private void OnLanguageChanged()
        {
            for (int i = 0; i < Items.Count; i++)
                Items[i].RefreshLocalization();

            UpdateSummary();
        }

        private void Scan()
        {
            var operationId = "ctx-scan-" + Guid.NewGuid().ToString("N");
            AppLogger.Info("contextmenu", "scan.start", "Starting context menu scan.", operationId);

            try
            {
                IsBusy = true;
                SetStatus(Loc.StatusSearching.Value);

                Items.Clear();

                var entries = ContextMenuScanner.Scan();
                for (int i = 0; i < entries.Count; i++)
                    Items.Add(new ContextMenuItemViewModel(entries[i]));

                UpdateSummary();
                AppLogger.Info("contextmenu", "scan.done", "Context menu scan completed. Items: " + Items.Count, operationId);
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
            var operationId = "ctx-apply-" + Guid.NewGuid().ToString("N");
            AppLogger.Info("contextmenu", "apply.start", "Starting context menu apply.", operationId);

            try
            {
                IsBusy = true;
                SetStatus(Loc.StatusCleaning.Value);

                var toDelete = Items
                    .Where(x => x.Delete && x.IsDeleteSupported)
                    .Select(x => x.Entry)
                    .ToList();

                if (toDelete.Count > 0)
                {
                    var confirm = DialogService.ShowConfirm(
                        Localizer.T("contextmenu.confirm.delete", toDelete.Count));

                    if (confirm != DialogResult.Yes)
                    {
                        SetStatus(Localizer.T("contextmenu.delete.cancelled"));
                        AppLogger.Warn("contextmenu", "delete.cancelled", "Context menu delete cancelled by user.", operationId);
                        return;
                    }
                }

                var toDisable = Items
                    .Where(x => !x.Delete && x.IsToggleSupported && x.Disable && x.Entry.IsEnabled)
                    .Select(x => x.Entry)
                    .ToList();

                var toEnable = Items
                    .Where(x => !x.Delete && x.IsToggleSupported && !x.Disable && !x.Entry.IsEnabled)
                    .Select(x => x.Entry)
                    .ToList();

                int changed = 0;
                changed += ContextMenuApplier.ApplyDisable(toDisable);
                changed += ContextMenuApplier.ApplyEnable(toEnable);
                changed += ContextMenuApplier.ApplyDelete(toDelete);

                Items.Clear();
                var entries = ContextMenuScanner.Scan();
                for (int i = 0; i < entries.Count; i++)
                    Items.Add(new ContextMenuItemViewModel(entries[i]));

                UpdateSummary();
                SetStatus(Localizer.T("contextmenu.applied", changed.ToString()));
                AppLogger.Info("contextmenu", "apply.done", "Context menu apply completed. Changed: " + changed + ", Deleted: " + toDelete.Count, operationId);
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
