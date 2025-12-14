using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace WashinPC.Controls
{
    public partial class SidebarControl : UserControl
    {
        // Публичное событие навигации
        public event EventHandler<string> PageChanged;

        private readonly Dictionary<string, Button> _buttons =
            new Dictionary<string, Button>(StringComparer.OrdinalIgnoreCase);

        private string _activePage;

        public SidebarControl()
        {
            InitializeComponent();

            if (IsDesignTime())
                return;

            ApplyRuntimeStyle();
            WireButtons();
            SetActive("Cleaner");
        }

        private bool IsDesignTime()
        {
            return DesignMode ||
                   System.ComponentModel.LicenseManager.UsageMode ==
                   System.ComponentModel.LicenseUsageMode.Designtime;
        }

        private void ApplyRuntimeStyle()
        {
            BackColor = Color.White;

            lblApp.Font = Theme.FontHeader;
            lblApp.ForeColor = Theme.PrimaryText;

            StyleNavButton(btnCleaner, "Cleaner");
            StyleNavButton(btnPrivacy, "Privacy");
            StyleNavButton(btnRegistry, "Registry");
            StyleNavButton(btnSettings, "Settings");
        }

        private void StyleNavButton(Button b, string key)
        {
            _buttons[key] = b;

            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.TextAlign = ContentAlignment.MiddleLeft;
            b.Padding = new Padding(14, 0, 0, 0);
            b.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            b.Height = 44;
            b.Dock = DockStyle.Top;

            b.BackColor = Color.White;
            b.ForeColor = Theme.PrimaryText;

            // hover
            var hover = ControlPaint.Light(Theme.Border, 0.6f);
            b.MouseEnter += (s, e) =>
            {
                if (_activePage != key)
                    b.BackColor = hover;
            };
            b.MouseLeave += (s, e) =>
            {
                if (_activePage != key)
                    b.BackColor = Color.White;
            };
        }

        private void WireButtons()
        {
            btnCleaner.Click += (s, e) => Navigate("Cleaner");
            btnPrivacy.Click += (s, e) => Navigate("Privacy");
            btnRegistry.Click += (s, e) => Navigate("Registry");
            btnSettings.Click += (s, e) => Navigate("Settings");
        }

        private void Navigate(string page)
        {
            SetActive(page);
            PageChanged?.Invoke(this, page);
        }

        public void SetActive(string page)
        {
            _activePage = page;

            foreach (var kv in _buttons)
            {
                var isActive = kv.Key.Equals(page, StringComparison.OrdinalIgnoreCase);
                kv.Value.BackColor = isActive ? Theme.Border : Color.White;
            }
        }
    }
}
