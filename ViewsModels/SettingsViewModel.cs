using WashinPC.Localization;

namespace WashinPC.ViewModels
{
    public class SettingsViewModel : BaseViewModel
    {
        private int _languageIndex;

        public int LanguageIndex
        {
            get => _languageIndex;
            set
            {
                if (_languageIndex == value) return;
                _languageIndex = value;

                Localizer.Current = value == 0 ? Lang.RU : Lang.EN;
                Loc.RefreshAll();


                OnPropertyChanged();
            }
        }

        public SettingsViewModel()
        {
            LanguageIndex = Localizer.Current == Lang.RU ? 0 : 1;
        }
    }
}
