using System;
using System.IO;
using System.Windows.Forms;


namespace WashinPC
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            try
            {
                AppBootstrap.Initialize();

                if (!Utils.IsAdministrator())
                {
                    MessageBox.Show(
                        Localizer.T("admin.required"),
                        "WashinPC",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new Forms.MainForm());
            }
            catch (Exception ex)
            {
                try
                {
                    File.AppendAllText(
                        AppBootstrap.LogFile,
                        $"FATAL: {DateTime.Now:u} {ex}\r\n"
                    );
                }
                catch { }

                MessageBox.Show(
                    Localizer.T("app.error") + "\r\n" + ex.Message,
                    "WashinPC",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }
    }
}
