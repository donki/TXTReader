using System.Collections.ObjectModel;
using TXTReader.Services;

namespace TXTReader
{
    public partial class MainPage : ContentPage
    {
        private readonly RecentFilesService _recentFilesService;
        public ObservableCollection<RecentFile> RecentFiles { get; set; }

        public MainPage()
        {
            try
            {
                InitializeComponent();
                _recentFilesService = new RecentFilesService();
                RecentFiles = new ObservableCollection<RecentFile>();
                BindingContext = this;
                _ = LoadRecentFiles();
                
                // Suscribirse a archivos abiertos por intent
                FileIntentService.FileOpened += async (filePath) =>
                {
                    try
                    {
                        await OpenFile(filePath, Path.GetFileName(filePath));
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
            var recentFiles = await _recentFilesService.GetRecentFilesAsync();
            RecentFiles.Clear();
            foreach (var file in recentFiles)
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
                    await DisplayAlertAsync("Error", "El archivo ya no existe en la ubicación especificada.", "OK");
                }
            }
        }

        private async Task OpenFile(string filePath, string fileName)
        {
            try
            {
                // Agregar a archivos recientes
                await _recentFilesService.AddRecentFileAsync(filePath, fileName);
                
                // Recargar lista de archivos recientes
                await LoadRecentFiles();
                
                // Abrir el archivo en la página del lector
                await Navigation.PushAsync(new TextReaderPage(filePath, fileName));
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Error", $"Error al abrir archivo: {ex.Message}", "OK");
            }
        }

        private async void OnAboutClicked(object? sender, EventArgs e)
        {
            await Navigation.PushAsync(new AboutPage());
        }
    }
}