using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using WashinPC.Core.Mvvm;
using WashinPC.UI.Dialogs;

namespace WashinPC.Services
{
    // ===================== ENUMS =====================

    public enum DialogType
    {
        Info,
        Warning,
        Error,
        Confirm
    }

    public enum DialogResult
    {
        None,
        Yes,
        No,
        Ok
    }

    // ===================== SERVICE =====================

    public static class DialogService
    {
        public static DialogResult ShowInfo(string message, string title = "WashinPC")
            => Show(title, message, DialogType.Info);

        public static DialogResult ShowWarning(string message, string title = "WashinPC")
            => Show(title, message, DialogType.Warning);

        public static DialogResult ShowError(string message, string title = "WashinPC")
            => Show(title, message, DialogType.Error);

        public static DialogResult ShowConfirm(string message, string title = "WashinPC")
            => Show(title, message, DialogType.Confirm);

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

    // ===================== VIEW MODEL =====================

    internal sealed class DialogViewModel
    {
        public string Title { get; }
        public string Message { get; }
        public DialogType Type { get; }

        public bool ShowYesNo => Type == DialogType.Confirm;
        public bool ShowOk => Type != DialogType.Confirm;

        public string IconText =>
            Type == DialogType.Info ? "ℹ" :
            Type == DialogType.Warning ? "⚠" :
            Type == DialogType.Error ? "⛔" : "❓";

        public DialogResult Result { get; private set; } = DialogResult.None;

        public ICommand YesCommand { get; }
        public ICommand NoCommand { get; }
        public ICommand OkCommand { get; }
        public Brush AccentBrush
        {
            get
            {
                switch (Type)
                {
                    case DialogType.Info:
                        return new SolidColorBrush(Color.FromRgb(58, 122, 254));

                    case DialogType.Confirm:
                        return new SolidColorBrush(Color.FromRgb(10, 91, 211));

                    case DialogType.Warning:
                        return new SolidColorBrush(Color.FromRgb(230, 167, 0));

                    case DialogType.Error:
                        return new SolidColorBrush(Color.FromRgb(209, 52, 56));

                    default:
                        return Brushes.Transparent;
                }
            }
        }

        public DialogViewModel(
            string title,
            string message,
            DialogType type,
            Action close)
        {
            Title = title;
            Message = message;
            Type = type;

            YesCommand = new RelayCommand(() =>
            {
                Result = DialogResult.Yes;
                close();
            });

            NoCommand = new RelayCommand(() =>
            {
                Result = DialogResult.No;
                close();
            });

            OkCommand = new RelayCommand(() =>
            {
                Result = DialogResult.Ok;
                close();
            });
        }

    }
}
