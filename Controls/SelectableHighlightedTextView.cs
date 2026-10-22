using Microsoft.Maui.Controls;

namespace TXTReader.Controls;

/// <summary>
/// Versión con WebView que permite selección real de texto
/// </summary>
public class SelectableHighlightedTextView : ContentView
{
    public static readonly BindableProperty TextProperty = BindableProperty.Create(
        nameof(Text), typeof(string), typeof(SelectableHighlightedTextView), string.Empty,
        propertyChanged: OnAnyChanged);

    public static readonly BindableProperty SearchTermProperty = BindableProperty.Create(
        nameof(SearchTerm), typeof(string), typeof(SelectableHighlightedTextView), string.Empty,
        propertyChanged: OnAnyChanged);

    public static readonly BindableProperty CaseSensitiveProperty = BindableProperty.Create(
        nameof(CaseSensitive), typeof(bool), typeof(SelectableHighlightedTextView), false,
        propertyChanged: OnAnyChanged);

    public static readonly BindableProperty FontSizeProperty = BindableProperty.Create(
        nameof(FontSize), typeof(double), typeof(SelectableHighlightedTextView), 14d,
        propertyChanged: OnAnyChanged);

    public static readonly BindableProperty FontFamilyProperty = BindableProperty.Create(
        nameof(FontFamily), typeof(string), typeof(SelectableHighlightedTextView), default(string),
        propertyChanged: OnAnyChanged);

    public static readonly BindableProperty ForegroundProperty = BindableProperty.Create(
        nameof(Foreground), typeof(string), typeof(SelectableHighlightedTextView), "#222",
        propertyChanged: OnAnyChanged);

    public static readonly BindableProperty HighlightTextColorProperty = BindableProperty.Create(
        nameof(HighlightTextColor), typeof(string), typeof(SelectableHighlightedTextView), "#000",
        propertyChanged: OnAnyChanged);

    public static readonly BindableProperty HighlightBackgroundColorProperty = BindableProperty.Create(
        nameof(HighlightBackgroundColor), typeof(string), typeof(SelectableHighlightedTextView), "#ffff00",
        propertyChanged: OnAnyChanged);

    public static readonly BindableProperty LineHeightProperty = BindableProperty.Create(
        nameof(LineHeight), typeof(double), typeof(SelectableHighlightedTextView), 1.4,
        propertyChanged: OnAnyChanged);

    // Zoom lógico via porcentaje de fuente
    public static readonly BindableProperty ZoomProperty = BindableProperty.Create(
        nameof(Zoom), typeof(double), typeof(SelectableHighlightedTextView), 1.0,
        propertyChanged: OnAnyChanged);

    /// <summary>Vista renderizada de Markdown (true) o el texto tal cual (false).</summary>
    public static readonly BindableProperty RenderMarkdownProperty = BindableProperty.Create(
        nameof(RenderMarkdown), typeof(bool), typeof(SelectableHighlightedTextView), false,
        propertyChanged: OnAnyChanged);

    /// <summary>Carpeta del documento, para las imagenes con ruta relativa del Markdown.</summary>
    public static readonly BindableProperty BaseDirectoryProperty = BindableProperty.Create(
        nameof(BaseDirectory), typeof(string), typeof(SelectableHighlightedTextView), default(string),
        propertyChanged: OnAnyChanged);

    /// <summary>Colores del tema para el resto de la pagina (fondo, codigo, bordes, enlaces).</summary>
    public static readonly BindableProperty PaletteProperty = BindableProperty.Create(
        nameof(Palette), typeof(TXTReader.Services.ReaderPalette), typeof(SelectableHighlightedTextView), TXTReader.Services.ReaderPalette.Default,
        propertyChanged: OnAnyChanged);

    private readonly WebView _web;

    public SelectableHighlightedTextView()
    {
        _web = new WebView
        {
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill,
            // Sin el blanco de serie mientras carga: debajo esta la tarjeta, del mismo color que la pagina.
            BackgroundColor = Colors.Transparent
        };

        _web.Navigating += OnNavigating;
        Content = _web;
        UpdateHtml();
    }

    public string Text 
    { 
        get => (string)GetValue(TextProperty); 
        set => SetValue(TextProperty, value); 
    }

    public string SearchTerm 
    { 
        get => (string)GetValue(SearchTermProperty); 
        set => SetValue(SearchTermProperty, value); 
    }

    public bool CaseSensitive 
    { 
        get => (bool)GetValue(CaseSensitiveProperty); 
        set => SetValue(CaseSensitiveProperty, value); 
    }

    public double FontSize 
    { 
        get => (double)GetValue(FontSizeProperty); 
        set => SetValue(FontSizeProperty, value); 
    }

    public string? FontFamily 
    { 
        get => (string?)GetValue(FontFamilyProperty); 
        set => SetValue(FontFamilyProperty, value); 
    }

    public string Foreground 
    { 
        get => (string)GetValue(ForegroundProperty); 
        set => SetValue(ForegroundProperty, value); 
    }

    public string HighlightTextColor 
    { 
        get => (string)GetValue(HighlightTextColorProperty); 
        set => SetValue(HighlightTextColorProperty, value); 
    }

    public string HighlightBackgroundColor 
    { 
        get => (string)GetValue(HighlightBackgroundColorProperty); 
        set => SetValue(HighlightBackgroundColorProperty, value); 
    }

    public double LineHeight 
    { 
        get => (double)GetValue(LineHeightProperty); 
        set => SetValue(LineHeightProperty, value); 
    }

    public double Zoom 
    { 
        get => (double)GetValue(ZoomProperty); 
        set => SetValue(ZoomProperty, value); 
    }

    public bool RenderMarkdown
    {
        get => (bool)GetValue(RenderMarkdownProperty);
        set => SetValue(RenderMarkdownProperty, value);
    }

    public string? BaseDirectory
    {
        get => (string?)GetValue(BaseDirectoryProperty);
        set => SetValue(BaseDirectoryProperty, value);
    }

    public TXTReader.Services.ReaderPalette Palette
    {
        get => (TXTReader.Services.ReaderPalette)GetValue(PaletteProperty);
        set => SetValue(PaletteProperty, value);
    }

    /// <summary>La pagina que se esta enseñando (para las pruebas).</summary>
    public string Html { get; private set; } = string.Empty;

    /// <summary>Se ha tocado un enlace que se abre fuera (web, correo, telefono).</summary>
    public event EventHandler<Uri>? ExternalLinkRequested;

    private void OnNavigating(object? sender, WebNavigatingEventArgs e) => e.Cancel = HandleNavigation(e.Url);

    /// <summary>
    /// La pagina no navega nunca fuera de si misma: true (cancelar) para todo lo que no sea su carga;
    /// si es un enlace externo, se avisa para abrirlo con la app que toque.
    /// </summary>
    internal bool HandleNavigation(string? url)
    {
        if (TXTReader.Services.ReaderLinks.IsViewerPage(url))
            return false;
        if (TXTReader.Services.ReaderLinks.ExternalUri(url) is { } uri)
            ExternalLinkRequested?.Invoke(this, uri);
        return true;
    }

    private static void OnAnyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is SelectableHighlightedTextView v)
            v.UpdateHtml();
    }

    private void UpdateHtml()
    {
        // Los colores de texto y resaltado vienen de sus propiedades (las pone la pagina desde los
        // tokens de Colors.xaml); el resto, de la paleta del tema.
        var palette = Palette with
        {
            Foreground = Foreground,
            HighlightBackground = HighlightBackgroundColor,
            HighlightText = HighlightTextColor
        };

        var html = TXTReader.Services.ReaderHtml.Build(new TXTReader.Services.ReaderHtmlOptions
        {
            Text = Text,
            SearchTerm = SearchTerm,
            CaseSensitive = CaseSensitive,
            FontSize = FontSize,
            Zoom = Zoom,
            LineHeight = LineHeight,
            FontFamily = FontFamily,
            Palette = palette,
            Markdown = RenderMarkdown,
            BaseDirectory = BaseDirectory
        });

        Html = html;
        _web.Source = new HtmlWebViewSource { Html = html };
    }
}
