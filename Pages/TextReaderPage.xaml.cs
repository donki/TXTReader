using System.Text;
using TXTReader.Services;

namespace TXTReader.Pages
{
    public partial class TextReaderPage : ContentPage
    {
        private string _originalContent = string.Empty;
        private double _currentFontSize = 14;
        private const double MinFontSize = 8;
        private const double MaxFontSize = 32;
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
            LoadFileContent(filePath);
        }

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

        private async void LoadFileContent(string filePath)
        {
            try
            {
                var (content, encoding) = await EncodingDetectionService.ReadFileWithEncodingDetectionAsync(filePath);
                _originalContent = content;
                
                ContentViewer.Text = _originalContent;
                
                // Sincronizar el slider con el tamaño de fuente inicial
                ZoomSlider.Value = _currentFontSize;
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(_localizationService.GetString("Error"), $"{_localizationService.GetString("FileLoadError")}: {ex.Message}", _localizationService.GetString("OK"));
                await Navigation.PopAsync();
            }
        }



        private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
        {
            // El resaltado se maneja automáticamente por el binding en XAML
            // No necesitamos código adicional aquí
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