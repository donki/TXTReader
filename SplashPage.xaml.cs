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
            // Simular carga de la aplicación
            await Task.Delay(2000);
            
            // Navegar a la página principal usando la ventana actual
            if (Application.Current?.Windows.Count > 0)
            {
                Application.Current.Windows[0].Page = new AppShell();
            }
        }
    }
}