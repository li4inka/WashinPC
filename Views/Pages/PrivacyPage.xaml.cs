using System.Windows.Controls;
using WashinPC.ViewModels.Pages;

namespace WashinPC.Views.Pages
{
    public partial class PrivacyPage : UserControl
    {
        public PrivacyPage()
        {
            InitializeComponent();
            DataContext = new PrivacyViewModel();
        }
    }
}
