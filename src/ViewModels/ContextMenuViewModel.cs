using System.Threading.Tasks;
using WashinPC.Core.Mvvm;
using WashinPC.Engines.Operations;
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

            ScanCommand = new RelayCommand(async () => await ScanAsync(), () => CanStart);
            ApplyCommand = new RelayCommand(async () => await ApplyAsync(), () => CanStart && Items.Count > 0);

            Localizer.LanguageChanged += OnLanguageChanged;


        }

        private void OnLanguageChanged()
        {
            for (int i = 0; i < Items.Count; i++)
                Items[i].RefreshLocalization();

            UpdateSummary();
        }

        private Task ScanAsync() => RunOperation(async token =>
        {
            SetStatus(() => Loc.StatusSearching.Value);
            var entries = await Task.Run(() => ContextMenuScanner.Scan(token));
            Items.Clear();
            foreach (var entry in entries) Items.Add(new ContextMenuItemViewModel(entry));
            UpdateSummary();
            SetStatus(() => token.IsCancellationRequested ? Localizer.T("op.cancelled") : Localizer.T("contextmenu.summary", Items.Count));
        });
        private async Task ApplyAsync()
        {
            var deletes = Items.Where(x => x.Delete && x.IsDeleteSupported).Select(x => x.Entry).ToList();
            var disables = Items.Where(x => !x.Delete && x.IsToggleSupported && x.Disable && x.Entry.IsEnabled).Select(x => x.Entry).ToList();
            var enables = Items.Where(x => !x.Delete && x.IsToggleSupported && !x.Disable && !x.Entry.IsEnabled).Select(x => x.Entry).ToList();
            if (deletes.Count > 0 && DialogService.ShowConfirm(Localizer.T("contextmenu.confirm.delete", deletes.Count)) != DialogResult.Yes) return;
            await RunOperation(async token =>
            {
                var result = await Task.Run(() =>
                {
                    var total = new OperationResult();
                    foreach (var part in new[] { ContextMenuApplier.Apply(disables, MenuAction.Disable, token), ContextMenuApplier.Apply(enables, MenuAction.Enable, token), ContextMenuApplier.Apply(deletes, MenuAction.Delete, token) })
                    { total.Items.AddRange(part.Items); total.Backups.AddRange(part.Backups); total.Cancelled |= part.Cancelled; }
                    return total;
                });
                var applied = new System.Collections.Generic.HashSet<string>(result.Items.Where(x => x.Status == ItemStatus.Applied).Select(x => x.Path));
                foreach (var item in Items.Where(x => applied.Contains(x.Entry.RelativeKeyPath)).ToArray()) Items.Remove(item);
                UpdateSummary(); FileMaintenanceViewModel.ShowResult(result);
            });
        }
        protected override void OnBusyChanged() { RaiseCommands(); }

        private void UpdateSummary()
        {
            Summary = string.Format(Localizer.T("contextmenu.summary"), Items.Count);
        }

        private void RaiseCommands()
        {
            (ScanCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (ApplyCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }
    }
}
