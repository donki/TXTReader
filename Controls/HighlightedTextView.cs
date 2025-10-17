using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Maui.Controls;

namespace TXTReader.Controls;

/// <summary>
/// Control reutilizable para .NET MAUI que muestra contenido de texto y resalta
/// todas las coincidencias de una palabra/frase buscada. 100% gratuito (sin libs de pago).
/// </summary>
public class HighlightedTextView : ContentView
{
    public static readonly BindableProperty TextProperty = BindableProperty.Create(
        nameof(Text), typeof(string), typeof(HighlightedTextView), string.Empty,
        propertyChanged: OnAnyPropertyChanged);

    public static readonly BindableProperty SearchTermProperty = BindableProperty.Create(
        nameof(SearchTerm), typeof(string), typeof(HighlightedTextView), string.Empty,
        propertyChanged: OnAnyPropertyChanged);

    public static readonly BindableProperty CaseSensitiveProperty = BindableProperty.Create(
        nameof(CaseSensitive), typeof(bool), typeof(HighlightedTextView), false,
        propertyChanged: OnAnyPropertyChanged);

    public static readonly BindableProperty HighlightTextColorProperty = BindableProperty.Create(
        nameof(HighlightTextColor), typeof(Color), typeof(HighlightedTextView), Colors.Black,
        propertyChanged: OnAnyPropertyChanged);

    public static readonly BindableProperty HighlightBackgroundColorProperty = BindableProperty.Create(
        nameof(HighlightBackgroundColor), typeof(Color), typeof(HighlightedTextView), Colors.Yellow,
        propertyChanged: OnAnyPropertyChanged);

    public static readonly BindableProperty FontSizeProperty = BindableProperty.Create(
        nameof(FontSize), typeof(double), typeof(HighlightedTextView), 14d,
        propertyChanged: OnAnyPropertyChanged);

    public static readonly BindableProperty FontFamilyProperty = BindableProperty.Create(
        nameof(FontFamily), typeof(string), typeof(HighlightedTextView), default(string),
        propertyChanged: OnAnyPropertyChanged);

    // === Zoom properties ===
    public static readonly BindableProperty IsZoomEnabledProperty = BindableProperty.Create(
        nameof(IsZoomEnabled), typeof(bool), typeof(HighlightedTextView), true);

    public static readonly BindableProperty MinZoomProperty = BindableProperty.Create(
        nameof(MinZoom), typeof(double), typeof(HighlightedTextView), 1.0);

    public static readonly BindableProperty MaxZoomProperty = BindableProperty.Create(
        nameof(MaxZoom), typeof(double), typeof(HighlightedTextView), 3.0);

    public static readonly BindableProperty ZoomProperty = BindableProperty.Create(
        nameof(Zoom), typeof(double), typeof(HighlightedTextView), 1.0,
        propertyChanged: (b, o, n) => (b as HighlightedTextView)?.ApplyZoom());

    private readonly Label _label;
    private readonly Grid _zoomHost;
    private readonly ScrollView _scroll;

    public HighlightedTextView()
    {
        _label = new Label
        {
            LineBreakMode = LineBreakMode.WordWrap,
            HorizontalOptions = LayoutOptions.Start,
            VerticalOptions = LayoutOptions.Start,
            FontSize = FontSize,
            FontFamily = FontFamily
        };

        // Contenedor para aplicar escala (zoom)
        _zoomHost = new Grid
        {
            HorizontalOptions = LayoutOptions.Start,
            VerticalOptions = LayoutOptions.Start
        };
        _zoomHost.Add(_label);

        // Scroll vertical por defecto
        _scroll = new ScrollView
        {
            Orientation = ScrollOrientation.Vertical,
            Content = _zoomHost
        };

        // Gestos de zoom y doble toque para reset
        var pinch = new PinchGestureRecognizer();
        pinch.PinchUpdated += OnPinchUpdated;
        var doubleTap = new TapGestureRecognizer { NumberOfTapsRequired = 2 };
        doubleTap.Tapped += (s, e) => Zoom = 1.0; // reset

        _zoomHost.GestureRecognizers.Add(pinch);
        _zoomHost.GestureRecognizers.Add(doubleTap);

        Content = _scroll;
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

    public Color HighlightTextColor
    {
        get => (Color)GetValue(HighlightTextColorProperty);
        set => SetValue(HighlightTextColorProperty, value);
    }

    public Color HighlightBackgroundColor
    {
        get => (Color)GetValue(HighlightBackgroundColorProperty);
        set => SetValue(HighlightBackgroundColorProperty, value);
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

    public bool IsZoomEnabled
    {
        get => (bool)GetValue(IsZoomEnabledProperty);
        set => SetValue(IsZoomEnabledProperty, value);
    }

    public double MinZoom
    {
        get => (double)GetValue(MinZoomProperty);
        set => SetValue(MinZoomProperty, value);
    }

    public double MaxZoom
    {
        get => (double)GetValue(MaxZoomProperty);
        set => SetValue(MaxZoomProperty, value);
    }

    public double Zoom
    {
        get => (double)GetValue(ZoomProperty);
        set => SetValue(ZoomProperty, value);
    }

    /// <summary>
    /// Carga el contenido de un fichero de texto en el control.
    /// </summary>
    public async Task LoadFromFileAsync(string filePath, Encoding? encoding = null)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path is required", nameof(filePath));

        encoding ??= Encoding.UTF8;
        using var fs = File.OpenRead(filePath);
        using var sr = new StreamReader(fs, encoding, detectEncodingFromByteOrderMarks: true);
        Text = await sr.ReadToEndAsync();
    }

    private static void OnAnyPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is HighlightedTextView htv)
        {
            htv.ApplyFormatting();
        }
    }

    private void ApplyFormatting()
    {
        _label.FontSize = FontSize;
        _label.FontFamily = FontFamily;

        var formatted = new FormattedString();
        var text = Text ?? string.Empty;
        var term = SearchTerm ?? string.Empty;

        if (string.IsNullOrEmpty(term))
        {
            // Sin término de búsqueda: mostramos el texto tal cual
            formatted.Spans.Add(new Span { Text = text });
            _label.FormattedText = formatted;
            return;
        }

        // Preparamos regex para encontrar todas las coincidencias
        var options = RegexOptions.Multiline;
        if (!CaseSensitive)
            options |= RegexOptions.IgnoreCase;

        string escaped = Regex.Escape(term);
        var regex = new Regex(escaped, options);

        int lastIndex = 0;
        const int MAX_SPANS = 10000; // Seguridad para textos muy grandes
        int spanCount = 0;

        foreach (Match m in regex.Matches(text))
        {
            if (spanCount > MAX_SPANS)
                break; // Evitar exceso de spans en casos extremos

            if (m.Index > lastIndex)
            {
                formatted.Spans.Add(new Span { Text = text.Substring(lastIndex, m.Index - lastIndex) });
                spanCount++;
            }

            formatted.Spans.Add(new Span
            {
                Text = m.Value,
                BackgroundColor = HighlightBackgroundColor,
                TextColor = HighlightTextColor,
                FontAttributes = FontAttributes.Bold
            });
            spanCount++;

            lastIndex = m.Index + m.Length;
        }

        if (lastIndex < text.Length)
        {
            formatted.Spans.Add(new Span { Text = text.Substring(lastIndex) });
        }

        _label.FormattedText = formatted;
    }

    private void ApplyZoom()
    {
        _zoomHost.Scale = Math.Clamp(Zoom, MinZoom, MaxZoom);
    }

    private double _startScale = 1.0;

    private void OnPinchUpdated(object? sender, PinchGestureUpdatedEventArgs e)
    {
        if (!IsZoomEnabled) return;

        switch (e.Status)
        {
            case GestureStatus.Started:
                _startScale = Zoom;
                break;
            case GestureStatus.Running:
                var newScale = _startScale * e.Scale;
                Zoom = Math.Clamp(newScale, MinZoom, MaxZoom);
                break;
            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                break;
        }
    }
}