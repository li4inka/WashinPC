using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WashinPC.Controls;

namespace WashinPC.Pages
{
    public partial class CleanerPage : UserControl
    {
        private readonly CleanerEngine _engine = new CleanerEngine();

        private CancellationTokenSource _cts;

        // Состояние карточек
        private readonly Dictionary<string, GroupResult> _groups = new Dictionary<string, GroupResult>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, bool> _enabled = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        public CleanerPage()
        {
            InitializeComponent();

            if (IsDesignTime())
                return;

            ApplyRuntimeStyle();
            WireEvents();
        }

        private bool IsDesignTime()
        {
            return DesignMode || LicenseManager.UsageMode == System.ComponentModel.LicenseUsageMode.Designtime;
        }

        private void ApplyRuntimeStyle()
        {
            Theme.Apply(this);

            // фон страницы
            BackColor = Theme.Background;

            // “заголовок”
            lblHeader.Font = Theme.FontHeader;
            lblHeader.ForeColor = Theme.PrimaryText;

            // summary
            lblSummary.ForeColor = Theme.SecondaryText;
            lblStatus.ForeColor = Theme.SecondaryText;

            // кнопки
            Theme.StylePrimaryButton(btnScan);
            Theme.StylePrimaryButton(btnClean);
            Theme.StyleSecondaryButton(btnRecycle);
            Theme.StyleSecondaryButton(btnCancel);

            Theme.EnableHover(btnScan);
            Theme.EnableHover(btnClean);
            Theme.EnableHover(btnRecycle);
            Theme.EnableHover(btnCancel);

            // прогресс
            progress.Visible = false;
            btnCancel.Enabled = false;
            btnCancel.Visible = false;

            // cards container
            cardsPanel.BackColor = Theme.Background;
        }

        private void WireEvents()
        {
            btnScan.Click += async (s, e) => await ScanAsync();
            btnClean.Click += async (s, e) => await CleanAsync();
            btnCancel.Click += (s, e) => CancelWork();
            btnRecycle.Click += (s, e) =>
            {
                Native.EmptyRecycleBinQuiet();
                MessageBox.Show(Localizer.T("recycle.done"), "WashinPC", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            chkAggressive.CheckedChanged += (s, e) =>
            {
                _engine.AggressiveMode = chkAggressive.Checked;
                _engine.InitRules();
            };
        }

        // ───────────────────────── ASYNC WORKFLOW ─────────────────────────

        private void StartWork(string status)
        {
            CancelWork(); // на всякий случай

            _cts = new CancellationTokenSource();

            progress.Value = 0;
            progress.Visible = true;

            btnCancel.Enabled = true;
            btnCancel.Visible = true;

            lblStatus.Text = status;

            btnScan.Enabled = false;
            btnClean.Enabled = false;
            btnRecycle.Enabled = false;
            chkAggressive.Enabled = false;
        }

        private void EndWork(string status)
        {
            btnScan.Enabled = true;
            btnClean.Enabled = true;
            btnRecycle.Enabled = true;
            chkAggressive.Enabled = true;

            btnCancel.Enabled = false;
            btnCancel.Visible = false;

            progress.Visible = false;
            progress.Value = 0;

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

            if (percent < 0) percent = 0;
            if (percent > 100) percent = 100;

            progress.Value = percent;
            if (status != null) lblStatus.Text = status;
        }

        // ───────────────────────── SCAN ─────────────────────────

        public async Task ScanAsync()
        {
            StartWork("Scanning…");

            try
            {
                var token = _cts.Token;

                var result = await Task.Run(() =>
                {
                    token.ThrowIfCancellationRequested();

                    int totalFiles;
                    long totalBytes;
                    var groups = _engine.ScanDetailed(out totalFiles, out totalBytes);

                    return (groups, totalFiles, totalBytes);
                }, token);

                token.ThrowIfCancellationRequested();

                // обновляем UI
                cardsPanel.SuspendLayout();
                cardsPanel.Controls.Clear();
                _groups.Clear();

                foreach (var g in result.groups)
                {
                    _groups[g.GroupName] = g;
                    if (!_enabled.ContainsKey(g.GroupName))
                        _enabled[g.GroupName] = true;

                    var card = CreateCard(g);
                    cardsPanel.Controls.Add(card);
                }

                cardsPanel.ResumeLayout();

                lblSummary.Text = Localizer.T(
                    "label.total",
                    result.totalFiles,
                    result.totalBytes / (1024 * 1024),
                    (result.totalBytes / (1024.0 * 1024 * 1024)).ToString("0.00")
                );

                EndWork("Scan complete.");
            }
            catch (OperationCanceledException)
            {
                EndWork("Cancelled.");
            }
            catch (Exception ex)
            {
                Utils.Log("CleanerPage Scan error: " + ex);
                EndWork("Error.");
                MessageBox.Show(ex.Message, "WashinPC", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ───────────────────────── CLEAN ─────────────────────────

        public async Task CleanAsync()
        {
            if (_groups.Count == 0)
            {
                MessageBox.Show("Run Scan first.", "WashinPC", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var selected = _groups.Values
                .Where(g => _enabled.TryGetValue(g.GroupName, out var on) && on)
                .ToList();

            if (selected.Count == 0)
            {
                MessageBox.Show(Localizer.T("none.selected"), "WashinPC", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            long bytes = selected.Sum(g => g.TotalBytes);
            int files = selected.Sum(g => g.Files.Count);

            var confirmText =
                Localizer.T("confirm.clean") + "\r\n\r\n" +
                $"Selected groups: {selected.Count}\r\n" +
                $"Files: {files}\r\n" +
                $"Size: {bytes / (1024 * 1024)} MB";

            if (MessageBox.Show(confirmText, "WashinPC", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            StartWork("Cleaning…");

            try
            {
                var token = _cts.Token;

                await Task.Run(() =>
                {
                    token.ThrowIfCancellationRequested();

                    int total = files;
                    int done = 0;

                    foreach (var g in selected)
                    {
                        token.ThrowIfCancellationRequested();

                        foreach (var f in g.Files)
                        {
                            token.ThrowIfCancellationRequested();

                            Utils.SafeDeleteFile(f, null);

                            done++;
                            if (done % 25 == 0 || done == total)
                            {
                                int p = total <= 0 ? 0 : (int)(done * 100L / total);
                                SetProgressSafe(p, $"Cleaning… {done}/{total}");
                            }
                        }
                    }
                }, token);

                EndWork("Cleaning complete.");
                await ScanAsync(); // refresh
            }
            catch (OperationCanceledException)
            {
                EndWork("Cancelled.");
            }
            catch (Exception ex)
            {
                Utils.Log("CleanerPage Clean error: " + ex);
                EndWork("Error.");
                MessageBox.Show(ex.Message, "WashinPC", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ───────────────────────── CARD FACTORY ─────────────────────────

        private CleanerCardControl CreateCard(GroupResult g)
        {
            var card = new CleanerCardControl();

            card.Title = g.GroupName;
            card.Info = $"{g.Files.Count} files • {ToMb(g.TotalBytes)} MB";
            card.Hint = "Click card to toggle • Details shows file list";

            card.EnabledToggle = _enabled.TryGetValue(g.GroupName, out var on) ? on : true;
            card.ToggleChanged += (s, e) =>
            {
                _enabled[g.GroupName] = card.EnabledToggle;
            };


            card.DetailsClicked += (s, e) => ShowDetails(g.GroupName, g.Files);

            return card;
        }

        private static string ToMb(long b) => (b / (1024 * 1024)).ToString();

        private void ShowDetails(string title, IList<string> files)
        {
            var f = new Form
            {
                Text = title,
                Size = new Size(820, 520),
                StartPosition = FormStartPosition.CenterParent
            };

            var list = new ListBox
            {
                Dock = DockStyle.Fill
            };
            list.Items.AddRange(files.ToArray());

            var bottom = new Panel { Dock = DockStyle.Bottom, Height = 44, Padding = new Padding(10, 8, 10, 8) };
            var btnCopy = new Button { Text = "Copy list", Width = 120, Dock = DockStyle.Right };
            Theme.StyleSecondaryButton(btnCopy);
            Theme.EnableHover(btnCopy);

            btnCopy.Click += (s, e) =>
            {
                try { Clipboard.SetText(string.Join(Environment.NewLine, files)); } catch { }
            };

            bottom.Controls.Add(btnCopy);

            f.Controls.Add(list);
            f.Controls.Add(bottom);

            Theme.Apply(f);
            f.ShowDialog(this);
        }

        // protected override void Dispose(bool disposing)
        // {
        //     if (disposing)
        //     {
        //         CancelWork();
        //         _cts?.Dispose();
        //     }
        //     base.Dispose(disposing);
        // }
    }
}
