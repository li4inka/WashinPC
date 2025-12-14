namespace WashinPC.Pages
{
    partial class PrivacyPage
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Label lblHeader;
        private System.Windows.Forms.Panel topPanel;
        private System.Windows.Forms.Button btnScan;
        private System.Windows.Forms.Button btnClean;

        private System.Windows.Forms.FlowLayoutPanel cardsPanel;

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
            this.topPanel = new System.Windows.Forms.Panel();
            this.btnScan = new System.Windows.Forms.Button();
            this.btnClean = new System.Windows.Forms.Button();
            this.cardsPanel = new System.Windows.Forms.FlowLayoutPanel();
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
            this.lblHeader.Size = new System.Drawing.Size(79, 15);
            this.lblHeader.TabIndex = 0;
            this.lblHeader.Text = "Приватность";
            // 
            // topPanel
            // 
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
            // 
            // btnClean
            // 
            this.btnClean.Location = new System.Drawing.Point(148, 6);
            this.btnClean.Name = "btnClean";
            this.btnClean.Size = new System.Drawing.Size(140, 30);
            this.btnClean.TabIndex = 1;
            this.btnClean.Text = "Очистить";
            // 
            // cardsPanel
            // 
            this.cardsPanel.AutoScroll = true;
            this.cardsPanel.Location = new System.Drawing.Point(0, 78);
            this.cardsPanel.Name = "cardsPanel";
            this.cardsPanel.Size = new System.Drawing.Size(860, 360);
            this.cardsPanel.TabIndex = 2;
            // 
            // bottomPanel
            // 
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
            this.lblStatus.Location = new System.Drawing.Point(0, 14);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(360, 18);
            // 
            // progress
            // 
            this.progress.Location = new System.Drawing.Point(372, 14);
            this.progress.Name = "progress";
            this.progress.Size = new System.Drawing.Size(260, 18);
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(646, 9);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(140, 30);
            this.btnCancel.TabIndex = 2;
            this.btnCancel.Text = "Cancel";
            // 
            // PrivacyPage
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.Controls.Add(this.statusPanel);
            this.Controls.Add(this.bottomPanel);
            this.Controls.Add(this.cardsPanel);
            this.Controls.Add(this.topPanel);
            this.Controls.Add(this.lblHeader);
            this.Name = "PrivacyPage";
            this.Size = new System.Drawing.Size(860, 526);
            this.topPanel.ResumeLayout(false);
            this.bottomPanel.ResumeLayout(false);
            this.bottomPanel.PerformLayout();
            this.statusPanel.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
