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
                var primary = Application.Current?.TryFindResource("Brush.Primary") as Brush;
                var warning = Application.Current?.TryFindResource("Brush.Warning") as Brush;
                var danger = Application.Current?.TryFindResource("Brush.Destructive") as Brush;

                switch (Type)
                {
                    case DialogType.Info:
                        return primary ?? new SolidColorBrush(Color.FromRgb(0, 123, 255));

                    case DialogType.Confirm:
                        return primary ?? new SolidColorBrush(Color.FromRgb(0, 123, 255));

                    case DialogType.Warning:
                        return warning ?? new SolidColorBrush(Color.FromRgb(245, 176, 51));

                    case DialogType.Error:
                        return danger ?? new SolidColorBrush(Color.FromRgb(227, 90, 90));

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
