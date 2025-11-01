using System.Collections.ObjectModel;
using System.ComponentModel;
using TXTReader.Services;

namespace TXTReader.Pages
{
    public partial class MainPage : ContentPage, INotifyPropertyChanged
    {
        private readonly RecentFilesService _recentFilesService = new();
        private readonly LocalizationService _localizationService = LocalizationService.Instance;
        public ObservableCollection<RecentFile> RecentFiles { get; set; } = new();
        
        public string NoRecentFilesText => _localizationService.GetString("NoRecentFiles");

        public MainPage()
        {
            try
            {
                InitializeComponent();

                _localizationService.LanguageChanged += OnLanguageChanged;
                BindingContext = this;
                UpdateTexts();
                _ = LoadRecentFiles();

                // Suscribirse a archivos abiertos por intent
                FileIntentService.FileOpened += async (filePath) =>
                {
                    try
                    {
                        _ = MobileLogService.LogAsync($"MainPage: FileOpened event received with path: {filePath}");
                        
                        // Asegurar que la navegación se ejecute en el hilo principal
                        await MainThread.InvokeOnMainThreadAsync(async () =>
                        {
                            _ = MobileLogService.LogAsync($"MainPage: About to call OpenFile with: {filePath}");
                            await OpenFile(filePath, Path.GetFileName(filePath), true);
                            _ = MobileLogService.LogAsync($"MainPage: OpenFile completed for: {filePath}");
                        });
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error opening file from intent: {ex.Message}");
                        _ = MobileLogService.LogAsync($"MainPage: ERROR in FileOpened event: {ex.Message}");
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

        private void UpdateTexts()
        {
            MainTitleLabel.Text = _localizationService.GetString("MainTitle");
            MainSubtitleLabel.Text = _localizationService.GetString("MainSubtitle");
            OpenFileLabel.Text = _localizationService.GetString("OpenFile");
            SelectFileBtn.Text = _localizationService.GetString("SelectFile");
            RecentFilesLabel.Text = _localizationService.GetString("RecentFiles");
            // NoRecentFilesLabel está en un template, se manejará con binding
            AboutBtn.Text = _localizationService.GetString("AboutTitle");
        }

        private void OnLanguageChanged(object? sender, EventArgs e)
        {
            UpdateTexts();
            OnPropertyChanged(nameof(NoRecentFilesText));
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
                    PickerTitle = _localizationService.GetString("SelectFileTitle"),
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
                await DisplayAlertAsync(_localizationService.GetString("Error"), $"{_localizationService.GetString("FileSelectError")}: {ex.Message}", _localizationService.GetString("OK"));
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
                    await DisplayAlertAsync(_localizationService.GetString("FileDeletedTitle"), _localizationService.GetString("FileDeletedMessage"), _localizationService.GetString("OK"));
                }
            }
        }

        private async Task OpenFile(string filePath, string fileName, bool isIntent = false)
        {
            try
            {
                _ = MobileLogService.LogAsync($"OpenFile: Called with filePath='{filePath}', fileName='{fileName}', isIntent={isIntent}");
                
                // Verificar que el archivo existe (solo para archivos locales)
                if (!filePath.StartsWith("content://") && !File.Exists(filePath))
                {
                    _ = MobileLogService.LogAsync($"OpenFile: Local file does not exist: {filePath}");
                    await DisplayAlertAsync(_localizationService.GetString("Error"), _localizationService.GetString("FileNotExist"), _localizationService.GetString("OK"));
                    return;
                }
                
                // Para URIs de content, no verificamos existencia local
                if (filePath.StartsWith("content://"))
                {
                    System.Diagnostics.Debug.WriteLine($"Content URI detected: {filePath}");
                    _ = MobileLogService.LogAsync($"OpenFile: Content URI detected: {filePath}");
                }

                _ = MobileLogService.LogAsync($"OpenFile: Adding to recent files");
                // Agregar a archivos recientes (siempre, incluso para intents)
                await _recentFilesService.AddRecentFileAsync(filePath, fileName);
                
                // Recargar lista de archivos recientes si no es un intent
                if (!isIntent)
                {
                    _ = MobileLogService.LogAsync($"OpenFile: Reloading recent files");
                    await LoadRecentFiles();
                }

                // Abrir el archivo en la página del lector
                System.Diagnostics.Debug.WriteLine($"Opening file: {filePath} (Intent: {isIntent})");
                _ = MobileLogService.LogAsync($"OpenFile: About to navigate to TextReaderPage");
                await Navigation.PushAsync(new TextReaderPage(filePath, fileName));
                _ = MobileLogService.LogAsync($"OpenFile: Navigation to TextReaderPage completed");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in OpenFile: {ex.Message}");
                _ = MobileLogService.LogAsync($"OpenFile: ERROR - {ex.Message}");
                _ = MobileLogService.LogAsync($"OpenFile: Stack trace - {ex.StackTrace}");
                await DisplayAlertAsync(_localizationService.GetString("Error"), $"{_localizationService.GetString("FileOpenError")}: {ex.Message}", _localizationService.GetString("OK"));
            }
        }

        private async void OnAboutClicked(object? sender, EventArgs e)
        {
            await Navigation.PushAsync(new AboutPage());
        }

        public new event PropertyChangedEventHandler? PropertyChanged;

        protected virtual new void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}