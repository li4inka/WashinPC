using System.Windows.Controls;
using WashinPC.ViewModels.Pages;

namespace WashinPC.Views.Pages
{
    public partial class RegistryPage : UserControl
    {
        public RegistryPage()
        {
            InitializeComponent();
            DataContext = new RegistryViewModel();
        }
    }
}
