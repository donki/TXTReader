using System.Reflection;
using TXTReader.Controls;
using TXTReader.Pages;
using TXTReader.Services;

namespace TXTReader.Tests;

/// <summary>CSV a tabla y JSON/XML indentados y coloreados: cada pieza y los casos mal formados.</summary>
public sealed class StructuredRendererTests
{
    [Theory]
    [InlineData("datos.CSV", ReaderFormat.Csv)]
    [InlineData("a.json", ReaderFormat.Json)]
    [InlineData("config.XML", ReaderFormat.Xml)]
    [InlineData("ruta.gpx", ReaderFormat.Xml)]
    [InlineData("README.md", ReaderFormat.Markdown)]
    [InlineData("notas.txt", ReaderFormat.PlainText)]
    [InlineData("sin-extension", ReaderFormat.PlainText)]
    [InlineData("", ReaderFormat.PlainText)]
    [InlineData(null, ReaderFormat.PlainText)]
    public void DetectFormat_ByExtension(string? name, ReaderFormat expected) =>
        Assert.Equal(expected, StructuredRenderer.DetectFormat(name));

    [Theory]
    [InlineData("a,b,c\n1,2,3", ',')]
    [InlineData("a;b;c\n1;2;3", ';')]
    [InlineData("a\tb\tc", '\t')]
    [InlineData("a|b|c", '|')]
    [InlineData("\"x,y\";\"z\"", ';')]   // las comas dentro de comillas no cuentan
    [InlineData("sin separador", ',')]   // por defecto, coma
    public void DetectDelimiter_PicksTheMostLikely(string text, char expected) =>
        Assert.Equal(expected, StructuredRenderer.DetectDelimiter(text));

    [Fact]
    public void Csv_RespectsQuotesCommasAndDoubledQuotes()
    {
        var rows = StructuredRenderer.ParseCsv("a,b\n\"x,y\",\"dijo \"\"hola\"\"\"\n1,2", ',');
        Assert.Equal(3, rows.Count);
        Assert.Equal(["a", "b"], rows[0]);
        Assert.Equal(["x,y", "dijo \"hola\""], rows[1]);
        Assert.Equal(["1", "2"], rows[2]);
    }

    [Fact]
    public void Csv_NewlineInsideQuotes_StaysInTheSameField()
    {
        var rows = StructuredRenderer.ParseCsv("a,b\n\"linea1\nlinea2\",z", ',');
        Assert.Equal(2, rows.Count);
        Assert.Equal("linea1\nlinea2", rows[1][0]);
    }

    [Fact]
    public void Csv_ToHtml_IsATableWithHeaderAndRowNumbers()
    {
        var html = StructuredRenderer.CsvToHtml("nombre,edad\nAna,3\nLuis,5");
        Assert.Contains("<table class=\"csv\">", html);
        Assert.Contains("<thead><tr><th class=\"rownum\"></th><th>nombre</th><th>edad</th></tr></thead>", html);
        Assert.Contains("<tr><td class=\"rownum\">1</td><td>Ana</td><td>3</td></tr>", html);
        Assert.Contains("<tr><td class=\"rownum\">2</td><td>Luis</td><td>5</td></tr>", html);
    }

    [Fact]
    public void Csv_RaggedRows_ArePaddedAndCellsAreEscaped()
    {
        var html = StructuredRenderer.CsvToHtml("a,b,c\n1\n<b>,\"&\"");
        // La fila corta se completa con celdas vacias hasta el ancho maximo (3 columnas).
        Assert.Contains("<tr><td class=\"rownum\">1</td><td>1</td><td></td><td></td></tr>", html);
        Assert.Contains("<td>&lt;b&gt;</td><td>&amp;</td>", html);
    }

    [Fact]
    public void Json_IsIndentedAndColored()
    {
        var html = StructuredRenderer.JsonToHtml("{\"nombre\":\"Ana\",\"edad\":3,\"ok\":true,\"x\":null,\"lista\":[1,2]}");
        Assert.StartsWith("<pre class=\"code\">", html);
        Assert.Contains("<span class=\"tok-key\">&quot;nombre&quot;</span>", html);
        Assert.Contains("<span class=\"tok-str\">&quot;Ana&quot;</span>", html);
        Assert.Contains("<span class=\"tok-num\">3</span>", html);
        Assert.Contains("<span class=\"tok-kw\">true</span>", html);
        Assert.Contains("<span class=\"tok-kw\">null</span>", html);
        Assert.Contains("<span class=\"tok-punct\">[</span>", html);
        Assert.Contains('\n', html); // indentado en varias lineas
    }

    [Fact]
    public void Json_EmptyObjectAndArray_StayOnOneLine()
    {
        Assert.Contains("<span class=\"tok-punct\">{}</span>", StructuredRenderer.JsonToHtml("{}"));
        Assert.Contains("<span class=\"tok-punct\">[]</span>", StructuredRenderer.JsonToHtml("[]"));
    }

    [Fact]
    public void Json_Malformed_ShownAsIs_WithoutColors()
    {
        var html = StructuredRenderer.JsonToHtml("{no es json");
        Assert.Contains("<pre class=\"code\">", html);
        Assert.Contains("{no es json", html);
        Assert.DoesNotContain("tok-key", html);
    }

    [Fact]
    public void Xml_IsIndentedAndColored()
    {
        var html = StructuredRenderer.XmlToHtml("<root a=\"1\"><!-- c --><item>hola</item></root>");
        Assert.StartsWith("<pre class=\"code\">", html);
        Assert.Contains("<span class=\"tok-tag\">&lt;root</span>", html); // la apertura y el nombre, un token
        Assert.Contains("<span class=\"tok-tag\">&gt;</span>", html);
        Assert.Contains("<span class=\"tok-attr\">a</span>", html);
        Assert.Contains("<span class=\"tok-str\">&quot;1&quot;</span>", html);
        Assert.Contains("<span class=\"tok-comment\">&lt;!-- c --&gt;</span>", html);
        Assert.Contains("hola", html);
    }

    [Fact]
    public void Xml_KeepsTheDeclarationAndEscapesText()
    {
        var html = StructuredRenderer.XmlToHtml("<?xml version=\"1.0\" encoding=\"UTF-8\"?><n>a &lt; b</n>");
        Assert.Contains("<span class=\"tok-decl\">&lt;?xml version=&quot;1.0&quot; encoding=&quot;UTF-8&quot;?&gt;</span>", html);
        Assert.Contains("a &amp;lt; b", html); // la entidad del documento se enseña tal cual (&lt;)
    }

    [Fact]
    public void Xml_Malformed_ShownAsIs_WithoutColors()
    {
        var html = StructuredRenderer.XmlToHtml("<root><sin-cerrar>");
        Assert.Contains("<pre class=\"code\">", html);
        Assert.DoesNotContain("tok-tag", html);
        Assert.Contains("&lt;root&gt;", html);
    }

    [Fact]
    public void EmptyInput_IsEmpty()
    {
        Assert.Equal(string.Empty, StructuredRenderer.ToHtml(ReaderFormat.Csv, ""));
        Assert.Equal(string.Empty, StructuredRenderer.ToHtml(ReaderFormat.Json, "   "));
        Assert.Equal(string.Empty, StructuredRenderer.ToHtml(ReaderFormat.Xml, null));
        Assert.Equal(string.Empty, StructuredRenderer.ToHtml(ReaderFormat.PlainText, "x"));
    }
}

/// <summary>La pagina del visor con las vistas de datos: los colores del tema y el marcado por el script.</summary>
public class ReaderHtmlDataTests
{
    private static readonly ReaderPalette Dark =
        new("#E6E1E9", "#201F27", "#C7C4D8", "#48454F", "#2D2B36", "#C3C0FF", "#FFE08A", "#000000");

    [Fact]
    public void Csv_IsWrappedInData_AndTheScriptMarksTheTerm()
    {
        var html = ReaderHtml.Build(new ReaderHtmlOptions { Text = "a,b\n1,2", RenderFormat = ReaderFormat.Csv, SearchTerm = "1" });
        Assert.Contains("<article class=\"data\"><table class=\"csv\">", html);
        Assert.Contains("var term = \"1\"", html);   // marca sobre el HTML pintado, como en Markdown
        Assert.Contains(".tok-key", html);           // el CSS de datos esta presente
        Assert.DoesNotContain("white-space: pre-wrap;\n", html); // no es el modo de texto tal cual
    }

    [Fact]
    public void SyntaxColors_FollowTheTheme()
    {
        var light = ReaderHtml.Build(new ReaderHtmlOptions { Text = "{}", RenderFormat = ReaderFormat.Json });
        Assert.Contains(".tok-str { color: #0B7285", light);
        Assert.Contains(".tok-num { color: #C2255C", light);

        var dark = ReaderHtml.Build(new ReaderHtmlOptions { Text = "{}", RenderFormat = ReaderFormat.Json, Palette = Dark });
        Assert.Contains(".tok-str { color: #9BE59B", dark);
        Assert.Contains(".tok-key { color: #C3C0FF", dark); // la clave usa el enlace del tema
    }

    [Theory]
    [InlineData("#FFFFFF", false)]
    [InlineData("#201F27", true)]
    [InlineData("#FF201F27", true)] // ARGB de ToArgbHex
    [InlineData("nope", false)]
    public void IsDarkColor_ByLuminance(string hex, bool expected) =>
        Assert.Equal(expected, ReaderHtml.IsDarkColor(hex));
}

/// <summary>El lector con un CSV/JSON/XML: se abre con formato y el boton alterna con el texto.</summary>
public sealed class DataReaderPageTests : IDisposable
{
    private readonly AppHarness _h = new();

    public void Dispose() => _h.Dispose();

    private static T Field<T>(object page, string name) =>
        (T)page.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(page)!;

    [Fact]
    public async Task CsvFile_OpensAsTable_AndTheButtonShowsTheText()
    {
        var page = new TextReaderPage(_h.Temp.File("datos.csv", "nombre,edad\nAna,3"), "datos.csv");
        await page.Loading;

        var viewer = Field<SelectableHighlightedTextView>(page, "ContentViewer");
        var toggle = Field<ToolbarItem>(page, "MarkdownToggle");
        Assert.True(page.IsRendered);
        Assert.Equal(ReaderFormat.Csv, viewer.Format);
        Assert.Contains("<table class=\"csv\">", viewer.Html);
        Assert.Equal("View plain text", toggle.Text); // con formato: el boton ofrece el texto tal cual

        page.ToggleMarkdown();
        Assert.False(page.IsRendered);
        Assert.Contains("nombre,edad", viewer.Html);
        Assert.DoesNotContain("<table", viewer.Html);
        Assert.Equal("View as table", toggle.Text); // tal cual: el boton ofrece la tabla

        LocalizationService.Instance.SetLanguage("es");
        Assert.Equal("Ver como tabla", toggle.Text);
        LocalizationService.Instance.SetLanguage("en");
    }

    [Fact]
    public async Task JsonFile_OpensFormatted()
    {
        var page = new TextReaderPage(_h.Temp.File("a.json", "{\"x\":1}"), "a.json");
        await page.Loading;
        var viewer = Field<SelectableHighlightedTextView>(page, "ContentViewer");
        var toggle = Field<ToolbarItem>(page, "MarkdownToggle");

        Assert.True(page.IsRendered);
        Assert.Equal(ReaderFormat.Json, viewer.Format);
        Assert.Contains("<span class=\"tok-key\">&quot;x&quot;</span>", viewer.Html);
        page.ToggleMarkdown(); // al texto tal cual: el boton ofrece volver al JSON con formato
        Assert.Equal("View formatted JSON", toggle.Text);
    }

    [Fact]
    public async Task XmlFile_OpensFormatted()
    {
        var page = new TextReaderPage(_h.Temp.File("a.xml", "<r><a>1</a></r>"), "a.xml");
        await page.Loading;
        var viewer = Field<SelectableHighlightedTextView>(page, "ContentViewer");
        var toggle = Field<ToolbarItem>(page, "MarkdownToggle");

        Assert.True(page.IsRendered);
        Assert.Equal(ReaderFormat.Xml, viewer.Format);
        Assert.Contains("<span class=\"tok-tag\">&lt;r</span>", viewer.Html);
        page.ToggleMarkdown(); // al texto tal cual: el boton ofrece volver al XML con formato
        Assert.Equal("View formatted XML", toggle.Text);
    }
}
