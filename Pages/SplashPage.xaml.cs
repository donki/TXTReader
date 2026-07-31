using TXTReader.Services;

namespace TXTReader.Pages
{
    public partial class SplashPage : ContentPage
    {
        private readonly LocalizationService _localizationService;

        public SplashPage()
        {
            InitializeComponent();
            _localizationService = LocalizationService.Instance;
            UpdateTexts();
            NavigateToMainPage();
        }

        private void UpdateTexts()
        {
            // Respeta el idioma configurado (antes se forzaba inglés "para pruebas"). §8 i18n.
            SubtitleLabel.Text = _localizationService.GetString("SplashSubtitle");
        }

        private async void NavigateToMainPage()
        {
            try
            {
                // Simular carga de la aplicación
                await Task.Delay(1500);
                
                // Navegar al AppShell, que provee el menu hamburguesa (constitucion, A.9).
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    if (Application.Current?.Windows.Count > 0)
                    {
                        Application.Current.Windows[0].Page = new AppShell();
                    }
                    else
                    {
                        Application.Current!.MainPage = new AppShell();
                    }
                });
            }
            catch (Exception ex)
            {
                // En caso de error, navegación más simple
                System.Diagnostics.Debug.WriteLine($"Error en navegación: {ex.Message}");
                try
                {
                    Application.Current!.MainPage = new AppShell();
                }
                catch
                {
                    // Último recurso
                    System.Diagnostics.Debug.WriteLine("Error crítico en navegación");
                }
            }
        }
    }
}