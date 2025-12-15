using System.Windows;

namespace WashinPC.UI.Dialogs
{
    public partial class AppDialog : Window
    {
        public DialogResult Result => ((DialogViewModel)DataContext).Result;

        public AppDialog(string title, string message, DialogType type)
        {
            InitializeComponent();
            DataContext = new DialogViewModel(title, message, type, Close);
        }
    }
}
