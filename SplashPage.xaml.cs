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