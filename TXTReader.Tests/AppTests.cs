using System.Text;
using Microsoft.Maui.Devices;
using TXTReader.Controls;
using TXTReader.Pages;
using TXTReader.Services;

namespace TXTReader.Tests;

/// <summary>Apertura de ficheros que llegan de otra app (lo que antes estaba dentro de MainActivity).</summary>
public sealed class IntentFileHandlerTests : IDisposable
{
    private readonly AppHarness _h = new();

    public void Dispose() => _h.Dispose();

    [Theory]
    [InlineData("file:///sdcard/a.txt", "file", "/sdcard/a.txt", "/sdcard/a.txt")]
    [InlineData("FILE:///sdcard/a.txt", "FILE", "/sdcard/a.txt", "/sdcard/a.txt")]
    [InlineData("content://media/doc/7", "content", "/doc/7", "content://media/doc/7")]
    [InlineData("other:/x/y.txt", "other", "/x/y.txt", "/x/y.txt")]
    [InlineData("weird", null, null, null)]
    public void ReadableReference_DependsOnTheScheme(string uri, string? scheme, string? path, string? expected) =>
        Assert.Equal(expected, IntentFileHandler.GetReadableReference(uri, scheme, path));

    [Theory]
    [InlineData("Notas.TXT", "content://x/1", ".txt")]
    [InlineData(null, "/sdcard/ruta.LOG", ".log")]
    [InlineData("  ", "/sdcard/ruta.md", ".md")]
    [InlineData("sin_extension", "/sdcard/a.json", "")]
    public void Extension_ComesFromTheDisplayName_OrThePath(string? name, string path, string expected) =>
        Assert.Equal(expected, IntentFileHandler.GetExtension(name, path));

    [Theory]
    [InlineData(".txt", true)]
    [InlineData(".GPX", true)]
    [InlineData(".conf", true)]
    [InlineData("", true)]
    [InlineData(".pdf", false)]
    [InlineData(".jpg", false)]
    public void Supported_AreTextExtensions_OrNone(string extension, bool expected) =>
        Assert.Equal(expected, IntentFileHandler.IsSupported(extension));

    [Theory]
    [InlineData("content://com.microsoft.skydrive.content.StorageAccessProvider/OneDrive/x", "CloudOneDrive")]
    [InlineData("content://com.google.android.apps.docs.storage/document/acc%3D1/DRIVE.GOOGLE", "CloudGoogleDrive")]
    [InlineData("content://com.dropbox.android.FileCache/x", "CloudStorage")]
    [InlineData("content://otra.app/x", "CloudGeneric")]
    public void CloudHint_IsChosenFromTheUri(string uri, string key) =>
        Assert.Equal(key, IntentFileHandler.CloudHintKey(uri));

    [Fact]
    public async Task SupportedFile_IsLeftPending_AndOpenedOnResume()
    {
        var file = _h.Temp.File("notas.txt", "hola");
        var received = new List<string>();
        FileIntentService.FileOpened += received.Add;
        var handler = new IntentFileHandler();

        await handler.HandleViewIntentAsync("file://" + file, "file", file, "notas.txt");
        Assert.Equal(file, handler.PendingFilePath);

        await handler.ProcessPendingAsync();

        Assert.Null(handler.PendingFilePath);
        Assert.Equal(new[] { file }, received);
        Assert.Equal(new[] { IntentFileHandler.PendingDelay }, _h.Delays);
        Assert.Empty(_h.Alerts);
    }

    [Fact]
    public async Task ContentUri_IsOpenedWithoutCheckingTheDisk()
    {
        var received = new List<string>();
        FileIntentService.FileOpened += received.Add;
        var handler = new IntentFileHandler();

        await handler.HandleViewIntentAsync("content://prov/doc/9", "content", "/doc/9", null);
        await handler.ProcessPendingAsync();

        Assert.Equal(new[] { "content://prov/doc/9" }, received);
    }

    [Fact]
    public async Task UnsupportedFile_IsIgnored()
    {
        var handler = new IntentFileHandler();

        await handler.HandleViewIntentAsync("file:///sdcard/foto.jpg", "file", "/sdcard/foto.jpg", "foto.jpg");
        await handler.ProcessPendingAsync();

        Assert.Null(handler.PendingFilePath);
        Assert.Empty(_h.Delays);
        Assert.Contains("Unsupported file type: .jpg", await TestLog.WaitForAsync("Unsupported"));
    }

    [Fact]
    public async Task NothingPending_DoesNothing()
    {
        await new IntentFileHandler().ProcessPendingAsync();

        Assert.Empty(_h.Delays);
        Assert.Empty(_h.Alerts);
    }

    [Fact]
    public async Task MissingLocalFile_ShowsTheLocalizedNotice()
    {
        // Antes este aviso salia en castellano aunque la app estuviera en ingles.
        _h.CurrentPage = new ContentPage();
        var handler = new IntentFileHandler();
        await handler.HandleViewIntentAsync("file:///sdcard/no.txt", "file", _h.Temp.Combine("no.txt"), "no.txt");

        await handler.ProcessPendingAsync();

        var shown = Assert.Single(_h.Alerts);
        Assert.Equal("File not accessible", shown.Title);
        Assert.Equal("OK", shown.Accept);
        Assert.Equal(new[] { IntentFileHandler.PendingDelay, IntentFileHandler.MissingFileDelay }, _h.Delays);
    }

    [Fact]
    public async Task NoReadableReference_ShowsTheCloudHint_InTheAppLanguage()
    {
        LocalizationService.Instance.SetLanguage("es");
        _h.CurrentPage = new ContentPage();

        await new IntentFileHandler().HandleViewIntentAsync("content://com.dropbox.android/x", "dropbox", null, null);

        var shown = Assert.Single(_h.Alerts);
        Assert.Equal("No se puede abrir el fichero", shown.Title);
        Assert.Equal(new[] { IntentFileHandler.CloudHintDelay }, _h.Delays);
    }

    [Fact]
    public async Task CloudHint_WithoutPage_IsNotShown()
    {
        await new IntentFileHandler().HandleViewIntentAsync("x", null, null, null);

        Assert.Empty(_h.Alerts);
    }

    [Fact]
    public async Task Errors_AreLogged_NotThrown()
    {
        AppPlatform.Delay = _ => throw new InvalidOperationException("roto");
        var handler = new IntentFileHandler();

        await handler.HandleViewIntentAsync("x", null, null, null);
        await handler.HandleViewIntentAsync("content://a/b", "content", null, null);
        await handler.ProcessPendingAsync();

        var log = await TestLog.WaitForAsync("OnResume: ERROR");
        Assert.Contains("HandleIntent: ERROR - roto", log);
        Assert.Contains("OnResume: ERROR - roto", log);
    }
}

public sealed class PickedFilesTests : IDisposable
{
    private readonly TempFolder _tmp = new();

    public void Dispose() => _tmp.Dispose();

    [Fact]
    public async Task WithAPath_ThePathIsUsed_AndNothingIsCopied()
    {
        var opened = false;

        var path = await PickedFiles.GetReadablePathAsync("/sdcard/a.txt", "a.txt", () => { opened = true; return Task.FromResult<Stream>(new MemoryStream()); }, _tmp.Path);

        Assert.Equal("/sdcard/a.txt", path);
        Assert.False(opened);
        Assert.False(Directory.Exists(_tmp.Combine("picked_files")));
    }

    [Fact]
    public async Task WithoutAPath_TheContentIsCopiedToTheCache()
    {
        var path = await PickedFiles.GetReadablePathAsync(null, "notas de: hoy?.txt",
            () => Task.FromResult<Stream>(new MemoryStream(Encoding.UTF8.GetBytes("contenido"))), _tmp.Path);

        Assert.Equal(_tmp.Combine("picked_files"), Path.GetDirectoryName(path));
        Assert.EndsWith("_notas de_ hoy_.txt", Path.GetFileName(path));
        Assert.Equal("contenido", await File.ReadAllTextAsync(path));
    }

    [Theory]
    [InlineData("")]
    [InlineData("///")]
    public async Task WithoutAUsableName_ANameIsMadeUp(string name)
    {
        var path = await PickedFiles.GetReadablePathAsync("  ", name, () => Task.FromResult<Stream>(new MemoryStream([1, 2])), _tmp.Path);

        Assert.Matches(@"^\d+_selected_file_\d+\.txt$", Path.GetFileName(path));
        Assert.Equal(new byte[] { 1, 2 }, await File.ReadAllBytesAsync(path));
    }

    [Theory]
    [InlineData("a/b\\c.txt", "a_b_c.txt")]
    [InlineData("<x>|y*.log", "x_y_.log")]
    [InlineData(null, "")]
    [InlineData("normal.txt", "normal.txt")]
    public void SafeFileName_DropsForbiddenCharacters(string? name, string expected) =>
        Assert.Equal(expected, PickedFiles.SafeFileName(name));

    [Fact]
    public void FileTypes_AreTextOnly()
    {
        var types = PickedFiles.FileTypes();

        Assert.DoesNotContain("*/*", PickedFiles.AndroidMimeTypes);
        Assert.Contains("application/gpx+xml", PickedFiles.AndroidMimeTypes);
        Assert.NotNull(types);
    }
}

public sealed class UpdateServiceTests : IDisposable
{
    private readonly AppHarness _h = new();

    public void Dispose() => _h.Dispose();

    private static UpdateService With(string json) => new(_ => Task.FromResult(json));

    [Theory]
    [InlineData("2026.10.02.0", "2026.10.01.0", 1)]
    [InlineData("2026.10.01.0", "2026.10.01.0", 0)]
    [InlineData("2026.9.30.0", "2026.10.01.0", -1)]
    [InlineData("2026.10.01", "2026.10.01.0", 0)]
    [InlineData("2026.10.01.1", "2026.10.01", 1)]
    [InlineData("2026.x.1", "2026.0.1", 0)]
    public void CompareVersions_IsNumericByParts(string a, string b, int sign) =>
        Assert.Equal(sign, Math.Sign(UpdateService.CompareVersions(a, b)));

    [Fact]
    public async Task NewerVersion_AsksInTheAppLanguage_AndOpensTheLink()
    {
        LocalizationService.Instance.SetLanguage("es");
        _h.AlertAnswer = true;
        var page = new ContentPage();

        await With("""{"version":"2026.12.01.0","url":"https://example.com/txt"}""").CheckAndPromptAsync(page);

        var shown = Assert.Single(_h.Alerts);
        Assert.Equal("Actualización disponible", shown.Title);
        Assert.Contains("2026.12.01.0", shown.Message);
        Assert.Contains("2026.10.01.0", shown.Message);
        Assert.Equal(("Actualizar", "Ahora no"), (shown.Accept, shown.Cancel));
        Assert.Equal(new Uri("https://example.com/txt"), Assert.Single(_h.Browser.Opened));
    }

    [Fact]
    public async Task Declined_OrWithoutLink_OpensNothing()
    {
        await With("""{"version":"2027.1.1.0","url":"https://example.com"}""").CheckAndPromptAsync(new ContentPage());
        _h.AlertAnswer = true;
        await With("""{"version":"2027.1.1.0"}""").CheckAndPromptAsync(new ContentPage());

        Assert.Equal(2, _h.Alerts.Count);
        Assert.Empty(_h.Browser.Opened);
    }

    [Theory]
    [InlineData("""{"version":"2026.10.01.0"}""")]
    [InlineData("""{"version":"2020.1.1.0"}""")]
    [InlineData("""{"url":"https://example.com"}""")]
    [InlineData("no es json")]
    public async Task UpToDate_OrBadManifest_SaysNothing(string json)
    {
        await With(json).CheckAndPromptAsync(new ContentPage());

        Assert.Empty(_h.Alerts);
    }

    [Fact]
    public async Task WithoutNetwork_SaysNothing()
    {
        await new UpdateService(_ => throw new HttpRequestException("sin red")).CheckAndPromptAsync(new ContentPage());

        Assert.Empty(_h.Alerts);
    }

    [Fact]
    public async Task OnlyOnePerSession()
    {
        var calls = 0;
        var service = new UpdateService(_ => { calls++; return Task.FromResult("{}"); });

        await service.CheckAndPromptAsync(new ContentPage());
        await service.CheckAndPromptAsync(new ContentPage());

        Assert.Equal(1, calls);
    }

    [Fact]
    public void DefaultConstructor_UsesTheNetwork_ButDoesNotCallIt() =>
        Assert.NotNull(new UpdateService());
}

public sealed class MobileLogServiceTests : IDisposable
{
    private readonly TempFolder _tmp = new();

    public void Dispose()
    {
        TestLog.Reset();
        _tmp.Dispose();
    }

    [Fact]
    public async Task Appends_WithTimestamp()
    {
        MobileLogService.LogFilePath = _tmp.Combine("debug.log");

        await MobileLogService.LogAsync("uno");
        await MobileLogService.LogAsync("dos");

        var lines = await File.ReadAllLinesAsync(MobileLogService.LogFilePath);
        Assert.Equal(2, lines.Length);
        Assert.Matches(@"^\[\d{4}-\d\d-\d\d \d\d:\d\d:\d\d\.\d{3}\] uno$", lines[0]);
        Assert.EndsWith("] dos", lines[1]);
    }

    [Fact]
    public async Task UnwritableLog_IsIgnored()
    {
        MobileLogService.LogFilePath = _tmp.Combine("no", "existe", "debug.log");

        await MobileLogService.LogAsync("nada");

        Assert.False(File.Exists(MobileLogService.LogFilePath));
    }
}

public sealed class ContentUriReadingTests : IDisposable
{
    private readonly AppHarness _h = new();

    public void Dispose() => _h.Dispose();

    [Fact]
    public async Task ContentUri_IsReadThroughThePlatform_WithEncodingDetection()
    {
        string? asked = null;
        AppPlatform.OpenContentUri = uri => { asked = uri; return new MemoryStream([0xEF, 0xBB, 0xBF, .. Encoding.UTF8.GetBytes("ñandú")]); };

        var (content, encoding) = await EncodingDetectionService.ReadFileWithEncodingDetectionAsync("content://prov/doc/1");

        Assert.Equal("content://prov/doc/1", asked);
        Assert.Equal("ñandú", content);
        Assert.Equal(Encoding.UTF8.EncodingName, encoding);
    }

    [Fact]
    public async Task ContentUri_WithoutStream_Fails()
    {
        AppPlatform.OpenContentUri = _ => null;

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            EncodingDetectionService.ReadFileWithEncodingDetectionAsync("content://prov/doc/2"));
        Assert.Contains("inputStream is null", ex.Message);
    }
}

/// <summary>El visor de texto: HTML con el texto escapado, el resaltado, los colores y el zoom.</summary>
public sealed class SelectableHighlightedTextViewTests
{
    private static string Html(SelectableHighlightedTextView view) =>
        Assert.IsType<HtmlWebViewSource>(Assert.IsType<WebView>(view.Content).Source).Html;

    [Fact]
    public void Defaults_GiveAnEmptyMonospacePage()
    {
        var view = new SelectableHighlightedTextView();

        var html = Html(view);
        Assert.Contains("<body></body>", html);
        Assert.Contains("font-family: Consolas, Monaco, 'Courier New', monospace;", html);
        Assert.Contains("font-size: 14px;", html);
        Assert.Contains("color: #222;", html);
        Assert.Contains("background: #ffff00;", html);
        Assert.Equal(14d, view.FontSize);
        Assert.Equal(1.0, view.Zoom);
        Assert.False(view.CaseSensitive);
        Assert.Null(view.FontFamily);
        Assert.Equal(1.4, view.LineHeight);
    }

    [Fact]
    public void EveryProperty_RebuildsThePage()
    {
        var view = new SelectableHighlightedTextView
        {
            Text = "Hola <b> hola",
            SearchTerm = "hola",
            CaseSensitive = true,
            FontFamily = "Monospace",
            Foreground = "#111111",
            HighlightTextColor = "#000001",
            HighlightBackgroundColor = "#FFFF01",
            LineHeight = 2,
            FontSize = 10,
            Zoom = 2,
        };

        var html = Html(view);
        Assert.Contains("<body>Hola &lt;b&gt; <mark>hola</mark></body>", html);
        Assert.Contains("font-family: Monospace;", html);
        Assert.Contains("color: #111111;", html);
        Assert.Contains("color: #000001;", html);
        Assert.Contains("background: #FFFF01;", html);
        Assert.Contains("line-height: 2;", html);
        Assert.Contains("font-size: 20px;", html);
        Assert.Equal("Hola <b> hola", view.Text);
        Assert.Equal("hola", view.SearchTerm);
        Assert.Equal("#111111", view.Foreground);
        Assert.Equal("#000001", view.HighlightTextColor);
        Assert.Equal("#FFFF01", view.HighlightBackgroundColor);
    }

    [Theory]
    [InlineData(20, 0.1, "10")]   // el zoom no baja de 0,5 ...
    [InlineData(14, 10, "64")]    // ... y la letra se queda entre 8 y 64 px
    [InlineData(4, 1, "8")]
    public void FontSize_IsClamped(double size, double zoom, string px)
    {
        var view = new SelectableHighlightedTextView { FontSize = size, Zoom = zoom };

        Assert.Contains($"font-size: {px}px;", Html(view));
    }

    [Fact]
    public void CssNumbers_UseADot_EvenInSpanish()
    {
        // Con la cultura en castellano salia «font-size: 15,5px» y el navegador lo descartaba.
        var previous = System.Globalization.CultureInfo.CurrentCulture;
        System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("es-ES");
        try
        {
            var view = new SelectableHighlightedTextView { FontSize = 14, Zoom = 15.5 / 14, LineHeight = 1.4 };

            var html = Html(view);
            Assert.Contains("font-size: 15.5px;", html);
            Assert.Contains("line-height: 1.4;", html);
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = previous;
        }
    }
}
