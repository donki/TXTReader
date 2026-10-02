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

        /// <summary>Comprobacion de version (en la app la da el contenedor de servicios de MAUI).</summary>
        internal Func<UpdateService?> UpdateServiceProvider { get; set; }

        public MainPage()
        {
            InitializeComponent();
            UpdateServiceProvider = () => (Handler?.MauiContext?.Services ?? IPlatformApplication.Current?.Services)?.GetService<UpdateService>();

            _localizationService.LanguageChanged += OnLanguageChanged;
            BindingContext = this;
            UpdateTexts();
            _ = LoadRecentFiles();

            // Archivos abiertos desde otra app (intent): MainActivity -> IntentFileHandler -> aqui.
            FileIntentService.FileOpened += OnFileOpenedFromIntent;
        }

        internal async void OnFileOpenedFromIntent(string filePath)
        {
            try
            {
                _ = MobileLogService.LogAsync($"MainPage: FileOpened event received with path: {filePath}");

                // Asegurar que la navegación se ejecute en el hilo principal
                await AppPlatform.RunOnMainThreadAsync(() => OpenFile(filePath, Path.GetFileName(filePath), true));
            }
            catch (Exception ex)
            {
                _ = MobileLogService.LogAsync($"MainPage: ERROR in FileOpened event: {ex.Message}");
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _ = OnAppearingAsync();
        }

        internal async Task OnAppearingAsync()
        {
            await LoadRecentFiles();

            // Comprobacion de version al arrancar (constitucion, seccion 15): no bloqueante y
            // silenciosa si ya se esta al dia o no hay red.
            var updateService = UpdateServiceProvider();
            if (updateService != null)
                _ = updateService.CheckAndPromptAsync(this);
        }

        private void UpdateTexts()
        {
            // El titulo de la barra de navegacion ya identifica la app: la cabecera con
            // "TXT Reader" / "Lector de archivos de texto" y el rotulo "Abrir archivo" se
            // quitaron por repetir lo mismo tres veces (nota de autor del 2026-08-01).
            Title = _localizationService.GetString("AppName");
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

        internal async Task LoadRecentFiles()
        {
            // Usar el método que automáticamente filtra archivos que no existen
            var validRecentFiles = await _recentFilesService.GetValidRecentFilesAsync();
            RecentFiles.Clear();
            foreach (var file in validRecentFiles)
            {
                RecentFiles.Add(file);
            }
        }

        private async void OnSelectFileClicked(object? sender, EventArgs e) => await SelectFileAsync();

        internal async Task SelectFileAsync()
        {
            try
            {
                var options = new PickOptions
                {
                    PickerTitle = _localizationService.GetString("SelectFileTitle"),
                    FileTypes = PickedFiles.FileTypes()
                };

                var result = await AppPlatform.FilePicker.PickAsync(options);
                if (result != null)
                {
                    var filePath = await PickedFiles.GetReadablePathAsync(result.FullPath, result.FileName, result.OpenReadAsync, AppPlatform.CacheDirectory());
                    await OpenFile(filePath, result.FileName);
                }
            }
            catch (Exception ex)
            {
                await AlertAsync(_localizationService.GetString("Error"), $"{_localizationService.GetString("FileSelectError")}: {ex.Message}");
            }
        }

        private async void OnRecentFileSelected(object? sender, TappedEventArgs e) => await OpenRecentAsync((sender as BindableObject)?.BindingContext as RecentFile);

        internal async Task OpenRecentAsync(RecentFile? recentFile)
        {
            if (recentFile is null)
                return;

            if (File.Exists(recentFile.FilePath))
            {
                await OpenFile(recentFile.FilePath, recentFile.FileName);
            }
            else
            {
                // Eliminar el archivo del historial y recargar la lista
                await _recentFilesService.RemoveRecentFileAsync(recentFile.FilePath);
                await LoadRecentFiles();
                await AlertAsync(_localizationService.GetString("FileDeletedTitle"), _localizationService.GetString("FileDeletedMessage"));
            }
        }

        internal async Task OpenFile(string filePath, string fileName, bool isIntent = false)
        {
            try
            {
                _ = MobileLogService.LogAsync($"OpenFile: Called with filePath='{filePath}', fileName='{fileName}', isIntent={isIntent}");

                // Verificar que el archivo existe (solo para archivos locales; las URIs content://
                // se leen con el ContentResolver)
                if (!IntentFileHandler.IsContentUri(filePath) && !File.Exists(filePath))
                {
                    _ = MobileLogService.LogAsync($"OpenFile: Local file does not exist: {filePath}");
                    await AlertAsync(_localizationService.GetString("Error"), _localizationService.GetString("FileNotExist"));
                    return;
                }

                // Agregar a archivos recientes (siempre, incluso para intents)
                await _recentFilesService.AddRecentFileAsync(filePath, fileName);

                // Recargar lista de archivos recientes si no es un intent
                if (!isIntent)
                    await LoadRecentFiles();

                // Abrir el archivo en la página del lector
                await Navigation.PushAsync(new TextReaderPage(filePath, fileName));
                _ = MobileLogService.LogAsync($"OpenFile: Navigation to TextReaderPage completed");
            }
            catch (Exception ex)
            {
                _ = MobileLogService.LogAsync($"OpenFile: ERROR - {ex.Message}\n{ex.StackTrace}");
                await AlertAsync(_localizationService.GetString("Error"), $"{_localizationService.GetString("FileOpenError")}: {ex.Message}");
            }
        }

        private async void OnAboutClicked(object? sender, EventArgs e) => await Navigation.PushAsync(new AboutPage());

        private Task<bool> AlertAsync(string title, string message) =>
            AppPlatform.Alert(this, title, message, _localizationService.GetString("OK"), null);

        public new event PropertyChangedEventHandler? PropertyChanged;

        protected virtual new void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
