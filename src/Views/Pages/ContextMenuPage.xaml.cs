using System.Windows.Controls;

namespace WashinPC.Views.Pages
{
    public partial class ContextMenuPage : UserControl
    {
        private bool _initialScanStarted;
        public ContextMenuPage()
        {
            InitializeComponent();
            Loaded += (sender, args) =>
            {
                // The headless renderer never scans the host's context menu.
                if (_initialScanStarted || System.Windows.PresentationSource.FromVisual(this) == null) return;
                var model = DataContext as ViewModels.ContextMenuViewModel;
                if (model != null && model.ScanCommand.CanExecute(null))
                {
                    _initialScanStarted = true;
                    model.ScanCommand.Execute(null);
                }
            };
        }
    }
}
