using System;
using System.Windows.Input;
using WashinPC.Core.Mvvm;
using WashinPC.Localization;

namespace WashinPC.ViewModels
{
    public sealed class RegistryCategoryView : BaseViewModel
    {
        public string CategoryKey { get; }
        public RegistryRiskLevel Risk { get; }

        private bool _isChecked;

        public bool IsChecked
        {
            get { return _isChecked; }
            set
            {
                if (_isChecked == value) return;
                _isChecked = value;
                OnPropertyChanged();
            }
        }

        public string Title
        {
            get { return Localizer.T(CategoryKey); }
        }

        public string RiskTitle
        {
            get
            {
                if (Risk == RegistryRiskLevel.Safe)
                    return Localizer.T("registry.risk.safe");
                if (Risk == RegistryRiskLevel.Advanced)
                    return Localizer.T("registry.risk.advanced");
                if (Risk == RegistryRiskLevel.Dangerous)
                    return Localizer.T("registry.risk.dangerous");

                return Risk.ToString();
            }
        }

        public bool IsDangerous
        {
            get { return Risk == RegistryRiskLevel.Dangerous; }
        }

        // 🔹 КОМАНДА ДЛЯ ?
        public ICommand InfoCommand { get; }

        private readonly Action<string> _showInfo;

        public RegistryCategoryView(
            string categoryKey,
            RegistryRiskLevel risk,
            bool enabled,
            Action<string> showInfo)
        {
            CategoryKey = categoryKey;
            Risk = risk;
            _isChecked = enabled;
            _showInfo = showInfo;

            InfoCommand = new RelayCommand(ShowInfo);

            Localizer.LanguageChanged += OnLanguageChanged;
        }

        private void ShowInfo()
        {
            if (_showInfo != null)
                _showInfo(CategoryKey);
        }

        private void OnLanguageChanged()
        {
            OnPropertyChanged(nameof(Title));
            OnPropertyChanged(nameof(RiskTitle));
        }
    }
}
