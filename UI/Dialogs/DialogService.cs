using System.Windows;

namespace WashinPC.UI.Dialogs
{
    public static class DialogService
    {
        public static DialogResult ShowConfirm(string message, string title = "WashinPC")
            => Show(title, message, DialogType.Confirm);

        public static DialogResult ShowInfo(string message, string title = "WashinPC")
            => Show(title, message, DialogType.Info);

        public static DialogResult ShowWarning(string message, string title = "WashinPC")
            => Show(title, message, DialogType.Warning);

        public static DialogResult ShowError(string message, string title = "WashinPC")
            => Show(title, message, DialogType.Error);

        private static DialogResult Show(string title, string message, DialogType type)
        {
            var dlg = new AppDialog(title, message, type)
            {
                Owner = Application.Current?.MainWindow
            };
            dlg.ShowDialog();
            return dlg.Result;
        }
    }
}
