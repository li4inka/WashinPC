using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace WashinPC.Pages
{
    public partial class RegistryPage : UserControl
    {
        private readonly RegistryScanner _scanner = new RegistryScanner();
        private List<RegistryIssue> _issues = new List<RegistryIssue>();

        public RegistryPage()
        {
            InitializeComponent();

            if (IsDesignTime())
                return;

            ApplyRuntimeStyle();
            WireEvents();
        }

        private bool IsDesignTime()
        {
            return DesignMode ||
                   System.ComponentModel.LicenseManager.UsageMode ==
                   System.ComponentModel.LicenseUsageMode.Designtime;
        }

        private void ApplyRuntimeStyle()
        {
            Theme.Apply(this);

            BackColor = Theme.Background;

            lblHeader.Font = Theme.FontHeader;
            lblHeader.ForeColor = Theme.PrimaryText;

            lblSummary.ForeColor = Theme.SecondaryText;
            lblStatus.ForeColor = Theme.SecondaryText;

            Theme.StylePrimaryButton(btnScan);
            Theme.StylePrimaryButton(btnFix);

            Theme.EnableHover(btnScan);
            Theme.EnableHover(btnFix);

            listIssues.BorderStyle = BorderStyle.None;
            listIssues.FullRowSelect = true;
            listIssues.CheckBoxes = true;
        }

        private void WireEvents()
        {
            btnScan.Click += (s, e) => Scan();
            btnFix.Click += (s, e) => FixSelected();
        }

        // ───────────────────────── SCAN ─────────────────────────

        private void Scan()
        {
            listIssues.Items.Clear();
            _issues.Clear();

            lblStatus.Text = "Scanning registry…";

            try
            {
                _issues = _scanner.ScanAll().ToList();

                foreach (var i in _issues)
                {
                    var item = new ListViewItem(i.Category)
                    {
                        Checked = true
                    };

                    item.SubItems.Add(i.KeyPath);
                    item.SubItems.Add(i.ValueName);
                    item.SubItems.Add(i.Details);

                    listIssues.Items.Add(item);
                }

                lblSummary.Text =
                    Localizer.T("status.found.issues", _issues.Count);

                lblStatus.Text = "Scan complete.";
            }
            catch (Exception ex)
            {
                Utils.Log("Registry scan error: " + ex);
                lblStatus.Text = "Error during scan.";
                MessageBox.Show(ex.Message, "WashinPC", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ───────────────────────── FIX ─────────────────────────

        private void FixSelected()
        {
            if (_issues.Count == 0)
            {
                MessageBox.Show("Run Scan first.", "WashinPC",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var selectedIndexes = listIssues.Items
                .Cast<ListViewItem>()
                .Select((item, index) => new { item, index })
                .Where(x => x.item.Checked)
                .Select(x => x.index)
                .ToList();

            if (selectedIndexes.Count == 0)
            {
                MessageBox.Show(Localizer.T("none.selected"), "WashinPC",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (MessageBox.Show(
                    Localizer.T("confirm.fix"),
                    "WashinPC",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            int fixedCount = 0;

            foreach (var idx in selectedIndexes)
            {
                try
                {
                    _issues[idx].Fix?.Invoke();
                    fixedCount++;
                }
                catch (Exception ex)
                {
                    Utils.Log("Registry fix error: " + ex);
                }
            }

            lblStatus.Text = $"Fixed {fixedCount} issue(s).";
            Scan(); // refresh
        }
    }
}
