using System;
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
            get => _isChecked;
            set
            {
                if (_isChecked == value) return;
                _isChecked = value;
                OnPropertyChanged();
            }
        }

        // 🔹 ЛОКАЛИЗОВАННОЕ НАЗВАНИЕ КАТЕГОРИИ
        public string Title => Localizer.T(CategoryKey);

        // 🔹 ЛОКАЛИЗОВАННЫЙ ЗАГОЛОВОК ГРУППЫ
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

        public bool IsDangerous => Risk == RegistryRiskLevel.Dangerous;

        public RegistryCategoryView(
            string categoryKey,
            RegistryRiskLevel risk,
            bool enabled)
        {
            CategoryKey = categoryKey;
            Risk = risk;
            _isChecked = enabled;

            Localizer.LanguageChanged += () =>
            {
                OnPropertyChanged(nameof(Title));
                OnPropertyChanged(nameof(RiskTitle));
            };
        }
    }
}
