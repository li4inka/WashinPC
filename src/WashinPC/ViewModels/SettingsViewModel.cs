using System.Collections.ObjectModel;
using System.Linq;
using WashinPC.Core;
using WashinPC.Core.Mvvm;
using WashinPC.Localization;
using WashinPC.Services;

namespace WashinPC.ViewModels
{
    public sealed class LanguageOption : BaseViewModel
    {
        private string _displayName;

        public Lang Value { get; }

        public string DisplayName
        {
            get => _displayName;
            set
            {
                if (_displayName == value)
                    return;

                _displayName = value;
                OnPropertyChanged();
            }
        }

        public LanguageOption(Lang value)
        {
            Value = value;
            _displayName = string.Empty;
        }

        public override string ToString()
        {
            return DisplayName;
        }
    }

    public class SettingsViewModel : BaseViewModel
    {
        private bool _registryExpertMode;
        private string _themeMode;
        private LanguageOption _selectedLanguage;

        public ObservableCollection<LanguageOption> LanguageOptions { get; } =
            new ObservableCollection<LanguageOption>
            {
                new LanguageOption(Lang.EN),
                new LanguageOption(Lang.RU)
            };

        public LanguageOption SelectedLanguage
        {
            get => _selectedLanguage;
            set
            {
                if (value == null || ReferenceEquals(_selectedLanguage, value))
                    return;

                _selectedLanguage = value;
                if (Localizer.Current != value.Value)
                {
                    Localizer.Current = value.Value;
                    RefreshLanguageOptions();
                }

                OnPropertyChanged();
                OnPropertyChanged(nameof(ThemeStatusText));
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

        public string ThemeMode
        {
            get => _themeMode;
            private set
            {
                if (_themeMode == value) return;
                _themeMode = value;
                AppSettings.ThemeMode = value;
                ThemeService.Apply(value);

                OnPropertyChanged();
                OnPropertyChanged(nameof(IsThemeAuto));
                OnPropertyChanged(nameof(IsThemeLight));
                OnPropertyChanged(nameof(IsThemeDark));
                OnPropertyChanged(nameof(ThemeStatusText));
            }
        }

        public bool IsThemeAuto => ThemeMode == ThemeService.ModeAuto;
        public bool IsThemeLight => ThemeMode == ThemeService.ModeLight;
        public bool IsThemeDark => ThemeMode == ThemeService.ModeDark;

        public string ThemeStatusText => IsThemeAuto ? Localizer.T("settings.theme.auto.status") : Localizer.T("settings.theme.using", Localizer.T("settings.theme." + ThemeMode));

        public RelayCommand SetThemeAutoCommand { get; }
        public RelayCommand SetThemeLightCommand { get; }
        public RelayCommand SetThemeDarkCommand { get; }

        public SettingsViewModel()
        {
            SetThemeAutoCommand = new RelayCommand(() => ThemeMode = ThemeService.ModeAuto);
            SetThemeLightCommand = new RelayCommand(() => ThemeMode = ThemeService.ModeLight);
            SetThemeDarkCommand = new RelayCommand(() => ThemeMode = ThemeService.ModeDark);

            RefreshLanguageOptions();
            _selectedLanguage = LanguageOptions.First(x => x.Value == Localizer.Current);
            RegistryExpertMode = AppSettings.RegistryExpertMode;

            var initialThemeMode = AppSettings.ThemeMode;
            if (!ThemeService.IsKnownMode(initialThemeMode))
                initialThemeMode = ThemeService.ModeAuto;

            ThemeService.Apply(initialThemeMode);
            _themeMode = initialThemeMode;
        }

        private void RefreshLanguageOptions()
        {
            foreach (var option in LanguageOptions)
            {
                option.DisplayName = option.Value == Lang.RU
                    ? Loc.SettingsLangRussian.Value
                    : Loc.SettingsLangEnglish.Value;
            }
        }
    }
}
