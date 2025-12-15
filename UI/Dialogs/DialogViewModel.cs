using System.Windows.Input;

namespace WashinPC.UI.Dialogs
{
    public class DialogViewModel
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

        public DialogViewModel(string title, string message, DialogType type, System.Action close)
        {
            Title = title;
            Message = message;
            Type = type;

            YesCommand = new RelayCommand(() => { Result = DialogResult.Yes; close(); });
            NoCommand = new RelayCommand(() => { Result = DialogResult.No; close(); });
            OkCommand = new RelayCommand(() => { Result = DialogResult.Ok; close(); });
        }
    }
}
