using System.Globalization;
using System.Text.Json;

namespace TXTReader.Services;

/// <summary>Colores del visor (CSS) para el tema que se ve, sacados de los tokens de Colors.xaml.</summary>
public sealed record ReaderPalette(
    string Foreground,
    string Background,
    string Secondary,
    string Border,
    string CodeBackground,
    string Link,
    string HighlightBackground,
    string HighlightText)
{
    /// <summary>Paleta clara por defecto (la de Colors.xaml), para cuando no hay recursos de la app.</summary>
    public static ReaderPalette Default { get; } =
        new("#191C1D", "#FFFFFF", "#464555", "#C7C4D8", "#F1F0F7", "#3525CD", "#FFE08A", "#000000");
}

/// <summary>Lo que pinta el visor: el texto, la busqueda, la letra y el modo.</summary>
public sealed record ReaderHtmlOptions
{
    public string? Text { get; init; }
    public string? SearchTerm { get; init; }
    public bool CaseSensitive { get; init; }
    public double FontSize { get; init; } = 14;
    public double Zoom { get; init; } = 1;
    public double LineHeight { get; init; } = 1.4;
    public string? FontFamily { get; init; }
    public ReaderPalette Palette { get; init; } = ReaderPalette.Default;

    /// <summary>Vista renderizada de Markdown en vez del texto tal cual.</summary>
    public bool Markdown { get; init; }

    /// <summary>Carpeta del documento (imagenes relativas del Markdown).</summary>
    public string? BaseDirectory { get; init; }
}

/// <summary>
/// La pagina HTML del visor. En el texto tal cual, las coincidencias se marcan aqui
/// (<see cref="TextHighlighter"/>); en Markdown las marca un script de la pagina sobre el texto ya
/// pintado, para no partir las etiquetas. Una politica de contenido (CSP) impide pedir nada a la red
/// y ejecutar otros scripts que el propio (el HTML que traiga el documento no puede hacer nada).
/// </summary>
public static class ReaderHtml
{
    /// <summary>Tamaño de letra en px que se pinta: el de la app por el zoom, entre 8 y 64.</summary>
    public static double BaseFontPx(double fontSize, double zoom) =>
        Math.Clamp(fontSize * Math.Max(zoom, 0.5), 8, 64);

    public static string Build(ReaderHtmlOptions o)
    {
        // Numeros CSS siempre con punto: con la app en castellano salian «15,5px» y «1,4», que el
        // navegador descarta (el zoom no cambiaba la letra y se perdia el interlineado).
        var inv = CultureInfo.InvariantCulture;
        var basePx = BaseFontPx(o.FontSize, o.Zoom).ToString(inv);
        var lineHeight = o.LineHeight.ToString(inv);
        var p = o.Palette;
        var nonce = Guid.NewGuid().ToString("N");

        string monospace = string.IsNullOrWhiteSpace(o.FontFamily)
            ? "Consolas, Monaco, 'Courier New', monospace"
            : o.FontFamily!;

        string body;
        string modeCss;
        string term = string.Empty;
        if (o.Markdown)
        {
            // Las casillas de las listas de tareas, dibujadas a mano: las de serie salen grises
            // (estan deshabilitadas) y casi no se distingue la marcada. La marca, del color del fondo.
            var checkMark = "data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 16 16'%3E%3Cpath d='M3.5 8.5l3 3 6-7' fill='none' stroke='"
                + p.Background.Replace("#", "%23") + "' stroke-width='2.5' stroke-linecap='round' stroke-linejoin='round'/%3E%3C/svg%3E";
            body = $"<article class=\"md\">{MarkdownRenderer.ToHtml(o.Text, o.BaseDirectory)}</article>";
            term = o.SearchTerm ?? string.Empty;
            modeCss = $$"""
        body { font-family: system-ui, -apple-system, 'Segoe UI', Roboto, sans-serif; }
        .md > :first-child { margin-top: 0; }
        h1, h2, h3, h4, h5, h6 { line-height: 1.25; margin: 1.2em 0 0.5em; font-weight: 700; }
        h1 { font-size: 1.9em; padding-bottom: .25em; border-bottom: 1px solid {{p.Border}}; }
        h2 { font-size: 1.5em; padding-bottom: .2em; border-bottom: 1px solid {{p.Border}}; }
        h3 { font-size: 1.25em; } h4 { font-size: 1.1em; } h5 { font-size: 1em; }
        h6 { font-size: .9em; color: {{p.Secondary}}; }
        p, ul, ol, dl, table, pre, blockquote { margin: 0 0 1em; }
        a { color: {{p.Link}}; text-decoration: underline; word-break: break-word; }
        ul, ol { padding-left: 1.6em; }
        li > p { margin: 0 0 .4em; }
        li.task-list-item { list-style: none; margin-left: -1.4em; }
        li.task-list-item input {
            -webkit-appearance: none; appearance: none; opacity: 1;
            width: 1em; height: 1em; margin: 0 .5em 0 0; vertical-align: -0.15em;
            border: 2px solid {{p.Link}}; border-radius: 3px; background: transparent;
        }
        li.task-list-item input:checked { background: {{p.Link}} url("{{checkMark}}") center / 85% no-repeat; }
        blockquote { padding: 0 1em; color: {{p.Secondary}}; border-left: 4px solid {{p.Border}}; }
        code, pre, kbd { font-family: {{monospace}}; font-size: .9em; }
        :not(pre) > code { background: {{p.CodeBackground}}; padding: .1em .35em; border-radius: 4px; }
        pre { background: {{p.CodeBackground}}; padding: .8em 1em; border-radius: 6px; overflow-x: auto; line-height: 1.35; }
        pre code { white-space: pre; }
        hr { border: 0; border-top: 2px solid {{p.Border}}; margin: 1.5em 0; }
        table { border-collapse: collapse; display: block; overflow-x: auto; max-width: 100%; }
        th, td { border: 1px solid {{p.Border}}; padding: .35em .7em; }
        th { background: {{p.CodeBackground}}; font-weight: 700; }
        img { max-width: 100%; height: auto; }
        dt { font-weight: 700; } dd { margin: 0 0 .5em 1.4em; }
        .footnotes { font-size: .9em; color: {{p.Secondary}}; }
""";
        }
        else
        {
            // 1-2) Texto escapado a HTML con las coincidencias marcadas con <mark>.
            body = TextHighlighter.ToHighlightedHtml(o.Text, o.SearchTerm, o.CaseSensitive);
            modeCss = $$"""
        body {
            font-family: {{monospace}};
            white-space: pre-wrap;      /* conserva saltos de línea */
            word-wrap: break-word;      /* evita desbordes */
        }
""";
        }

        // El termino viaja como literal JSON: el codificador por defecto escapa < > & ' y comillas.
        var termJson = JsonSerializer.Serialize(term);
        var caseJson = o.CaseSensitive ? "true" : "false";

        return $$"""
<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8" />
    <meta http-equiv="Content-Security-Policy" content="default-src 'none'; img-src data:; style-src 'unsafe-inline'; script-src 'nonce-{{nonce}}'" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0, user-scalable=yes" />
    <style>
        html, body { margin:0; padding:0; background: {{p.Background}}; }
        body {
            color: {{p.Foreground}};
            font-size: {{basePx}}px;
            line-height: {{lineHeight}};
            padding: 12px;
            overflow-wrap: break-word;
        }
        mark {
            background: {{p.HighlightBackground}};
            color: {{p.HighlightText}};
            font-weight: 700;
        }
{{modeCss}}
    </style>
</head>
<body>{{body}}
<script nonce="{{nonce}}">
(function () {
    var term = {{termJson}}, cs = {{caseJson}};
    if (term) {
        var needle = cs ? term : term.toLowerCase();
        var walker = document.createTreeWalker(document.body, NodeFilter.SHOW_TEXT, {
            acceptNode: function (n) {
                var tag = n.parentNode && n.parentNode.nodeName;
                return tag === 'SCRIPT' || tag === 'STYLE' || tag === 'MARK' ? NodeFilter.FILTER_REJECT : NodeFilter.FILTER_ACCEPT;
            }
        });
        var nodes = [];
        while (walker.nextNode()) nodes.push(walker.currentNode);
        nodes.forEach(function (n) {
            var s = n.nodeValue, hay = cs ? s : s.toLowerCase(), i = hay.indexOf(needle);
            if (i < 0) return;
            var frag = document.createDocumentFragment(), last = 0;
            while (i >= 0) {
                frag.appendChild(document.createTextNode(s.slice(last, i)));
                var m = document.createElement('mark');
                m.textContent = s.slice(i, i + needle.length);
                frag.appendChild(m);
                last = i + needle.length;
                i = hay.indexOf(needle, last);
            }
            frag.appendChild(document.createTextNode(s.slice(last)));
            n.parentNode.replaceChild(frag, n);
        });
    }
    var first = document.querySelector('mark');
    if (first) first.scrollIntoView({ block: 'center' });
    // Anclas del propio documento (indice, notas al pie): se salta dentro de la pagina.
    document.addEventListener('click', function (e) {
        var a = e.target.closest ? e.target.closest('a') : null;
        if (!a) return;
        var href = a.getAttribute('href') || '';
        if (href.charAt(0) !== '#') return;
        e.preventDefault();
        var id = decodeURIComponent(href.slice(1));
        var target = document.getElementById(id) || document.getElementsByName(id)[0];
        if (target) target.scrollIntoView();
    });
})();
</script>
</body>
</html>
""";
    }
}
