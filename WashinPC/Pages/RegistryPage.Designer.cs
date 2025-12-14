namespace WashinPC.Pages
{
    partial class RegistryPage
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Label lblHeader;

        private System.Windows.Forms.Panel topPanel;
        private System.Windows.Forms.Button btnScan;
        private System.Windows.Forms.Button btnFix;

        private System.Windows.Forms.ListView listIssues;
        private System.Windows.Forms.ColumnHeader colCategory;
        private System.Windows.Forms.ColumnHeader colKey;
        private System.Windows.Forms.ColumnHeader colValue;
        private System.Windows.Forms.ColumnHeader colDetails;

        private System.Windows.Forms.Panel bottomPanel;
        private System.Windows.Forms.Label lblSummary;
        private System.Windows.Forms.Label lblStatus;

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
            this.btnFix = new System.Windows.Forms.Button();
            this.listIssues = new System.Windows.Forms.ListView();
            this.colCategory = new System.Windows.Forms.ColumnHeader();
            this.colKey = new System.Windows.Forms.ColumnHeader();
            this.colValue = new System.Windows.Forms.ColumnHeader();
            this.colDetails = new System.Windows.Forms.ColumnHeader();
            this.bottomPanel = new System.Windows.Forms.Panel();
            this.lblSummary = new System.Windows.Forms.Label();
            this.lblStatus = new System.Windows.Forms.Label();
            this.topPanel.SuspendLayout();
            this.bottomPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // lblHeader
            // 
            this.lblHeader.AutoSize = true;
            this.lblHeader.Location = new System.Drawing.Point(0, 0);
            this.lblHeader.Name = "lblHeader";
            this.lblHeader.Size = new System.Drawing.Size(49, 15);
            this.lblHeader.TabIndex = 0;
            this.lblHeader.Text = "Реестр";
            // 
            // topPanel
            // 
            this.topPanel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.topPanel.Controls.Add(this.btnFix);
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
            // btnFix
            // 
            this.btnFix.Location = new System.Drawing.Point(148, 6);
            this.btnFix.Name = "btnFix";
            this.btnFix.Size = new System.Drawing.Size(140, 30);
            this.btnFix.TabIndex = 1;
            this.btnFix.Text = "Исправить";
            this.btnFix.UseVisualStyleBackColor = true;
            // 
            // listIssues
            // 
            this.listIssues.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.listIssues.CheckBoxes = true;
            this.listIssues.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colCategory,
            this.colKey,
            this.colValue,
            this.colDetails});
            this.listIssues.FullRowSelect = true;
            this.listIssues.Location = new System.Drawing.Point(0, 78);
            this.listIssues.Name = "listIssues";
            this.listIssues.Size = new System.Drawing.Size(860, 360);
            this.listIssues.TabIndex = 2;
            this.listIssues.View = System.Windows.Forms.View.Details;
            // 
            // colCategory
            // 
            this.colCategory.Text = "Категория";
            this.colCategory.Width = 120;
            // 
            // colKey
            // 
            this.colKey.Text = "Ключ";
            this.colKey.Width = 260;
            // 
            // colValue
            // 
            this.colValue.Text = "Значение";
            this.colValue.Width = 140;
            // 
            // colDetails
            // 
            this.colDetails.Text = "Описание";
            this.colDetails.Width = 300;
            // 
            // bottomPanel
            // 
            this.bottomPanel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)
            | System.Windows.Forms.AnchorStyles.Bottom)));
            this.bottomPanel.Controls.Add(this.lblStatus);
            this.bottomPanel.Controls.Add(this.lblSummary);
            this.bottomPanel.Location = new System.Drawing.Point(0, 444);
            this.bottomPanel.Name = "bottomPanel";
            this.bottomPanel.Size = new System.Drawing.Size(860, 46);
            this.bottomPanel.TabIndex = 3;
            // 
            // lblSummary
            // 
            this.lblSummary.AutoSize = true;
            this.lblSummary.Location = new System.Drawing.Point(0, 6);
            this.lblSummary.Name = "lblSummary";
            this.lblSummary.Size = new System.Drawing.Size(10, 15);
            this.lblSummary.TabIndex = 0;
            this.lblSummary.Text = " ";
            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.Location = new System.Drawing.Point(0, 24);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(10, 15);
            this.lblStatus.TabIndex = 1;
            this.lblStatus.Text = " ";
            // 
            // RegistryPage
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.Controls.Add(this.bottomPanel);
            this.Controls.Add(this.listIssues);
            this.Controls.Add(this.topPanel);
            this.Controls.Add(this.lblHeader);
            this.Name = "RegistryPage";
            this.Size = new System.Drawing.Size(860, 490);
            this.topPanel.ResumeLayout(false);
            this.bottomPanel.ResumeLayout(false);
            this.bottomPanel.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
