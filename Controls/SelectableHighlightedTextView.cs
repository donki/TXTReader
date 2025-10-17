using System.Text;
using System.Text.RegularExpressions;
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

    private readonly WebView _web;

    public SelectableHighlightedTextView()
    {
        _web = new WebView
        {
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill
        };

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

    public async Task LoadFromFileAsync(string filePath, Encoding? encoding = null)
    {
        if (string.IsNullOrWhiteSpace(filePath)) 
            throw new ArgumentException("filePath");

        encoding ??= Encoding.UTF8;
        using var fs = File.OpenRead(filePath);
        using var sr = new StreamReader(fs, encoding, detectEncodingFromByteOrderMarks: true);
        Text = await sr.ReadToEndAsync();
    }

    private static void OnAnyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is SelectableHighlightedTextView v)
            v.UpdateHtml();
    }

    private void UpdateHtml()
    {
        // 1) Escapar a HTML seguro
        string encoded = System.Net.WebUtility.HtmlEncode(Text ?? string.Empty);

        // 2) Resaltar coincidencias insertando <mark>
        string term = SearchTerm ?? string.Empty;
        if (!string.IsNullOrEmpty(term))
        {
            var options = RegexOptions.Multiline | RegexOptions.CultureInvariant;
            if (!CaseSensitive) 
                options |= RegexOptions.IgnoreCase;

            string pattern = Regex.Escape(term);
            encoded = Regex.Replace(encoded, pattern, m => $"<mark>{m.Value}</mark>", options, TimeSpan.FromMilliseconds(200));
        }

        // 3) Construir HTML con estilos
        string fontFamilyCss = string.IsNullOrWhiteSpace(FontFamily) 
            ? "Consolas, Monaco, 'Courier New', monospace" 
            : FontFamily!;
        
        double basePx = Math.Clamp(FontSize * Math.Max(Zoom, 0.5), 8, 64);

        string html = $$"""
<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0, user-scalable=yes" />
    <style>
        html, body { margin:0; padding:0; }
        body {
            color: {{Foreground}};
            font-family: {{fontFamilyCss}};
            font-size: {{basePx}}px;
            line-height: {{LineHeight}};
            white-space: pre-wrap;      /* conserva saltos de línea */
            word-wrap: break-word;      /* evita desbordes */
            padding: 12px;
        }
        mark {
            background: {{HighlightBackgroundColor}};
            color: {{HighlightTextColor}};
            font-weight: 700;
        }
    </style>
</head>
<body>{{encoded}}</body>
</html>
""";

        _web.Source = new HtmlWebViewSource { Html = html };
    }
}