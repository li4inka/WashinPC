namespace WashinPC.Forms
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        private WashinPC.Controls.SidebarControl sidebar;
        private System.Windows.Forms.Panel contentHost;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.sidebar = new WashinPC.Controls.SidebarControl();
            this.contentHost = new System.Windows.Forms.Panel();
            this.SuspendLayout();
            // 
            // sidebar
            // 
            this.sidebar.Dock = System.Windows.Forms.DockStyle.Left;
            this.sidebar.Location = new System.Drawing.Point(0, 0);
            this.sidebar.Name = "sidebar";
            this.sidebar.Size = new System.Drawing.Size(180, 640);
            this.sidebar.TabIndex = 0;
            // 
            // contentHost
            // 
            this.contentHost.BackColor = System.Drawing.SystemColors.Control;
            this.contentHost.Dock = System.Windows.Forms.DockStyle.Fill;
            this.contentHost.Location = new System.Drawing.Point(180, 0);
            this.contentHost.Name = "contentHost";
            this.contentHost.Padding = new System.Windows.Forms.Padding(20);
            this.contentHost.Size = new System.Drawing.Size(844, 640);
            this.contentHost.TabIndex = 1;
            // 
            // MainForm
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(1024, 640);
            this.Controls.Add(this.contentHost);
            this.Controls.Add(this.sidebar);
            this.MinimumSize = new System.Drawing.Size(960, 560);
            this.Name = "MainForm";
            this.Text = "WashinPC";
            this.ResumeLayout(false);
        }
    }
}
