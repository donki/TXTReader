namespace TXTReader
{
    public partial class SplashPage : ContentPage
    {
        public SplashPage()
        {
            InitializeComponent();
            NavigateToMainPage();
        }

        private async void NavigateToMainPage()
        {
            try
            {
                // Simular carga de la aplicación
                await Task.Delay(2000);
                
                // Navegar a la página principal de forma segura
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    if (Application.Current?.Windows.Count > 0)
                    {
                        Application.Current.Windows[0].Page = new AppShell();
                    }
                });
            }
            catch (Exception ex)
            {
                // En caso de error, intentar navegación alternativa
                System.Diagnostics.Debug.WriteLine($"Error en navegación: {ex.Message}");
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Application.Current!.MainPage = new AppShell();
                });
            }
        }
    }
}