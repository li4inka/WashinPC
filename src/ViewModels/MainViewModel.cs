using System;
using System.Diagnostics;
using WashinPC.Core.Mvvm;
using WashinPC.Localization;
using WashinPC.Engines.Registry;
using WashinPC.Core;
using WashinPC.Services;

namespace WashinPC.ViewModels
{
    public sealed class MainViewModel : BaseViewModel
    {
        public RelayCommand BackupsCommand { get; }
        public RelayCommand DetailsCommand { get; }
        public string BackupsLabel => Localizer.T("op.backups");
        public string DetailsLabel => Localizer.T("op.details");
        public AppInfo AppInfo { get; }
        public AppUiState Ui { get; }

        public MainViewModel()
        {
            BackupsCommand = new RelayCommand(() => {
                try { Process.Start(new ProcessStartInfo("explorer.exe", "\"" + RegistryBackup.GetBackupRoot() + "\"") { UseShellExecute = true }); }
                catch { DialogService.ShowError(Localizer.T("op.failed")); }
            }, () => !Ui.IsBusy);
            DetailsCommand = new RelayCommand(() => DialogService.ShowInfo(Ui.Details), () => !Ui.IsBusy && !string.IsNullOrEmpty(Ui.Details));
            Localizer.LanguageChanged += () => { OnPropertyChanged(nameof(BackupsLabel)); OnPropertyChanged(nameof(DetailsLabel)); };
            AppInfo = new AppInfo();
            Ui = AppUiState.Instance;
        }
    }
}
