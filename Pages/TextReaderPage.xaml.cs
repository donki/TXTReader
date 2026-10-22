using System.Text;
using TXTReader.Services;

namespace TXTReader.Pages
{
    public partial class TextReaderPage : ContentPage
    {
        private double _currentFontSize = 14;
        private readonly LocalizationService _localizationService;

        public TextReaderPage(string filePath, string fileName)
        {
            InitializeComponent();
            _localizationService = LocalizationService.Instance;
            _localizationService.LanguageChanged += OnLanguageChanged;

            // Colores del resaltado desde los tokens centralizados (seccion 24). La propiedad
            // del control es string (color CSS), por eso se convierte el token a hex aqui.
            ContentViewer.HighlightBackgroundColor = ResourceHex("HighlightBackground");
            ContentViewer.HighlightTextColor = ResourceHex("Black");
            ApplyTheme();
            ContentViewer.ExternalLinkRequested += OnExternalLinkRequested;

            // Los .md se abren renderizados; cualquier otro, tal cual (el boton alterna en los dos).
            ContentViewer.BaseDirectory = BaseDirectoryOf(filePath);
            ContentViewer.RenderMarkdown = MarkdownRenderer.IsMarkdownFile(fileName) || MarkdownRenderer.IsMarkdownFile(filePath);

            Title = fileName;
            UpdateTexts();
            Loading = LoadFileContentAsync(filePath);
        }

        /// <summary>Carga del fichero en curso (las pruebas la esperan).</summary>
        internal Task Loading { get; }

        /// <summary>¿Se ve el Markdown renderizado?</summary>
        internal bool IsRendered => ContentViewer.RenderMarkdown;

        private void UpdateTexts()
        {
            // No cambiar el Title ya que debe mostrar el nombre del archivo
            SearchEntry.Placeholder = _localizationService.GetString("SearchPlaceholder");
            UpdateToggle();
        }

        /// <summary>
        /// El boton enseña lo que hara al tocarlo: ver el texto tal cual o verlo renderizado. El texto
        /// es su nombre en el idioma de la app.
        /// </summary>
        private void UpdateToggle()
        {
            var rendered = ContentViewer.RenderMarkdown;
            MarkdownToggle.IconImageSource = rendered ? "ic_view_source.png" : "ic_view_rendered.png";
            MarkdownToggle.Text = _localizationService.GetString(rendered ? "ShowSourceText" : "ShowRenderedMarkdown");
        }

        internal void ToggleMarkdown()
        {
            ContentViewer.RenderMarkdown = !ContentViewer.RenderMarkdown;
            UpdateToggle();
        }

        private void OnMarkdownToggleClicked(object? sender, EventArgs e) => ToggleMarkdown();

        /// <summary>Carpeta de un fichero local; null para las URIs content:// (no hay carpeta).</summary>
        internal static string? BaseDirectoryOf(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || IntentFileHandler.IsContentUri(filePath))
                return null;
            try
            {
                var directory = Path.GetDirectoryName(filePath);
                return string.IsNullOrEmpty(directory) ? null : directory;
            }
            catch (Exception)
            {
                return null;
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            ApplyTheme();
            if (Application.Current is { } app)
                app.RequestedThemeChanged += OnRequestedThemeChanged;
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            if (Application.Current is { } app)
                app.RequestedThemeChanged -= OnRequestedThemeChanged;
        }

        private void OnRequestedThemeChanged(object? sender, AppThemeChangedEventArgs e) => ApplyTheme();

        /// <summary>Colores del visor para el tema que se ve (claro u oscuro), desde Colors.xaml.</summary>
        internal void ApplyTheme()
        {
            var dark = Application.Current?.RequestedTheme == AppTheme.Dark;
            var suffix = dark ? "Dark" : "Light";
            var fallback = ReaderPalette.Default;
            ContentViewer.Foreground = ResourceHex("TextPrimary" + suffix, fallback.Foreground);
            ContentViewer.Palette = new ReaderPalette(
                Foreground: ContentViewer.Foreground,
                Background: ResourceHex("CardBackground" + suffix, fallback.Background),
                Secondary: ResourceHex("TextSecondary" + suffix, fallback.Secondary),
                Border: ResourceHex("Separator" + suffix, fallback.Border),
                CodeBackground: ResourceHex("CodeBackground" + suffix, fallback.CodeBackground),
                Link: ResourceHex("Link" + suffix, fallback.Link),
                HighlightBackground: ContentViewer.HighlightBackgroundColor,
                HighlightText: ContentViewer.HighlightTextColor);
        }

        /// <summary>Enlace del documento: se abre con la app que toque (navegador, correo...).</summary>
        private async void OnExternalLinkRequested(object? sender, Uri uri) => await OpenLinkAsync(uri);

        internal async Task OpenLinkAsync(Uri uri)
        {
            try
            {
                await AppPlatform.Launcher.OpenAsync(uri);
            }
            catch (Exception ex)
            {
                _ = MobileLogService.LogAsync($"OpenLink: ERROR - {ex.Message}");
                await AppPlatform.Alert(this, _localizationService.GetString("LinkErrorTitle"), _localizationService.GetString("LinkErrorMessage"), _localizationService.GetString("OK"), null);
            }
        }

        private void OnLanguageChanged(object? sender, EventArgs e)
        {
            UpdateTexts();
        }

        // Devuelve el color de un token de Colors.xaml como hex "#RRGGBB" para usarlo como
        // color CSS en el visor. Centraliza el valor (seccion 24) en lugar del literal en XAML.
        private static string ResourceHex(string key, string fallback = "#000000")
        {
            if (Application.Current?.Resources.TryGetValue(key, out var value) == true && value is Color color)
                return color.ToArgbHex();
            return fallback;
        }

        private async Task LoadFileContentAsync(string filePath)
        {
            try
            {
                var (content, _) = await EncodingDetectionService.ReadFileWithEncodingDetectionAsync(filePath);
                ContentViewer.Text = content;
                
                // Sincronizar el slider con el tamaño de fuente inicial
                ZoomSlider.Value = _currentFontSize;
            }
            catch (Exception ex)
            {
                await AppPlatform.Alert(this, _localizationService.GetString("Error"), $"{_localizationService.GetString("FileLoadError")}: {ex.Message}", _localizationService.GetString("OK"), null);
                await Navigation.PopAsync();
            }
        }

        private void OnZoomSliderValueChanged(object? sender, ValueChangedEventArgs e)
        {
            if (sender is Slider slider)
            {
                _currentFontSize = slider.Value;
                // Calcular el zoom como factor de la fuente base (14px)
                ContentViewer.Zoom = _currentFontSize / 14.0;
            }
        }
    }
}