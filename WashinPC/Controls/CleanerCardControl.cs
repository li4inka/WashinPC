using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace WashinPC.Controls
{
    public partial class CleanerCardControl : UserControl
    {
        // События для страницы (Pages)
        public event EventHandler DetailsClicked;
        public event EventHandler ToggleChanged;


        public CleanerCardControl()
        {
            InitializeComponent();

            // Designer-safe (не трогаем движки, только стиль)
            ApplyStyle();
        }

        private void ApplyStyle()
        {
            BackColor = Theme.Card;

            lblTitle.ForeColor = Theme.PrimaryText;
            lblTitle.Font = Theme.FontCardTitle;

            lblInfo.ForeColor = Theme.SecondaryText;
            lblHint.ForeColor = Theme.SecondaryText;

            btnDetails.FlatStyle = FlatStyle.Flat;
            btnDetails.FlatAppearance.BorderSize = 0;
            btnDetails.BackColor = Theme.Border;
            btnDetails.ForeColor = Theme.PrimaryText;
            btnDetails.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);

            Theme.EnableHover(btnDetails);
        }

        // ───────────── Bind API ─────────────

        [Browsable(true)]
        [Category("WashinPC")]
        public string Title
        {
            get => lblTitle.Text;
            set => lblTitle.Text = value;
        }

        [Browsable(true)]
        [Category("WashinPC")]
        public string Info
        {
            get => lblInfo.Text;
            set => lblInfo.Text = value;
        }

        [Browsable(true)]
        [Category("WashinPC")]
        public string Hint
        {
            get => lblHint.Text;
            set => lblHint.Text = value;
        }

        [Browsable(true)]
        [Category("WashinPC")]
        public bool EnabledToggle
        {
            get => chkEnabled.Checked;
            set => chkEnabled.Checked = value;
        }

        // ───────────── UI handlers ─────────────

        private void btnDetails_Click(object sender, EventArgs e)
        {
            DetailsClicked?.Invoke(this, EventArgs.Empty);
        }

        private void chkEnabled_CheckedChanged(object sender, EventArgs e)
        {
            ToggleChanged?.Invoke(this, EventArgs.Empty);
        }


        // Клик по карточке = переключить чекбокс (приятнее UX)
        private void Card_Click(object sender, EventArgs e)
        {
            // Не переключаем, если клик был по кнопке Details
            var me = sender as Control;
            if (me == btnDetails) return;

            chkEnabled.Checked = !chkEnabled.Checked;
        }
    }
}
