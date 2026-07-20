using TXTReader.Services;

namespace TXTReader
{
    public partial class AppShell : Shell
    {
        private readonly LocalizationService _localizationService = LocalizationService.Instance;

        public AppShell()
        {
            InitializeComponent();

            _localizationService.LanguageChanged += OnLanguageChanged;
            UpdateMenuTexts();
        }

        private void OnLanguageChanged(object? sender, EventArgs e) => UpdateMenuTexts();

        private void UpdateMenuTexts()
        {
            HomeFlyoutItem.Title = _localizationService.GetString("MenuHome");
            AboutFlyoutItem.Title = _localizationService.GetString("AboutTitle");
        }
    }
}
