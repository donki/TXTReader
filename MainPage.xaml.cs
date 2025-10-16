using System.Collections.ObjectModel;
using TXTReader.Services;

namespace TXTReader
{
    public partial class MainPage : ContentPage
    {
        private readonly RecentFilesService _recentFilesService = new();
        public ObservableCollection<RecentFile> RecentFiles { get; set; } = new();

        public MainPage()
        {
            try
            {
                InitializeComponent();
                BindingContext = this;
                _ = LoadRecentFiles();

                // Suscribirse a archivos abiertos por intent
                FileIntentService.FileOpened += async (filePath) =>
                {
                    try
                    {
                        // Asegurar que la navegación se ejecute en el hilo principal
                        await MainThread.InvokeOnMainThreadAsync(async () =>
                        {
                            await OpenFile(filePath, Path.GetFileName(filePath), true);
                        });
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error opening file from intent: {ex.Message}");
                    }
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing MainPage: {ex.Message}");
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadRecentFiles();
        }

        private async Task LoadRecentFiles()
        {
            // Usar el método que automáticamente filtra archivos que no existen
            var validRecentFiles = await _recentFilesService.GetValidRecentFilesAsync();
            RecentFiles.Clear();
            foreach (var file in validRecentFiles)
            {
                RecentFiles.Add(file);
            }
        }

        private async void OnSelectFileClicked(object? sender, EventArgs e)
        {
            try
            {
                var customFileType = new FilePickerFileType(
                    new Dictionary<DevicePlatform, IEnumerable<string>>
                    {
                        { DevicePlatform.Android, new[] { "text/*", "text/plain", "application/json", "application/xml", "text/x-log", "*/*" } },
                        { DevicePlatform.WinUI, new[] { ".txt", ".log", ".json", ".xml", ".csv", ".md", ".ini", ".cfg", ".conf" } }
                    });

                var options = new PickOptions
                {
                    PickerTitle = "Selecciona un archivo de texto",
                    FileTypes = customFileType
                };

                var result = await FilePicker.Default.PickAsync(options);
                if (result != null)
                {
                    await OpenFile(result.FullPath, result.FileName);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Error", $"Error al seleccionar archivo: {ex.Message}", "OK");
            }
        }

        private async void OnRecentFileSelected(object? sender, TappedEventArgs e)
        {
            if (sender is Grid grid && grid.BindingContext is RecentFile recentFile)
            {
                if (File.Exists(recentFile.FilePath))
                {
                    await OpenFile(recentFile.FilePath, recentFile.FileName);
                }
                else
                {
                    // Eliminar el archivo del historial y recargar la lista
                    await _recentFilesService.RemoveRecentFileAsync(recentFile.FilePath);
                    await LoadRecentFiles();
                    await DisplayAlertAsync("Archivo eliminado", "El archivo ya no existe y ha sido eliminado del historial.", "OK");
                }
            }
        }

        private async Task OpenFile(string filePath, string fileName, bool isIntent = false)
        {
            try
            {
                // Verificar que el archivo existe
                if (!File.Exists(filePath))
                {
                    await DisplayAlertAsync("Error", "El archivo no existe o no se puede acceder.", "OK");
                    return;
                }

                // Agregar a archivos recientes (siempre, incluso para intents)
                await _recentFilesService.AddRecentFileAsync(filePath, fileName);
                
                // Recargar lista de archivos recientes si no es un intent
                if (!isIntent)
                {
                    await LoadRecentFiles();
                }

                // Abrir el archivo en la página del lector
                System.Diagnostics.Debug.WriteLine($"Opening file: {filePath} (Intent: {isIntent})");
                await Navigation.PushAsync(new TextReaderPage(filePath, fileName));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in OpenFile: {ex.Message}");
                await DisplayAlertAsync("Error", $"Error al abrir archivo: {ex.Message}", "OK");
            }
        }

        private async void OnAboutClicked(object? sender, EventArgs e)
        {
            await Navigation.PushAsync(new AboutPage());
        }
    }
}