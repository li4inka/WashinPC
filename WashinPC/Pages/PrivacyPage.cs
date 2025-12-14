using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WashinPC.Controls;

namespace WashinPC.Pages
{
    public partial class PrivacyPage : UserControl
    {
        private readonly PrivacyEngine _engine = new PrivacyEngine();
        private CancellationTokenSource _cts;

        private readonly Dictionary<string, GroupResult> _groups = new Dictionary<string, GroupResult>();
        private readonly Dictionary<string, bool> _enabled = new Dictionary<string, bool>();

        public PrivacyPage()
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
            Theme.StylePrimaryButton(btnClean);
            Theme.StyleSecondaryButton(btnCancel);

            Theme.EnableHover(btnScan);
            Theme.EnableHover(btnClean);
            Theme.EnableHover(btnCancel);

            progress.Visible = false;
            btnCancel.Visible = false;
        }

        private void WireEvents()
        {
            btnScan.Click += async (s, e) => await ScanAsync();
            btnClean.Click += async (s, e) => await CleanAsync();
            btnCancel.Click += (s, e) => CancelWork();
        }

        private void StartWork(string status)
        {
            CancelWork();
            _cts = new CancellationTokenSource();

            lblStatus.Text = status;
            progress.Value = 0;
            progress.Visible = true;
            btnCancel.Visible = true;

            btnScan.Enabled = false;
            btnClean.Enabled = false;
        }

        private void EndWork(string status)
        {
            btnScan.Enabled = true;
            btnClean.Enabled = true;

            progress.Visible = false;
            btnCancel.Visible = false;
            lblStatus.Text = status;

            _cts?.Dispose();
            _cts = null;
        }

        private void CancelWork()
        {
            try
            {
                if (_cts != null && !_cts.IsCancellationRequested)
                    _cts.Cancel();
            }
            catch { }
        }

        private void SetProgressSafe(int percent, string status = null)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => SetProgressSafe(percent, status)));
                return;
            }

            progress.Value = Math.Max(0, Math.Min(100, percent));
            if (status != null)
                lblStatus.Text = status;
        }

        // ───────────── SCAN ─────────────

        private async Task ScanAsync()
        {
            StartWork("Scanning privacy data…");

            try
            {
                var token = _cts.Token;

                var data = await Task.Run(() =>
                {
                    token.ThrowIfCancellationRequested();
                    int files;
                    long bytes;
                    var groups = _engine.Scan(out files, out bytes);
                    return (groups, files, bytes);
                }, token);

                token.ThrowIfCancellationRequested();

                cardsPanel.Controls.Clear();
                _groups.Clear();

                foreach (var g in data.groups)
                {
                    _groups[g.GroupName] = g;
                    if (!_enabled.ContainsKey(g.GroupName))
                        _enabled[g.GroupName] = true;

                    cardsPanel.Controls.Add(CreateCard(g));
                }

                lblSummary.Text = Localizer.T(
                    "label.total.privacy",
                    data.files,
                    data.bytes / (1024 * 1024),
                    (data.bytes / (1024.0 * 1024 * 1024)).ToString("0.00")
                );

                EndWork("Scan complete.");
            }
            catch (OperationCanceledException)
            {
                EndWork("Cancelled.");
            }
            catch (Exception ex)
            {
                Utils.Log("Privacy scan error: " + ex);
                EndWork("Error.");
                MessageBox.Show(ex.Message, "WashinPC", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ───────────── CLEAN ─────────────

        private async Task CleanAsync()
        {
            if (_groups.Count == 0)
            {
                MessageBox.Show("Run Scan first.", "WashinPC",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var selected = _groups.Values
                .Where(g => _enabled.TryGetValue(g.GroupName, out var on) && on)
                .ToList();

            if (selected.Count == 0)
            {
                MessageBox.Show(Localizer.T("none.selected"), "WashinPC",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (MessageBox.Show(
                    Localizer.T("confirm.clean"),
                    "WashinPC",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            StartWork("Cleaning privacy data…");

            try
            {
                var token = _cts.Token;
                int total = selected.Sum(g => g.Files.Count);
                int done = 0;

                await Task.Run(() =>
                {
                    foreach (var g in selected)
                    {
                        token.ThrowIfCancellationRequested();

                        foreach (var f in g.Files)
                        {
                            token.ThrowIfCancellationRequested();
                            Utils.SafeDeleteFile(f, null);

                            done++;
                            if (done % 20 == 0 || done == total)
                            {
                                int p = total == 0 ? 0 : (int)(done * 100L / total);
                                SetProgressSafe(p, $"Cleaning… {done}/{total}");
                            }
                        }
                    }
                }, token);

                EndWork("Cleaning complete.");
                await ScanAsync();
            }
            catch (OperationCanceledException)
            {
                EndWork("Cancelled.");
            }
            catch (Exception ex)
            {
                Utils.Log("Privacy clean error: " + ex);
                EndWork("Error.");
                MessageBox.Show(ex.Message, "WashinPC", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private CleanerCardControl CreateCard(GroupResult g)
        {
            var card = new CleanerCardControl
            {
                Title = g.GroupName,
                Info = $"{g.Files.Count} files • {(g.TotalBytes / (1024 * 1024))} MB",
                Hint = "Browser data, cache, history"
            };

            card.EnabledToggle = _enabled[g.GroupName];
            card.EnabledChanged += (s, e) =>
            {
                _enabled[g.GroupName] = card.EnabledToggle;
            };

            card.DetailsClicked += (s, e) =>
                ShowDetails(g.GroupName, g.Files);

            return card;
        }

        private void ShowDetails(string title, IList<string> files)
        {
            var f = new Form
            {
                Text = title,
                Size = new Size(820, 520),
                StartPosition = FormStartPosition.CenterParent
            };

            var list = new ListBox { Dock = DockStyle.Fill };
            list.Items.AddRange(files.ToArray());

            f.Controls.Add(list);
            Theme.Apply(f);
            f.ShowDialog(this);
        }
    }
}
