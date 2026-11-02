using System.Net;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;

namespace TXTReader.Services;

/// <summary>El formato en que se enseña un documento en la vista con formato.</summary>
public enum ReaderFormat
{
    /// <summary>El texto tal cual (la vista de origen).</summary>
    PlainText,
    Markdown,
    Csv,
    Json,
    Xml,
}

/// <summary>
/// Vistas con formato para datos, todas en el dispositivo y sin red: una tabla para el CSV, y el JSON
/// y el XML indentados y coloreados. El resultado es HTML con clases de color (los colores los pone la
/// pagina del visor segun el tema, <see cref="ReaderHtml"/>). Si el contenido esta mal formado, se
/// enseña tal cual (escapado) en lugar de fallar. Fuera de la pagina para poder probarlo sin interfaz
/// (General 8.6).
/// </summary>
public static class StructuredRenderer
{
    private static readonly Dictionary<string, ReaderFormat> ByExtension = new(StringComparer.OrdinalIgnoreCase)
    {
        [".csv"] = ReaderFormat.Csv,
        [".json"] = ReaderFormat.Json,
        [".xml"] = ReaderFormat.Xml,
        [".gpx"] = ReaderFormat.Xml, // los GPX son XML
    };

    private const string Step = "  "; // dos espacios por nivel de indentado

    /// <summary>El formato por la extension del nombre (Markdown, CSV, JSON o XML); si no, texto tal cual.</summary>
    public static ReaderFormat DetectFormat(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return ReaderFormat.PlainText;
        if (MarkdownRenderer.IsMarkdownFile(fileName))
            return ReaderFormat.Markdown;
        return ByExtension.TryGetValue(Path.GetExtension(fileName.Trim()), out var format) ? format : ReaderFormat.PlainText;
    }

    /// <summary>HTML del cuerpo con formato para datos (CSV, JSON o XML); vacio para lo demas.</summary>
    public static string ToHtml(ReaderFormat format, string? text) => format switch
    {
        ReaderFormat.Csv => CsvToHtml(text),
        ReaderFormat.Json => JsonToHtml(text),
        ReaderFormat.Xml => XmlToHtml(text),
        _ => string.Empty,
    };

    // ---- CSV: una tabla ----

    /// <summary>El CSV como tabla: la primera fila es la cabecera y una columna extra numera las filas.</summary>
    internal static string CsvToHtml(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;
        var delimiter = DetectDelimiter(text);
        var rows = ParseCsv(text, delimiter);
        if (rows.Count == 0)
            return string.Empty;

        var columns = rows.Max(r => r.Count);
        var sb = new StringBuilder("<table class=\"csv\">");

        var header = rows[0];
        sb.Append("<thead><tr><th class=\"rownum\"></th>");
        for (var c = 0; c < columns; c++)
            sb.Append("<th>").Append(Enc(c < header.Count ? header[c] : string.Empty)).Append("</th>");
        sb.Append("</tr></thead><tbody>");

        for (var i = 1; i < rows.Count; i++)
        {
            var row = rows[i];
            sb.Append("<tr><td class=\"rownum\">").Append(i).Append("</td>");
            for (var c = 0; c < columns; c++)
                sb.Append("<td>").Append(Enc(c < row.Count ? row[c] : string.Empty)).Append("</td>");
            sb.Append("</tr>");
        }

        sb.Append("</tbody></table>");
        return sb.ToString();
    }

    /// <summary>El separador mas probable de la primera linea: coma, punto y coma, tabulador o barra.</summary>
    internal static char DetectDelimiter(string text)
    {
        var line = text;
        var inQuotes = false;
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] == '"') inQuotes = !inQuotes;
            else if (text[i] == '\n' && !inQuotes) { line = text[..i]; break; }
        }

        var best = ',';
        var bestCount = 0;
        foreach (var candidate in new[] { ',', ';', '\t', '|' })
        {
            var count = CountOutsideQuotes(line, candidate);
            if (count > bestCount) { bestCount = count; best = candidate; }
        }
        return best;
    }

    private static int CountOutsideQuotes(string line, char delimiter)
    {
        var count = 0;
        var inQuotes = false;
        foreach (var ch in line)
        {
            if (ch == '"') inQuotes = !inQuotes;
            else if (ch == delimiter && !inQuotes) count++;
        }
        return count;
    }

    /// <summary>Filas y campos del CSV (RFC 4180): campos entre comillas, comillas dobladas y saltos dentro.</summary>
    internal static List<List<string>> ParseCsv(string text, char delimiter)
    {
        var rows = new List<List<string>>();
        var row = new List<string>();
        var field = new StringBuilder();
        var inQuotes = false;
        var started = false; // hay algo en la fila (campo, comillas o separador)

        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i];
            if (inQuotes)
            {
                if (ch == '"')
                {
                    if (i + 1 < text.Length && text[i + 1] == '"') { field.Append('"'); i++; }
                    else inQuotes = false;
                }
                else field.Append(ch);
            }
            else if (ch == '"') { inQuotes = true; started = true; }
            else if (ch == delimiter) { row.Add(field.ToString()); field.Clear(); started = true; }
            else if (ch == '\r') { /* el salto lo cierra el \n */ }
            else if (ch == '\n')
            {
                row.Add(field.ToString());
                field.Clear();
                rows.Add(row);
                row = new List<string>();
                started = false;
            }
            else { field.Append(ch); started = true; }
        }

        if (started || field.Length > 0)
        {
            row.Add(field.ToString());
            rows.Add(row);
        }
        return rows;
    }

    // ---- JSON: indentado y coloreado ----

    internal static string JsonToHtml(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;
        try
        {
            using var doc = JsonDocument.Parse(text, new JsonDocumentOptions
            {
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
            });
            var sb = new StringBuilder("<pre class=\"code\">");
            WriteJson(sb, doc.RootElement, 0);
            sb.Append("</pre>");
            return sb.ToString();
        }
        catch (JsonException)
        {
            return Raw(text); // mal formado: se enseña tal cual, sin colorear
        }
    }

    private static void WriteJson(StringBuilder sb, JsonElement element, int depth)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var properties = element.EnumerateObject().ToList();
                if (properties.Count == 0) { Punct(sb, "{}"); break; }
                Punct(sb, "{");
                sb.Append('\n');
                for (var i = 0; i < properties.Count; i++)
                {
                    Pad(sb, depth + 1);
                    Tok(sb, "tok-key", "\"" + properties[i].Name + "\"");
                    Punct(sb, ": ");
                    WriteJson(sb, properties[i].Value, depth + 1);
                    if (i < properties.Count - 1) Punct(sb, ",");
                    sb.Append('\n');
                }
                Pad(sb, depth);
                Punct(sb, "}");
                break;

            case JsonValueKind.Array:
                var items = element.EnumerateArray().ToList();
                if (items.Count == 0) { Punct(sb, "[]"); break; }
                Punct(sb, "[");
                sb.Append('\n');
                for (var i = 0; i < items.Count; i++)
                {
                    Pad(sb, depth + 1);
                    WriteJson(sb, items[i], depth + 1);
                    if (i < items.Count - 1) Punct(sb, ",");
                    sb.Append('\n');
                }
                Pad(sb, depth);
                Punct(sb, "]");
                break;

            case JsonValueKind.String:
                Tok(sb, "tok-str", element.GetRawText());
                break;
            case JsonValueKind.Number:
                Tok(sb, "tok-num", element.GetRawText());
                break;
            default: // True, False, Null
                Tok(sb, "tok-kw", element.GetRawText());
                break;
        }
    }

    // ---- XML: indentado y coloreado ----

    internal static string XmlToHtml(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;
        try
        {
            var document = XDocument.Parse(text);
            var body = new StringBuilder();
            var settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = Step,
                OmitXmlDeclaration = true, // la declaracion original se antepone a mano (conserva su codificacion)
                NewLineChars = "\n",
            };
            using (var writer = XmlWriter.Create(body, settings))
                document.Save(writer);

            var pretty = document.Declaration != null ? document.Declaration + "\n" + body : body.ToString();
            return "<pre class=\"code\">" + HighlightXml(pretty) + "</pre>";
        }
        catch (XmlException)
        {
            return Raw(text); // mal formado: se enseña tal cual, sin colorear
        }
    }

    /// <summary>Colorea un XML ya bien formado: etiquetas, atributos, valores, comentarios y texto.</summary>
    private static string HighlightXml(string s)
    {
        var sb = new StringBuilder();
        var i = 0;
        while (i < s.Length)
        {
            if (s[i] == '<')
            {
                if (StartsAt(s, i, "<!--")) { i = Chunk(sb, s, i, "-->", "tok-comment"); continue; }
                if (StartsAt(s, i, "<![CDATA[")) { i = Chunk(sb, s, i, "]]>", "tok-cdata"); continue; }
                if (StartsAt(s, i, "<?")) { i = Chunk(sb, s, i, "?>", "tok-decl"); continue; }
                if (StartsAt(s, i, "<!")) { i = Chunk(sb, s, i, ">", "tok-decl"); continue; }
                i = Tag(sb, s, i);
                continue;
            }
            var start = i;
            while (i < s.Length && s[i] != '<') i++;
            sb.Append(Enc(s[start..i])); // texto entre etiquetas
        }
        return sb.ToString();
    }

    private static int Tag(StringBuilder sb, string s, int start)
    {
        // Fin de la etiqueta: el primer '>' fuera de comillas.
        var i = start + 1;
        var quote = '\0';
        while (i < s.Length)
        {
            var c = s[i];
            if (quote != '\0') { if (c == quote) quote = '\0'; }
            else if (c is '"' or '\'') quote = c;
            else if (c == '>') { i++; break; }
            i++;
        }
        TokenizeTag(sb, s, start, i);
        return i;
    }

    private static void TokenizeTag(StringBuilder sb, string s, int start, int end)
    {
        var i = start;
        Open(sb, "tok-tag");
        sb.Append(Enc("<"));
        i++;
        if (i < end && s[i] == '/') { sb.Append('/'); i++; }
        var nameStart = i;
        while (i < end && !char.IsWhiteSpace(s[i]) && s[i] != '>' && s[i] != '/') i++;
        sb.Append(Enc(s[nameStart..i]));
        Close(sb);

        while (i < end)
        {
            var c = s[i];
            if (char.IsWhiteSpace(c)) { sb.Append(c); i++; continue; }
            if (c == '>') { Tok(sb, "tok-tag", ">"); i++; continue; }
            if (c == '/') { Tok(sb, "tok-tag", "/"); i++; continue; }

            var attrStart = i;
            while (i < end && !char.IsWhiteSpace(s[i]) && s[i] != '=' && s[i] != '>' && s[i] != '/') i++;
            if (i > attrStart)
            {
                Open(sb, "tok-attr");
                sb.Append(Enc(s[attrStart..i]));
                Close(sb);
            }
            while (i < end && char.IsWhiteSpace(s[i])) { sb.Append(s[i]); i++; }
            if (i < end && s[i] == '=')
            {
                Tok(sb, "tok-punct", "=");
                i++;
                while (i < end && char.IsWhiteSpace(s[i])) { sb.Append(s[i]); i++; }
                if (i < end && s[i] is '"' or '\'')
                {
                    var q = s[i];
                    var valueStart = i;
                    i++;
                    while (i < end && s[i] != q) i++;
                    if (i < end) i++; // incluye la comilla de cierre
                    Open(sb, "tok-str");
                    sb.Append(Enc(s[valueStart..i]));
                    Close(sb);
                }
            }
        }
    }

    private static int Chunk(StringBuilder sb, string s, int start, string end, string cls)
    {
        var index = s.IndexOf(end, start, StringComparison.Ordinal);
        var stop = index < 0 ? s.Length : index + end.Length;
        Open(sb, cls);
        sb.Append(Enc(s[start..stop]));
        Close(sb);
        return stop;
    }

    // ---- utiles ----

    private static bool StartsAt(string s, int i, string prefix) =>
        i + prefix.Length <= s.Length && string.CompareOrdinal(s, i, prefix, 0, prefix.Length) == 0;

    private static void Pad(StringBuilder sb, int depth)
    {
        for (var i = 0; i < depth; i++) sb.Append(Step);
    }

    private static void Punct(StringBuilder sb, string raw) => Tok(sb, "tok-punct", raw);

    private static void Tok(StringBuilder sb, string cls, string raw)
    {
        Open(sb, cls);
        sb.Append(Enc(raw));
        Close(sb);
    }

    private static void Open(StringBuilder sb, string cls) => sb.Append("<span class=\"").Append(cls).Append("\">");

    private static void Close(StringBuilder sb) => sb.Append("</span>");

    private static string Raw(string text) => "<pre class=\"code\">" + Enc(text) + "</pre>";

    private static string Enc(string text) => WebUtility.HtmlEncode(text);
}
