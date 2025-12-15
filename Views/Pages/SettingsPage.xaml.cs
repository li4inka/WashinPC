using System.Windows.Controls;
using WashinPC.ViewModels.Pages;
using WashinPC.Services;

namespace WashinPC.Views.Pages
{
    public partial class SettingsPage : UserControl
    {
        public SettingsPage()
        {
            InitializeComponent();
            DataContext = new SettingsViewModel();

            AppUiState.Instance.StatusText = string.Empty;
            AppUiState.Instance.BusyText = string.Empty;
            AppUiState.Instance.IsBusy = false;
        }
    }
}
