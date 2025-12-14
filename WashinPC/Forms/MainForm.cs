using System;
using System.Windows.Forms;
using WashinPC.Controls;
using WashinPC.Pages;

namespace WashinPC.Forms
{
    public partial class MainForm : Form
    {
        private CleanerPage _cleanerPage;
        private PrivacyPage _privacyPage;
        private RegistryPage _registryPage;

        public MainForm()
        {
            InitializeComponent();

            if (IsDesignTime())
                return;

            Text = Localizer.T("app.title");

            // pages
            _cleanerPage = new CleanerPage();
            _privacyPage = new PrivacyPage();
            _registryPage = new RegistryPage();

            _cleanerPage.Dock = DockStyle.Fill;
            _privacyPage.Dock = DockStyle.Fill;
            _registryPage.Dock = DockStyle.Fill;

            // sidebar
            sidebar.PageChanged += Sidebar_PageChanged;

            // theme
            Theme.Apply(this);

            // default page
            ShowPage("Cleaner");
        }

        private bool IsDesignTime()
        {
            return DesignMode ||
                   System.ComponentModel.LicenseManager.UsageMode ==
                   System.ComponentModel.LicenseUsageMode.Designtime;
        }

        private void Sidebar_PageChanged(object sender, string page)
        {
            sidebar.SetActive(page);
            ShowPage(page);
        }

        private void ShowPage(string page)
        {
            contentHost.Controls.Clear();

            switch (page)
            {
                case "Cleaner":
                    contentHost.Controls.Add(_cleanerPage);
                    break;

                case "Privacy":
                    contentHost.Controls.Add(_privacyPage);
                    break;

                case "Registry":
                    contentHost.Controls.Add(_registryPage);
                    break;

                case "Settings":
                    contentHost.Controls.Add(new SettingsPage());
                    break;

                default:
                    contentHost.Controls.Add(_cleanerPage);
                    break;
            }
        }

        private Control CreatePlaceholder(string title)
        {
            var p = new Panel { Dock = DockStyle.Fill };
            var lbl = new Label
            {
                AutoSize = true,
                Text = title,
                Font = Theme.FontHeader,
                Left = 20,
                Top = 20
            };
            p.Controls.Add(lbl);
            // Theme.Apply(p as UserControl); // не сработает для Panel, но лейбл уже styled через Theme.Apply(form)
            return p;
        }
    }
}
