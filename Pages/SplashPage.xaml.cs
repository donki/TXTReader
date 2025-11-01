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
            // Force English for testing
            var culture = new System.Globalization.CultureInfo("en");
            var resourceManager = new System.Resources.ResourceManager(typeof(TXTReader.Resources.Strings.AppResources));
            SubtitleLabel.Text = resourceManager.GetString("SplashSubtitle", culture) ?? "Text file reader";
            
            System.Diagnostics.Debug.WriteLine($"SplashPage: Setting subtitle to: {SubtitleLabel.Text}");
        }

        private async void NavigateToMainPage()
        {
            try
            {
                // Simular carga de la aplicación
                await Task.Delay(1500);
                
                // Navegar directamente a MainPage para evitar problemas con AppShell
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    if (Application.Current?.Windows.Count > 0)
                    {
                        Application.Current.Windows[0].Page = new NavigationPage(new MainPage());
                    }
                    else
                    {
                        Application.Current!.MainPage = new NavigationPage(new MainPage());
                    }
                });
            }
            catch (Exception ex)
            {
                // En caso de error, navegación más simple
                System.Diagnostics.Debug.WriteLine($"Error en navegación: {ex.Message}");
                try
                {
                    Application.Current!.MainPage = new MainPage();
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