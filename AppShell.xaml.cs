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

        /// <summary>
        /// Atras (Mobile 7): con el menu lateral abierto, lo cierra; con el lector (u otra pagina)
        /// apilado, lo desapila el Shell y vuelve a la principal; en Acerca de abierta desde el menu
        /// vuelve a Inicio; en la principal, la aplicacion se oculta sin cerrarse.
        /// </summary>
        protected override bool OnBackButtonPressed()
        {
            if (FlyoutIsPresented)
            {
                FlyoutIsPresented = false;
                return true;
            }

            if (Navigation.ModalStack.Count > 0 || Navigation.NavigationStack.Count > 1)
                return base.OnBackButtonPressed();

            if (CurrentItem != HomeFlyoutItem)
            {
                CurrentItem = HomeFlyoutItem;
                return true;
            }

#if ANDROID
            Platform.CurrentActivity?.MoveTaskToBack(true);
            return true;
#else
            return base.OnBackButtonPressed();
#endif
        }

        private void OnLanguageChanged(object? sender, EventArgs e) => UpdateMenuTexts();

        private void UpdateMenuTexts()
        {
            HomeFlyoutItem.Title = _localizationService.GetString("MenuHome");
            AboutFlyoutItem.Title = _localizationService.GetString("AboutTitle");
            VersionLabel.Text = $"v{AppInfo.Current.VersionString}";
        }
    }
}
