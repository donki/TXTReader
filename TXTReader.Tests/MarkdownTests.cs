using System.Reflection;
using Markdig.Syntax.Inlines;
using TXTReader.Controls;
using TXTReader.Pages;
using TXTReader.Services;

namespace TXTReader.Tests;

/// <summary>Markdown a HTML: cada elemento que se pinta y que nada se pide a la red.</summary>
public sealed class MarkdownRendererTests : IDisposable
{
    private readonly TempFolder _temp = new();

    public void Dispose() => _temp.Dispose();

    [Theory]
    [InlineData("README.md", true)]
    [InlineData("notas.MARKDOWN", true)]
    [InlineData("x.mdown", true)]
    [InlineData("/sdcard/Download/guia.mkd", true)]
    [InlineData(" espacios.md ", true)]
    [InlineData("a.txt", false)]
    [InlineData("md", false)]
    [InlineData("a.md.txt", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsMarkdownFile_ByExtension(string? name, bool expected) =>
        Assert.Equal(expected, MarkdownRenderer.IsMarkdownFile(name));

    [Fact]
    public void Headings_HaveGitHubAnchors()
    {
        var html = MarkdownRenderer.ToHtml("# Título uno\n\n## Segundo nivel\n\n###### Seis", null);
        Assert.Contains("<h1 id=\"título-uno\">Título uno</h1>", html);
        Assert.Contains("<h2 id=\"segundo-nivel\">Segundo nivel</h2>", html);
        Assert.Contains("<h6 id=\"seis\">Seis</h6>", html);
    }

    [Fact]
    public void Emphasis_BoldItalicStrikethrough()
    {
        var html = MarkdownRenderer.ToHtml("**negrita** *cursiva* ~~tachado~~ ***las dos***", null);
        Assert.Contains("<strong>negrita</strong>", html);
        Assert.Contains("<em>cursiva</em>", html);
        Assert.Contains("<del>tachado</del>", html);
        Assert.Contains("<em><strong>las dos</strong></em>", html);
    }

    [Fact]
    public void Lists_BulletsNumbersAndTasks()
    {
        var html = MarkdownRenderer.ToHtml("- uno\n- dos\n\n3. tres\n4. cuatro\n\n- [x] hecha\n- [ ] pendiente\n\na. letra\nb. otra", null);
        Assert.Contains("<ul>\n<li>uno</li>", html);
        Assert.Contains("<ol start=\"3\">", html);
        Assert.Contains("class=\"task-list-item\"", html);
        Assert.Matches("<input disabled=\"disabled\" type=\"checkbox\" checked=\"checked\" />\\s*hecha", html);
        Assert.Matches("<input disabled=\"disabled\" type=\"checkbox\" />\\s*pendiente", html);
        Assert.Contains("<ol type=\"a\">", html);
    }

    [Fact]
    public void QuotesCodeAndRules()
    {
        var html = MarkdownRenderer.ToHtml("> cita\n\nusa `x < y` aqui\n\n```csharp\nvar a = \"<b>\";\n```\n\n---\n", null);
        Assert.Contains("<blockquote>\n<p>cita</p>\n</blockquote>", html);
        Assert.Contains("<code>x &lt; y</code>", html);
        Assert.Contains("<pre><code class=\"language-csharp\">var a = &quot;&lt;b&gt;&quot;;\n</code></pre>", html);
        Assert.Contains("<hr />", html);
    }

    [Fact]
    public void Tables_PipeAndAlignment()
    {
        var html = MarkdownRenderer.ToHtml("| A | B |\n|:--|--:|\n| 1 | 2 |", null);
        Assert.Contains("<table>", html);
        Assert.Contains("<th style=\"text-align: left;\">A</th>", html);
        Assert.Contains("<td style=\"text-align: right;\">2</td>", html);
    }

    [Fact]
    public void Links_InlineAndBare()
    {
        var html = MarkdownRenderer.ToHtml("[web](https://example.com) y www.example.org y <mailto:a@b.c>", null);
        Assert.Contains("<a href=\"https://example.com\">web</a>", html);
        Assert.Contains("href=\"http://www.example.org\"", html);
        Assert.Contains("href=\"mailto:a@b.c\"", html);
    }

    [Fact]
    public void FootnotesAndDefinitions()
    {
        var html = MarkdownRenderer.ToHtml("Texto[^1]\n\n[^1]: La nota.\n\nTermino\n:   Definicion", null);
        Assert.Contains("class=\"footnote-ref\"", html);
        Assert.Contains("La nota.", html);
        Assert.Contains("<dt>Termino</dt>", html);
        Assert.Contains("<dd>Definicion</dd>", html);
    }

    [Fact]
    public void NullOrEmpty_IsEmpty()
    {
        Assert.Equal(string.Empty, MarkdownRenderer.ToHtml(null, null));
        Assert.Equal(string.Empty, MarkdownRenderer.ToHtml("", null));
    }

    [Fact]
    public void LocalImage_IsEmbedded_AsDataUri()
    {
        var bytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 1, 2, 3 };
        Directory.CreateDirectory(_temp.Combine("img dir"));
        File.WriteAllBytes(_temp.Combine("img dir", "logo.png"), bytes);

        var html = MarkdownRenderer.ToHtml("![El logo](img%20dir/logo.png?v=2 \"titulo\")", _temp.Path);

        Assert.Contains($"src=\"data:image/png;base64,{Convert.ToBase64String(bytes)}\"", html);
        Assert.Contains("alt=\"El logo\"", html);
    }

    [Fact]
    public void LocalImage_AbsoluteAndFileUri_AreEmbedded()
    {
        var path = _temp.Combine("foto.JPG");
        File.WriteAllBytes(path, [1, 2]);

        Assert.Contains("data:image/jpeg;base64,AQI=", MarkdownRenderer.ToHtml($"![a]({new Uri(path).AbsoluteUri})", null));
        Assert.Contains("data:image/jpeg;base64,AQI=", MarkdownRenderer.ToHtml($"![a](<{path}>)", null));
    }

    [Fact]
    public void RemoteImage_BecomesALink_WithItsAltText()
    {
        var html = MarkdownRenderer.ToHtml("![Insignia](https://img.shields.io/x.svg)", null);
        Assert.Contains("<a href=\"https://img.shields.io/x.svg\">Insignia</a>", html);
        Assert.DoesNotContain("<img", html);
    }

    [Fact]
    public void MissingTooBigOrUnknownImages_StayAsTheyAre()
    {
        File.WriteAllText(_temp.Combine("dato.txt"), "no es imagen");
        using (var big = File.Create(_temp.Combine("enorme.png")))
            big.SetLength(MarkdownRenderer.MaxInlineImageBytes + 1);

        Assert.Contains("src=\"falta.png\"", MarkdownRenderer.ToHtml("![x](falta.png)", _temp.Path));
        Assert.Contains("src=\"dato.txt\"", MarkdownRenderer.ToHtml("![x](dato.txt)", _temp.Path));
        Assert.Contains("src=\"enorme.png\"", MarkdownRenderer.ToHtml("![x](enorme.png)", _temp.Path));
        // Sin carpeta (fichero de content://) no se busca nada relativo.
        Assert.Contains("src=\"falta.png\"", MarkdownRenderer.ToHtml("![x](falta.png)", null));
        // Otros esquemas tampoco se tocan, y data: se queda como viene.
        Assert.Contains("src=\"ftp://x/y.png\"", MarkdownRenderer.ToHtml("![x](ftp://x/y.png)", _temp.Path));
        Assert.Contains("src=\"data:image/gif;base64,R0lG\"", MarkdownRenderer.ToHtml("![x](data:image/gif;base64,R0lG)", _temp.Path));
    }

    [Fact]
    public void ImageHelpers_SurviveBadInput()
    {
        Assert.Null(MarkdownRenderer.LocalImagePath("a\0b.png", _temp.Path));
        Assert.Null(MarkdownRenderer.ToDataUri("\0.png"));
        Assert.Null(MarkdownRenderer.ToDataUri(_temp.Combine("no.png")));

        var empty = new LinkInline { IsImage = true, Url = "" };
        MarkdownRenderer.ResolveImage(empty, _temp.Path);
        Assert.True(empty.IsImage);
    }
}

/// <summary>La pagina del visor: modos, colores del tema, zoom, busqueda y la politica sin red.</summary>
public class ReaderHtmlTests
{
    private static readonly ReaderPalette Dark =
        new("#E6E1E9", "#201F27", "#C7C4D8", "#48454F", "#2D2B36", "#C3C0FF", "#FFE08A", "#000000");

    [Fact]
    public void Plain_IsTheEncodedText_WithServerSideMarks()
    {
        var html = ReaderHtml.Build(new ReaderHtmlOptions { Text = "# no es titulo <b>", SearchTerm = "titulo" });
        Assert.Contains("# no es <mark>titulo</mark> &lt;b&gt;", html);
        Assert.Contains("white-space: pre-wrap", html);
        Assert.DoesNotContain("<h1", html);
        Assert.Contains("var term = \"\"", html); // el script no vuelve a marcar
    }

    [Fact]
    public void Markdown_IsRendered_AndTheScriptMarksTheTerm()
    {
        var html = ReaderHtml.Build(new ReaderHtmlOptions { Text = "# Hola", SearchTerm = "</script>\"'&", Markdown = true, CaseSensitive = true });
        Assert.Contains("<article class=\"md\"><h1 id=\"hola\">Hola</h1>", html);
        // El termino va como JSON con < > & ' " escapados: no puede cerrar el script.
        Assert.Contains("var term = \"\\u003C/script\\u003E\\u0022\\u0027\\u0026\", cs = true;", html);
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(html, "</script>"));
    }

    [Fact]
    public void NoNetwork_AndOnlyItsOwnScript()
    {
        var html = ReaderHtml.Build(new ReaderHtmlOptions { Text = "<script>alert(1)</script><img src=\"https://x/y.png\">", Markdown = true });
        var nonce = System.Text.RegularExpressions.Regex.Match(html, "script-src 'nonce-([0-9a-f]{32})'").Groups[1].Value;
        Assert.NotEmpty(nonce);
        Assert.Contains("default-src 'none'; img-src data:;", html);
        Assert.Contains($"<script nonce=\"{nonce}\">", html);
        // Cada pagina, su nonce: el HTML del documento no lo puede adivinar.
        Assert.NotEqual(html, ReaderHtml.Build(new ReaderHtmlOptions { Text = "<script>alert(1)</script><img src=\"https://x/y.png\">", Markdown = true }));
    }

    [Fact]
    public void Colors_ComeFromThePalette()
    {
        var html = ReaderHtml.Build(new ReaderHtmlOptions { Text = "`c`", Markdown = true, Palette = Dark });
        Assert.Contains("background: #201F27", html);
        Assert.Contains("color: #E6E1E9", html);
        Assert.Contains("a { color: #C3C0FF", html);
        Assert.Contains(":not(pre) > code { background: #2D2B36", html);
        Assert.Contains("border-left: 4px solid #48454F", html);
        // La marca de las casillas hechas, del color del fondo sobre el de los enlaces.
        Assert.Contains("input:checked { background: #C3C0FF url(\"data:image/svg+xml,", html);
        Assert.Contains("stroke='%23201F27'", html);
    }

    [Theory]
    [InlineData(14, 1, "14px")]
    [InlineData(14, 1.5, "21px")]
    [InlineData(14, 0.1, "8px")]    // zoom minimo 0,5 y letra minima 8
    [InlineData(14, 10, "64px")]    // letra maxima 64
    [InlineData(15.5, 1, "15.5px")] // siempre con punto
    public void FontSize_FollowsTheZoom(double size, double zoom, string expected)
    {
        var previous = Thread.CurrentThread.CurrentCulture;
        Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("es-ES");
        try
        {
            Assert.Contains($"font-size: {expected};", ReaderHtml.Build(new ReaderHtmlOptions { FontSize = size, Zoom = zoom, Markdown = true }));
            Assert.Contains($"font-size: {expected};", ReaderHtml.Build(new ReaderHtmlOptions { FontSize = size, Zoom = zoom }));
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = previous;
        }
    }

    [Fact]
    public void Fonts_MonospaceForText_SansForMarkdown_CustomKept()
    {
        Assert.Contains("font-family: Consolas, Monaco, 'Courier New', monospace;", ReaderHtml.Build(new ReaderHtmlOptions()));
        Assert.Contains("font-family: Serif;", ReaderHtml.Build(new ReaderHtmlOptions { FontFamily = "Serif" }));
        var md = ReaderHtml.Build(new ReaderHtmlOptions { Markdown = true });
        Assert.Contains("font-family: system-ui", md);
        Assert.Contains("code, pre, kbd { font-family: Consolas", md);
    }
}

public class ReaderLinksTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("about:blank")]
    [InlineData("data:text/html,x")]
    [InlineData("file:///android_asset/")]
    [InlineData("FILE:///android_asset")]
    public void TheViewerItself_Loads(string? url) => Assert.True(ReaderLinks.IsViewerPage(url));

    [Theory]
    [InlineData("file:///android_asset/otro.md")]
    [InlineData("https://example.com")]
    public void AnythingElse_IsNotTheViewer(string url) => Assert.False(ReaderLinks.IsViewerPage(url));

    [Theory]
    [InlineData("https://example.com/a?b=1", "https://example.com/a?b=1")]
    [InlineData("http://x.org", "http://x.org/")]
    [InlineData(" mailto:a@b.c ", "mailto:a@b.c")]
    [InlineData("tel:+34600000000", "tel:+34600000000")]
    [InlineData("geo:41.4,2.1", "geo:41.4,2.1")]
    public void WebMailAndPhone_OpenOutside(string url, string expected) =>
        Assert.Equal(expected, ReaderLinks.ExternalUri(url)!.ToString());

    [Theory]
    [InlineData(null)]
    [InlineData(" ")]
    [InlineData("file:///android_asset/otro.md")]
    [InlineData("javascript:alert(1)")]
    [InlineData("intent://scan/#Intent;end")]
    [InlineData("otro.md")]
    public void Others_AreIgnored(string? url) => Assert.Null(ReaderLinks.ExternalUri(url));
}

/// <summary>El lector con un .md: se abre renderizado, el boton alterna, enlaces y tema.</summary>
public sealed class MarkdownReaderPageTests : IDisposable
{
    private readonly AppHarness _h = new();

    public void Dispose() => _h.Dispose();

    private static T Field<T>(object page, string name) =>
        (T)page.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(page)!;

    private static void Raise(object target, string handler, params object?[] args) =>
        target.GetType().GetMethod(handler, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)!.Invoke(target, args);

    [Fact]
    public async Task MarkdownFile_OpensRendered_AndTheButtonShowsTheText()
    {
        var file = _h.Temp.File("docs/README.md", "# Hola\n\n**negrita**");
        var page = new TextReaderPage(file, "README.md");
        await page.Loading;

        var viewer = Field<SelectableHighlightedTextView>(page, "ContentViewer");
        var toggle = Field<ToolbarItem>(page, "MarkdownToggle");
        Assert.True(page.IsRendered);
        Assert.Contains("<h1 id=\"hola\">Hola</h1>", viewer.Html);
        Assert.Equal(Path.GetDirectoryName(file), viewer.BaseDirectory);
        Assert.Equal("View plain text", toggle.Text);
        Assert.Equal("ic_view_source.png", ((FileImageSource)toggle.IconImageSource).File);
        Assert.Contains(toggle, page.ToolbarItems);

        Raise(page, "OnMarkdownToggleClicked", toggle, EventArgs.Empty);

        Assert.False(page.IsRendered);
        Assert.Contains("# Hola", viewer.Html);
        Assert.DoesNotContain("<h1", viewer.Html);
        Assert.Equal("View rendered Markdown", toggle.Text);
        Assert.Equal("ic_view_rendered.png", ((FileImageSource)toggle.IconImageSource).File);

        LocalizationService.Instance.SetLanguage("es");
        Assert.Equal("Ver Markdown renderizado", toggle.Text);
        page.ToggleMarkdown();
        Assert.Equal("Ver texto tal cual", toggle.Text);
    }

    [Fact]
    public async Task TextFile_OpensAsIs_AndCanBeRendered()
    {
        var page = new TextReaderPage(_h.Temp.File("notas.txt", "## Sub"), "notas.txt");
        await page.Loading;
        var viewer = Field<SelectableHighlightedTextView>(page, "ContentViewer");

        Assert.False(page.IsRendered);
        Assert.Contains("## Sub", viewer.Html);

        page.ToggleMarkdown();
        Assert.Contains("<h2 id=\"sub\">Sub</h2>", viewer.Html);
    }

    [Fact]
    public async Task Search_AndZoom_AlsoApplyToTheRenderedView()
    {
        var page = new TextReaderPage(_h.Temp.File("a.md", "uno *dos*"), "a.md");
        await page.Loading;
        var viewer = Field<SelectableHighlightedTextView>(page, "ContentViewer");

        Field<Entry>(page, "SearchEntry").Text = "dos";
        Field<Slider>(page, "ZoomSlider").Value = 21;

        Assert.Contains("var term = \"dos\"", viewer.Html);
        Assert.Contains("font-size: 21px;", viewer.Html);
    }

    [Fact]
    public void ContentUri_HasNoFolder_ButTheDisplayNameDecides()
    {
        Assert.Null(TextReaderPage.BaseDirectoryOf("content://prov/doc/1"));
        Assert.Null(TextReaderPage.BaseDirectoryOf(""));
        Assert.Null(TextReaderPage.BaseDirectoryOf("a\0b"));

        var page = new TextReaderPage("content://prov/doc/1", "guia.markdown");
        Assert.True(page.IsRendered);
    }

    [Fact]
    public async Task Links_OpenOutside_AndTheViewerNeverNavigates()
    {
        var page = new TextReaderPage(_h.Temp.File("a.md", "[x](https://example.com)"), "a.md");
        await page.Loading;
        var viewer = Field<SelectableHighlightedTextView>(page, "ContentViewer");

        Assert.False(viewer.HandleNavigation("about:blank"));
        Assert.True(viewer.HandleNavigation("file:///android_asset/otro.md"));
        Assert.True(viewer.HandleNavigation("https://example.com/"));

        // El aviso llega por el evento del visor y lo abre el Launcher (async void: se espera un poco).
        for (var i = 0; i < 50 && _h.Launcher.Opened.Count == 0; i++)
            await Task.Delay(10);
        Assert.Equal([new Uri("https://example.com/")], _h.Launcher.Opened);
        Assert.Empty(_h.Alerts);
    }

    [Fact]
    public async Task LinkThatCannotOpen_SaysSo()
    {
        _h.Launcher.Throws = new InvalidOperationException("sin navegador");
        var page = new TextReaderPage(_h.Temp.File("a.md", "x"), "a.md");
        await page.Loading;

        await page.OpenLinkAsync(new Uri("https://example.com"));

        var alert = Assert.Single(_h.Alerts);
        Assert.Equal("Error opening link", alert.Title);
    }

    [Fact]
    public async Task Theme_LightAndDark_AndFollowsTheChange()
    {
        var page = new TextReaderPage(_h.Temp.File("a.md", "x"), "a.md");
        await page.Loading;
        var viewer = Field<SelectableHighlightedTextView>(page, "ContentViewer");

        Assert.Equal("#FFFFFF", viewer.Palette.Background);
        Assert.Equal("#191C1D", viewer.Foreground);
        Assert.Contains("background: #FFFFFF", viewer.Html);

        _h.App.UserAppTheme = AppTheme.Dark;
        page.ApplyTheme();
        Assert.Equal("#201F27", viewer.Palette.Background);
        Assert.Equal("#E6E1E9", viewer.Foreground);
        Assert.Equal("#C3C0FF", viewer.Palette.Link);
        Assert.Equal("#2D2B36", viewer.Palette.CodeBackground);
        Assert.Contains("background: #201F27", viewer.Html);

        // Al aparecer se suscribe al cambio de tema y al irse lo suelta.
        Raise(page, "OnAppearing");
        _h.App.UserAppTheme = AppTheme.Light;
        Raise(page, "OnRequestedThemeChanged", _h.App, new AppThemeChangedEventArgs(AppTheme.Light));
        Assert.Equal("#FFFFFF", viewer.Palette.Background);
        Raise(page, "OnDisappearing");
    }

    [Fact]
    public async Task WithoutTheAppColors_UsesTheDefaultPalette()
    {
        Application.Current = AppHarness.AppWithout("CodeBackgroundLight", "LinkLight");
        var page = new TextReaderPage(_h.Temp.File("a.md", "x"), "a.md");
        await page.Loading;
        var viewer = Field<SelectableHighlightedTextView>(page, "ContentViewer");

        Assert.Equal(ReaderPalette.Default.CodeBackground, viewer.Palette.CodeBackground);
        Assert.Equal(ReaderPalette.Default.Link, viewer.Palette.Link);
        Assert.Equal("#FFFFFF", viewer.Palette.Background);
    }

    [Fact]
    public void MarkdownExtension_IsAcceptedFromOtherApps()
    {
        Assert.True(IntentFileHandler.IsSupported(".markdown"));
        Assert.True(IntentFileHandler.IsSupported(".md"));
    }
}
