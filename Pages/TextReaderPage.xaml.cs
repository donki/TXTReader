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

            Title = fileName;
            UpdateTexts();
            Loading = LoadFileContentAsync(filePath);
        }

        /// <summary>Carga del fichero en curso (las pruebas la esperan).</summary>
        internal Task Loading { get; }

        private void UpdateTexts()
        {
            // No cambiar el Title ya que debe mostrar el nombre del archivo
            SearchEntry.Placeholder = _localizationService.GetString("SearchPlaceholder");
        }

        private void OnLanguageChanged(object? sender, EventArgs e)
        {
            UpdateTexts();
        }

        // Devuelve el color de un token de Colors.xaml como hex "#RRGGBB" para usarlo como
        // color CSS en el visor. Centraliza el valor (seccion 24) en lugar del literal en XAML.
        private static string ResourceHex(string key)
        {
            if (Application.Current?.Resources.TryGetValue(key, out var value) == true && value is Color color)
                return color.ToArgbHex();
            return "#000000";
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