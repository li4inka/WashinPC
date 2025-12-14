using System.Windows.Forms;

namespace WashinPC.Pages
{
    public partial class SettingsPage : UserControl
    {
        public SettingsPage()
        {
            InitializeComponent();

            if (IsDesignTime())
                return;

            Theme.Apply(this);

            cmbLanguage.SelectedIndex = Localizer.Current == Lang.RU ? 0 : 1;

            cmbLanguage.SelectedIndexChanged += (s, e) =>
            {
                Localizer.Current = cmbLanguage.SelectedIndex == 0 ? Lang.RU : Lang.EN;
                Application.Restart();
            };
        }

        private bool IsDesignTime()
        {
            return DesignMode ||
                   System.ComponentModel.LicenseManager.UsageMode ==
                   System.ComponentModel.LicenseUsageMode.Designtime;
        }
    }
}
