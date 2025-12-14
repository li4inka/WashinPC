namespace WashinPC.Controls
{
    partial class SidebarControl
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Label lblApp;
        private System.Windows.Forms.Panel navPanel;

        private System.Windows.Forms.Button btnCleaner;
        private System.Windows.Forms.Button btnPrivacy;
        private System.Windows.Forms.Button btnRegistry;
        private System.Windows.Forms.Button btnSettings;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.lblApp = new System.Windows.Forms.Label();
            this.navPanel = new System.Windows.Forms.Panel();
            this.btnSettings = new System.Windows.Forms.Button();
            this.btnRegistry = new System.Windows.Forms.Button();
            this.btnPrivacy = new System.Windows.Forms.Button();
            this.btnCleaner = new System.Windows.Forms.Button();
            this.navPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // lblApp
            // 
            this.lblApp.AutoSize = true;
            this.lblApp.Location = new System.Drawing.Point(14, 14);
            this.lblApp.Name = "lblApp";
            this.lblApp.Size = new System.Drawing.Size(64, 15);
            this.lblApp.TabIndex = 0;
            this.lblApp.Text = "WashinPC";
            // 
            // navPanel
            // 
            this.navPanel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)));
            this.navPanel.Controls.Add(this.btnSettings);
            this.navPanel.Controls.Add(this.btnRegistry);
            this.navPanel.Controls.Add(this.btnPrivacy);
            this.navPanel.Controls.Add(this.btnCleaner);
            this.navPanel.Location = new System.Drawing.Point(0, 48);
            this.navPanel.Name = "navPanel";
            this.navPanel.Size = new System.Drawing.Size(180, 420);
            this.navPanel.TabIndex = 1;
            // 
            // btnCleaner
            // 
            this.btnCleaner.Location = new System.Drawing.Point(0, 0);
            this.btnCleaner.Name = "btnCleaner";
            this.btnCleaner.Size = new System.Drawing.Size(180, 44);
            this.btnCleaner.TabIndex = 0;
            this.btnCleaner.Text = "🧹  Очистка";
            this.btnCleaner.UseVisualStyleBackColor = true;
            // 
            // btnPrivacy
            // 
            this.btnPrivacy.Location = new System.Drawing.Point(0, 44);
            this.btnPrivacy.Name = "btnPrivacy";
            this.btnPrivacy.Size = new System.Drawing.Size(180, 44);
            this.btnPrivacy.TabIndex = 1;
            this.btnPrivacy.Text = "🛡  Приватность";
            this.btnPrivacy.UseVisualStyleBackColor = true;
            // 
            // btnRegistry
            // 
            this.btnRegistry.Location = new System.Drawing.Point(0, 88);
            this.btnRegistry.Name = "btnRegistry";
            this.btnRegistry.Size = new System.Drawing.Size(180, 44);
            this.btnRegistry.TabIndex = 2;
            this.btnRegistry.Text = "🧩  Реестр";
            this.btnRegistry.UseVisualStyleBackColor = true;
            // 
            // btnSettings
            // 
            this.btnSettings.Location = new System.Drawing.Point(0, 132);
            this.btnSettings.Name = "btnSettings";
            this.btnSettings.Size = new System.Drawing.Size(180, 44);
            this.btnSettings.TabIndex = 3;
            this.btnSettings.Text = "⚙  Настройки";
            this.btnSettings.UseVisualStyleBackColor = true;
            // 
            // SidebarControl
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.White;
            this.Controls.Add(this.navPanel);
            this.Controls.Add(this.lblApp);
            this.Name = "SidebarControl";
            this.Size = new System.Drawing.Size(180, 480);
            this.navPanel.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
