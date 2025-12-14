namespace WashinPC.Pages
{
    partial class CleanerPage
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Label lblHeader;
        private System.Windows.Forms.FlowLayoutPanel cardsPanel;

        private System.Windows.Forms.Panel topPanel;
        private System.Windows.Forms.Button btnScan;
        private System.Windows.Forms.Button btnClean;
        private System.Windows.Forms.Button btnRecycle;
        private System.Windows.Forms.CheckBox chkAggressive;

        private System.Windows.Forms.Panel bottomPanel;
        private System.Windows.Forms.Label lblSummary;

        private System.Windows.Forms.Panel statusPanel;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.ProgressBar progress;
        private System.Windows.Forms.Button btnCancel;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.lblHeader = new System.Windows.Forms.Label();
            this.cardsPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.topPanel = new System.Windows.Forms.Panel();
            this.btnScan = new System.Windows.Forms.Button();
            this.btnClean = new System.Windows.Forms.Button();
            this.btnRecycle = new System.Windows.Forms.Button();
            this.chkAggressive = new System.Windows.Forms.CheckBox();
            this.bottomPanel = new System.Windows.Forms.Panel();
            this.lblSummary = new System.Windows.Forms.Label();
            this.statusPanel = new System.Windows.Forms.Panel();
            this.lblStatus = new System.Windows.Forms.Label();
            this.progress = new System.Windows.Forms.ProgressBar();
            this.btnCancel = new System.Windows.Forms.Button();
            this.topPanel.SuspendLayout();
            this.bottomPanel.SuspendLayout();
            this.statusPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // lblHeader
            // 
            this.lblHeader.AutoSize = true;
            this.lblHeader.Location = new System.Drawing.Point(0, 0);
            this.lblHeader.Name = "lblHeader";
            this.lblHeader.Size = new System.Drawing.Size(52, 15);
            this.lblHeader.TabIndex = 0;
            this.lblHeader.Text = "Очистка";
            // 
            // topPanel
            // 
            this.topPanel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.topPanel.Controls.Add(this.chkAggressive);
            this.topPanel.Controls.Add(this.btnRecycle);
            this.topPanel.Controls.Add(this.btnClean);
            this.topPanel.Controls.Add(this.btnScan);
            this.topPanel.Location = new System.Drawing.Point(0, 28);
            this.topPanel.Name = "topPanel";
            this.topPanel.Size = new System.Drawing.Size(860, 40);
            this.topPanel.TabIndex = 1;
            // 
            // btnScan
            // 
            this.btnScan.Location = new System.Drawing.Point(0, 6);
            this.btnScan.Name = "btnScan";
            this.btnScan.Size = new System.Drawing.Size(140, 30);
            this.btnScan.TabIndex = 0;
            this.btnScan.Text = "Сканировать";
            this.btnScan.UseVisualStyleBackColor = true;
            // 
            // btnClean
            // 
            this.btnClean.Location = new System.Drawing.Point(148, 6);
            this.btnClean.Name = "btnClean";
            this.btnClean.Size = new System.Drawing.Size(140, 30);
            this.btnClean.TabIndex = 1;
            this.btnClean.Text = "Очистить";
            this.btnClean.UseVisualStyleBackColor = true;
            // 
            // btnRecycle
            // 
            this.btnRecycle.Location = new System.Drawing.Point(296, 6);
            this.btnRecycle.Name = "btnRecycle";
            this.btnRecycle.Size = new System.Drawing.Size(170, 30);
            this.btnRecycle.TabIndex = 2;
            this.btnRecycle.Text = "Очистить Корзину";
            this.btnRecycle.UseVisualStyleBackColor = true;
            // 
            // chkAggressive
            // 
            this.chkAggressive.AutoSize = true;
            this.chkAggressive.Location = new System.Drawing.Point(480, 11);
            this.chkAggressive.Name = "chkAggressive";
            this.chkAggressive.Size = new System.Drawing.Size(135, 19);
            this.chkAggressive.TabIndex = 3;
            this.chkAggressive.Text = "Агрессивный режим";
            this.chkAggressive.UseVisualStyleBackColor = true;
            // 
            // cardsPanel
            // 
            this.cardsPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cardsPanel.AutoScroll = true;
            this.cardsPanel.Location = new System.Drawing.Point(0, 78);
            this.cardsPanel.Name = "cardsPanel";
            this.cardsPanel.Size = new System.Drawing.Size(860, 360);
            this.cardsPanel.TabIndex = 2;
            // 
            // bottomPanel
            // 
            this.bottomPanel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)
            | System.Windows.Forms.AnchorStyles.Bottom)));
            this.bottomPanel.Controls.Add(this.lblSummary);
            this.bottomPanel.Location = new System.Drawing.Point(0, 444);
            this.bottomPanel.Name = "bottomPanel";
            this.bottomPanel.Size = new System.Drawing.Size(860, 30);
            this.bottomPanel.TabIndex = 3;
            // 
            // lblSummary
            // 
            this.lblSummary.AutoSize = true;
            this.lblSummary.Location = new System.Drawing.Point(0, 8);
            this.lblSummary.Name = "lblSummary";
            this.lblSummary.Size = new System.Drawing.Size(10, 15);
            this.lblSummary.TabIndex = 0;
            this.lblSummary.Text = " ";
            // 
            // statusPanel
            // 
            this.statusPanel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)
            | System.Windows.Forms.AnchorStyles.Bottom)));
            this.statusPanel.Controls.Add(this.btnCancel);
            this.statusPanel.Controls.Add(this.progress);
            this.statusPanel.Controls.Add(this.lblStatus);
            this.statusPanel.Location = new System.Drawing.Point(0, 480);
            this.statusPanel.Name = "statusPanel";
            this.statusPanel.Size = new System.Drawing.Size(860, 46);
            this.statusPanel.TabIndex = 4;
            // 
            // lblStatus
            // 
            this.lblStatus.AutoEllipsis = true;
            this.lblStatus.Location = new System.Drawing.Point(0, 14);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(380, 18);
            this.lblStatus.TabIndex = 0;
            this.lblStatus.Text = "";
            // 
            // progress
            // 
            this.progress.Location = new System.Drawing.Point(392, 14);
            this.progress.Name = "progress";
            this.progress.Size = new System.Drawing.Size(280, 18);
            this.progress.TabIndex = 1;
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(686, 9);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(170, 30);
            this.btnCancel.TabIndex = 2;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // CleanerPage
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.Controls.Add(this.statusPanel);
            this.Controls.Add(this.bottomPanel);
            this.Controls.Add(this.cardsPanel);
            this.Controls.Add(this.topPanel);
            this.Controls.Add(this.lblHeader);
            this.Name = "CleanerPage";
            this.Size = new System.Drawing.Size(860, 526);
            this.topPanel.ResumeLayout(false);
            this.topPanel.PerformLayout();
            this.bottomPanel.ResumeLayout(false);
            this.bottomPanel.PerformLayout();
            this.statusPanel.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
