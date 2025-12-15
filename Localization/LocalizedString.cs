using System.ComponentModel;
using WashinPC.Localization;

namespace WashinPC.Core
{
public class LocalizedString : INotifyPropertyChanged
{
    private string _value = string.Empty;

    public string Value
    {
        get => _value;
        private set
        {
            _value = value ?? string.Empty;
            PropertyChanged?.Invoke(this,
                new PropertyChangedEventArgs(nameof(Value)));
        }
    }

    public string Key { get; }

    public LocalizedString(string key)
    {
        Key = key;
        Refresh(); // важно
    }

    public void Refresh()
    {
        Value = Localizer.T(Key);
    }

    public event PropertyChangedEventHandler PropertyChanged;
}


}
