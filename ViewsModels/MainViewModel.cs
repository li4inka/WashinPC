using WashinPC.Core;
using WashinPC.Services;

namespace WashinPC.ViewModels
{
    public sealed class MainViewModel
    {
        public AppInfo AppInfo { get; }
        public AppUiState Ui { get; }

        public MainViewModel()
        {
            AppInfo = new AppInfo();
            Ui = AppUiState.Instance;
        }
    }
}
