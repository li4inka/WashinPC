using System.Windows;
using WashinPC.Services;
using WashinPC.ViewModels;

namespace WashinPC.Views.Dialogs
{
    public partial class BackupsWindow : Window
    {
        public BackupsWindow()
        {
            InitializeComponent();
            var vm = new BackupsViewModel(); DataContext = vm;
            Loaded += async (sender, args) => await vm.RefreshAsync();
            Closing += (sender, args) => { if (AppUiState.Instance.IsBusy) { args.Cancel = true; AppUiState.Instance.Cancel(); } };
            Closed += (sender, args) => vm.Dispose();
        }
    }
}
