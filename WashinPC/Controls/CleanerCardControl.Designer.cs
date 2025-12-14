namespace WashinPC.Controls
{
    partial class CleanerCardControl
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblInfo;
        private System.Windows.Forms.Label lblHint;
        private System.Windows.Forms.CheckBox chkEnabled;
        private System.Windows.Forms.Button btnDetails;
        private System.Windows.Forms.Panel divider;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblInfo = new System.Windows.Forms.Label();
            this.lblHint = new System.Windows.Forms.Label();
            this.chkEnabled = new System.Windows.Forms.CheckBox();
            this.btnDetails = new System.Windows.Forms.Button();
            this.divider = new System.Windows.Forms.Panel();
            this.SuspendLayout();
            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Location = new System.Drawing.Point(12, 12);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(93, 15);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "Category name";
            this.lblTitle.Click += new System.EventHandler(this.Card_Click);
            // 
            // lblInfo
            // 
            this.lblInfo.AutoSize = true;
            this.lblInfo.Location = new System.Drawing.Point(12, 38);
            this.lblInfo.Name = "lblInfo";
            this.lblInfo.Size = new System.Drawing.Size(128, 15);
            this.lblInfo.TabIndex = 1;
            this.lblInfo.Text = "0 files • 0 MB (0 GB)";
            this.lblInfo.Click += new System.EventHandler(this.Card_Click);
            // 
            // lblHint
            // 
            this.lblHint.AutoSize = true;
            this.lblHint.Location = new System.Drawing.Point(12, 58);
            this.lblHint.Name = "lblHint";
            this.lblHint.Size = new System.Drawing.Size(165, 15);
            this.lblHint.TabIndex = 2;
            this.lblHint.Text = "Tip: click to toggle selection";
            this.lblHint.Click += new System.EventHandler(this.Card_Click);
            // 
            // divider
            // 
            this.divider.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right) 
            | System.Windows.Forms.AnchorStyles.Bottom)));
            this.divider.Location = new System.Drawing.Point(12, 84);
            this.divider.Name = "divider";
            this.divider.Size = new System.Drawing.Size(296, 1);
            this.divider.TabIndex = 5;
            // 
            // chkEnabled
            // 
            this.chkEnabled.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Bottom)));
            this.chkEnabled.AutoSize = true;
            this.chkEnabled.Checked = true;
            this.chkEnabled.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkEnabled.Location = new System.Drawing.Point(12, 94);
            this.chkEnabled.Name = "chkEnabled";
            this.chkEnabled.Size = new System.Drawing.Size(66, 19);
            this.chkEnabled.TabIndex = 3;
            this.chkEnabled.Text = "Enable";
            this.chkEnabled.UseVisualStyleBackColor = true;
            this.chkEnabled.CheckedChanged += new System.EventHandler(this.chkEnabled_CheckedChanged);
            // 
            // btnDetails
            // 
            this.btnDetails.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Right | System.Windows.Forms.AnchorStyles.Bottom)));
            this.btnDetails.Location = new System.Drawing.Point(228, 90);
            this.btnDetails.Name = "btnDetails";
            this.btnDetails.Size = new System.Drawing.Size(80, 26);
            this.btnDetails.TabIndex = 4;
            this.btnDetails.Text = "Details";
            this.btnDetails.UseVisualStyleBackColor = true;
            this.btnDetails.Click += new System.EventHandler(this.btnDetails_Click);
            // 
            // CleanerCardControl
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.White;
            this.Controls.Add(this.btnDetails);
            this.Controls.Add(this.chkEnabled);
            this.Controls.Add(this.divider);
            this.Controls.Add(this.lblHint);
            this.Controls.Add(this.lblInfo);
            this.Controls.Add(this.lblTitle);
            this.Cursor = System.Windows.Forms.Cursors.Hand;
            this.Name = "CleanerCardControl";
            this.Size = new System.Drawing.Size(320, 128);
            this.Click += new System.EventHandler(this.Card_Click);
            this.ResumeLayout(false);
            this.PerformLayout();

        }
    }
}
