using WashinPC.Core;
using WashinPC.Localization;

namespace WashinPC.ViewModels
{
    public class SettingsViewModel : BaseViewModel
    {
        private int _languageIndex;
        private bool _registryExpertMode;

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

        public bool RegistryExpertMode
        {
            get => _registryExpertMode;
            set
            {
                if (_registryExpertMode == value) return;
                _registryExpertMode = value;
                AppSettings.RegistryExpertMode = value;
                OnPropertyChanged();
            }
        }

        public SettingsViewModel()
        {
            LanguageIndex = Localizer.Current == Lang.RU ? 0 : 1;
            RegistryExpertMode = AppSettings.RegistryExpertMode;
        }
    }
}
