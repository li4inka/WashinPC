using System.Windows.Controls;
using WashinPC.ViewModels.Pages;

namespace WashinPC.Views.Pages
{
    public partial class CleanerPage : UserControl
    {
        public CleanerPage()
        {
            InitializeComponent();
            DataContext = new CleanerViewModel();
        }
    }
}
