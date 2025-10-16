using System.Text;
using TXTReader.Services;

namespace TXTReader
{
    public partial class TextReaderPage : ContentPage
    {
        private string _originalContent = string.Empty;
        private string _currentDisplayContent = string.Empty;
        private double _currentFontSize = 14;
        private const double MinFontSize = 8;
        private const double MaxFontSize = 32;
        private string _detectedEncoding = string.Empty;
        private bool _isUpdatingContent = false;

        public TextReaderPage(string filePath, string fileName)
        {
            InitializeComponent();
            Title = fileName;
            LoadFileContent(filePath);
        }

        private async void LoadFileContent(string filePath)
        {
            try
            {
                var (content, encoding) = await EncodingDetectionService.ReadFileWithEncodingDetectionAsync(filePath);
                _originalContent = content;
                _currentDisplayContent = content;
                _detectedEncoding = encoding;
                
                _isUpdatingContent = true;
                ContentEditor.Text = _originalContent;
                _isUpdatingContent = false;
                
                // Mostrar información de codificación en el título
                Title = $"{Path.GetFileName(filePath)} ({encoding})";
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Error", $"No se pudo cargar el archivo: {ex.Message}", "OK");
                await Navigation.PopAsync();
            }
        }

        private async void OnCloseClicked(object? sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(e.NewTextValue))
            {
                _currentDisplayContent = _originalContent;
                _isUpdatingContent = true;
                ContentEditor.Text = _currentDisplayContent;
                _isUpdatingContent = false;
                return;
            }

            // Resaltar texto encontrado (simulado con mayúsculas por simplicidad)
            var searchText = e.NewTextValue.ToLower();
            var content = _originalContent;
            
            // Buscar y resaltar coincidencias
            var highlightedContent = HighlightSearchText(content, searchText);
            _currentDisplayContent = highlightedContent;
            _isUpdatingContent = true;
            ContentEditor.Text = _currentDisplayContent;
            _isUpdatingContent = false;
        }

        private string HighlightSearchText(string content, string searchText)
        {
            if (string.IsNullOrEmpty(searchText))
                return content;

            // Por simplicidad, convertimos las coincidencias a mayúsculas
            // En una implementación más avanzada, usarías FormattedString
            var result = new StringBuilder();
            var contentLower = content.ToLower();
            var searchLower = searchText.ToLower();
            
            int lastIndex = 0;
            int index = contentLower.IndexOf(searchLower);
            
            while (index != -1)
            {
                result.Append(content.Substring(lastIndex, index - lastIndex));
                result.Append(content.Substring(index, searchText.Length).ToUpper());
                lastIndex = index + searchText.Length;
                index = contentLower.IndexOf(searchLower, lastIndex);
            }
            
            result.Append(content.Substring(lastIndex));
            return result.ToString();
        }

        private void OnZoomInClicked(object? sender, EventArgs e)
        {
            if (_currentFontSize < MaxFontSize)
            {
                _currentFontSize += 2;
                ContentEditor.FontSize = _currentFontSize;
                UpdateFontSizeLabel();
            }
        }

        private void OnZoomOutClicked(object? sender, EventArgs e)
        {
            if (_currentFontSize > MinFontSize)
            {
                _currentFontSize -= 2;
                ContentEditor.FontSize = _currentFontSize;
                UpdateFontSizeLabel();
            }
        }

        private void UpdateFontSizeLabel()
        {
            FontSizeLabel.Text = $"{_currentFontSize:F0}px";
        }

        private void OnContentEditorTextChanged(object? sender, TextChangedEventArgs e)
        {
            // Si estamos actualizando el contenido programáticamente, no hacer nada
            if (_isUpdatingContent)
                return;

            // Si el usuario intentó modificar el texto, restaurar el contenido original
            if (e.NewTextValue != _currentDisplayContent)
            {
                _isUpdatingContent = true;
                ContentEditor.Text = _currentDisplayContent;
                _isUpdatingContent = false;
            }
        }
    }
}