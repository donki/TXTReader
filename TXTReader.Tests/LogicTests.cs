using System.Globalization;
using System.Text.Json;
using System.Xml.Linq;
using TXTReader.Services;

namespace TXTReader.Tests;

public class TextHighlighterTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void WithoutTerm_TheTextIsOnlyEncoded(string? term) =>
        Assert.Equal("a &lt;b&gt; &amp; &quot;c&quot;", TextHighlighter.ToHighlightedHtml("a <b> & \"c\"", term, false));

    [Fact]
    public void NullText_IsEmpty() =>
        Assert.Equal(string.Empty, TextHighlighter.ToHighlightedHtml(null, "x", false));

    [Fact]
    public void MarksEveryMatch_IgnoringCaseByDefault() =>
        Assert.Equal("<mark>Hola</mark> y <mark>hola</mark>", TextHighlighter.ToHighlightedHtml("Hola y hola", "hola", false));

    [Fact]
    public void CaseSensitive_MarksOnlyExactCase() =>
        Assert.Equal("Hola y <mark>hola</mark>", TextHighlighter.ToHighlightedHtml("Hola y hola", "hola", true));

    [Fact]
    public void TermIsLiteral_NotARegularExpression() =>
        Assert.Equal("a.b <mark>a*b</mark> (<mark>a*b</mark>)", TextHighlighter.ToHighlightedHtml("a.b a*b (a*b)", "a*b", false));

    [Fact]
    public void TermWithHtmlCharacters_IsFoundAndEncoded()
    {
        // Antes se buscaba en el texto ya codificado: «&» o «<» no se encontraban nunca.
        Assert.Equal("Tom <mark>&amp;</mark> Jerry", TextHighlighter.ToHighlightedHtml("Tom & Jerry", "&", false));
        Assert.Equal("<mark>&lt;div&gt;</mark>", TextHighlighter.ToHighlightedHtml("<div>", "<div>", false));
    }

    [Fact]
    public void TermInsideAnEntityName_DoesNotBreakTheEntity()
    {
        // Antes «amp» se marcaba dentro de «&amp;» y la pagina mostraba «&amp;» en vez de «&».
        Assert.Equal("Tom &amp; Jerry", TextHighlighter.ToHighlightedHtml("Tom & Jerry", "amp", false));
        Assert.Equal("&lt;<mark>lt</mark>&gt;", TextHighlighter.ToHighlightedHtml("<lt>", "lt", false));
    }

    [Fact]
    public void MatchesAcrossLines_AndAccents()
    {
        var html = TextHighlighter.ToHighlightedHtml("Canción\ncanción", "CANCIÓN", false);
        // WebUtility.HtmlEncode escribe los acentos como referencias numericas (&#243;).
        Assert.Equal("<mark>Canci&#243;n</mark>\n<mark>canci&#243;n</mark>", html);
    }

    [Fact]
    public void NoMatch_ReturnsTheEncodedText() =>
        Assert.Equal("abc &amp; def", TextHighlighter.ToHighlightedHtml("abc & def", "zzz", false));

    [Fact]
    public void Timeout_ReturnsTheTextWithoutHighlights_InsteadOfCrashing()
    {
        var text = string.Concat(Enumerable.Repeat("a<", 200_000));

        var html = TextHighlighter.ToHighlightedHtml(text, "a", false, TimeSpan.FromTicks(1));

        Assert.DoesNotContain("<mark>", html);
        Assert.StartsWith("a&lt;a&lt;", html);
    }

    [Fact]
    public void DefaultTimeout_IsOneSecond() =>
        Assert.Equal(TimeSpan.FromSeconds(1), TextHighlighter.SearchTimeout);
}

public sealed class RecentFilesServiceTests : IDisposable
{
    private readonly TempFolder _tmp = new();
    private readonly RecentFilesService _recent = new();

    public RecentFilesServiceTests() => SecureStorage.Clear();

    public void Dispose()
    {
        SecureStorage.Clear();
        _tmp.Dispose();
    }

    [Fact]
    public async Task StartsEmpty() => Assert.Empty(await _recent.GetRecentFilesAsync());

    [Fact]
    public async Task Add_PutsTheNewestFirst_WithNameAndTime()
    {
        var before = DateTime.Now;
        await _recent.AddRecentFileAsync("/a.txt", "a.txt");
        await _recent.AddRecentFileAsync("/b.txt", "b.txt");

        var files = await _recent.GetRecentFilesAsync();

        Assert.Equal(new[] { "/b.txt", "/a.txt" }, files.Select(f => f.FilePath));
        Assert.Equal("b.txt", files[0].FileName);
        Assert.InRange(files[0].LastOpened, before, DateTime.Now);
    }

    [Fact]
    public async Task Add_SameFileAgain_MovesItToTheTopWithoutDuplicates()
    {
        await _recent.AddRecentFileAsync("/a.txt", "a.txt");
        await _recent.AddRecentFileAsync("/b.txt", "b.txt");
        await _recent.AddRecentFileAsync("/a.txt", "a renamed.txt");

        var files = await _recent.GetRecentFilesAsync();

        Assert.Equal(new[] { "/a.txt", "/b.txt" }, files.Select(f => f.FilePath));
        Assert.Equal("a renamed.txt", files[0].FileName);
    }

    [Fact]
    public async Task KeepsOnlyTheLastFive()
    {
        for (var i = 1; i <= 7; i++)
            await _recent.AddRecentFileAsync($"/f{i}.txt", $"f{i}.txt");

        var files = await _recent.GetRecentFilesAsync();

        Assert.Equal(new[] { "/f7.txt", "/f6.txt", "/f5.txt", "/f4.txt", "/f3.txt" }, files.Select(f => f.FilePath));
    }

    [Fact]
    public async Task Remove_DropsOnlyThatFile_AndIgnoresUnknownPaths()
    {
        await _recent.AddRecentFileAsync("/a.txt", "a");
        await _recent.AddRecentFileAsync("/b.txt", "b");

        await _recent.RemoveRecentFileAsync("/a.txt");
        await _recent.RemoveRecentFileAsync("/ghost.txt");

        Assert.Equal(new[] { "/b.txt" }, (await _recent.GetRecentFilesAsync()).Select(f => f.FilePath));
    }

    [Fact]
    public async Task Clear_EmptiesTheList()
    {
        await _recent.AddRecentFileAsync("/a.txt", "a");
        await _recent.ClearRecentFilesAsync();
        Assert.Empty(await _recent.GetRecentFilesAsync());
    }

    [Fact]
    public async Task ValidFiles_DropsTheOnesThatNoLongerExist_AndSavesTheList()
    {
        var kept = _tmp.File("kept.txt");
        var gone = _tmp.File("gone.txt");
        await _recent.AddRecentFileAsync(gone, "gone.txt");
        await _recent.AddRecentFileAsync(kept, "kept.txt");
        File.Delete(gone);

        var valid = await _recent.GetValidRecentFilesAsync();

        Assert.Equal(new[] { kept }, valid.Select(f => f.FilePath));
        Assert.Equal(new[] { kept }, (await _recent.GetRecentFilesAsync()).Select(f => f.FilePath));
    }

    [Fact]
    public async Task ValidFiles_AllPresent_KeepsTheOrder()
    {
        var a = _tmp.File("a.txt");
        var b = _tmp.File("b.txt");
        await _recent.AddRecentFileAsync(a, "a");
        await _recent.AddRecentFileAsync(b, "b");

        Assert.Equal(new[] { b, a }, (await _recent.GetValidRecentFilesAsync()).Select(f => f.FilePath));
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("null")]
    [InlineData("{\"a\":1}")]
    public async Task CorruptStoredList_IsTreatedAsEmpty(string stored)
    {
        await SecureStorage.SetAsync("recent_files", stored);
        Assert.Empty(await _recent.GetRecentFilesAsync());
    }

    [Fact]
    public async Task StorageFailures_DoNotReachTheCaller()
    {
        await _recent.AddRecentFileAsync("/a.txt", "a");
        SecureStorage.FailWith = new InvalidOperationException("keystore broken");

        Assert.Empty(await _recent.GetRecentFilesAsync());
        await _recent.AddRecentFileAsync("/b.txt", "b"); // no lanza
        SecureStorage.FailWith = null;

        Assert.Equal(new[] { "/a.txt" }, (await _recent.GetRecentFilesAsync()).Select(f => f.FilePath));
    }

    [Fact]
    public async Task StoredFormat_IsAJsonListReadableByOlderVersions()
    {
        await _recent.AddRecentFileAsync("/a.txt", "a.txt");

        var json = await SecureStorage.GetAsync("recent_files");
        var doc = JsonDocument.Parse(json!);

        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
        Assert.Equal("/a.txt", doc.RootElement[0].GetProperty("FilePath").GetString());
    }
}

public sealed class LocalizationServiceTests : IDisposable
{
    private readonly CultureInfo _culture = CultureInfo.CurrentCulture;
    private readonly CultureInfo _uiCulture = CultureInfo.CurrentUICulture;

    private static readonly System.Reflection.FieldInfo DeviceCulture =
        typeof(LocalizationService).GetField("_deviceUICulture", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;

    private readonly object? _device = DeviceCulture.GetValue(null);

    public LocalizationServiceTests() => Preferences.Clear();

    private static void SetDevice(string culture) => DeviceCulture.SetValue(null, CultureInfo.GetCultureInfo(culture));

    public void Dispose()
    {
        DeviceCulture.SetValue(null, _device);
        Preferences.Clear();
        CultureInfo.CurrentCulture = _culture;
        CultureInfo.CurrentUICulture = _uiCulture;
    }

    private static string StringsFolder =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Resources", "Strings"));

    private static Dictionary<string, string> Resx(string file) =>
        XDocument.Load(Path.Combine(StringsFolder, file)).Root!
            .Elements("data")
            .ToDictionary(d => (string)d.Attribute("name")!, d => (string?)d.Element("value") ?? string.Empty);

    [Fact]
    public void SpanishAndEnglish_HaveTheSameKeys_NoEmptyTexts_AndTheSamePlaceholders()
    {
        var en = Resx("AppResources.resx");
        var es = Resx("AppResources.es.resx");
        var hole = new System.Text.RegularExpressions.Regex(@"\{\d+(:[^}]*)?\}");

        Assert.Equal(en.Keys.Order(), es.Keys.Order());
        Assert.True(en.Count >= 50);
        foreach (var (key, english) in en)
        {
            Assert.False(string.IsNullOrWhiteSpace(english), $"en:{key}");
            Assert.False(string.IsNullOrWhiteSpace(es[key]), $"es:{key}");
            Assert.Equal(hole.Matches(english).Select(m => m.Value).Order(), hole.Matches(es[key]).Select(m => m.Value).Order());
        }
    }

    [Fact]
    public void GetString_FollowsTheChosenLanguage()
    {
        var l = LocalizationService.Instance;
        var en = Resx("AppResources.resx");
        var es = Resx("AppResources.es.resx");

        l.SetLanguage("es");
        Assert.Equal(es["Error"], l.GetString("Error"));
        Assert.Equal("es", CultureInfo.CurrentUICulture.Name);

        l.SetLanguage("en");
        Assert.Equal(en["Error"], l.GetString("Error"));
    }

    [Fact]
    public void GetString_UnknownKey_IsEmpty_NeverTheKey()
    {
        LocalizationService.Instance.SetLanguage("es");
        Assert.Equal(string.Empty, LocalizationService.Instance.GetString("NoSuchKey"));
    }

    [Theory]
    [InlineData("es", "es", "es")]
    [InlineData("en", "en", "en")]
    [InlineData("fr", "en", "fr")]
    public void SetLanguage_SavesThePreference_AndUnsupportedFallsBackToEnglish(string code, string culture, string saved)
    {
        LocalizationService.Instance.SetLanguage(code);

        Assert.Equal(culture, CultureInfo.CurrentUICulture.Name);
        Assert.Equal(saved, Preferences.Get("app_language", ""));
        Assert.Equal(saved, LocalizationService.Instance.GetCurrentLanguageCode());
    }

    [Theory]
    [InlineData("es-ES", "es")]
    [InlineData("en-GB", "en")]
    [InlineData("de-DE", "en")]
    public void System_FollowsTheDeviceLanguage_WhenSupported(string device, string expected)
    {
        SetDevice(device);

        LocalizationService.Instance.SetLanguage("system");

        Assert.Equal(expected, CultureInfo.CurrentUICulture.Name);
    }

    [Fact]
    public void RaisesLanguageChanged()
    {
        var raised = 0;
        void Handler(object? s, EventArgs e) => raised++;
        LocalizationService.Instance.LanguageChanged += Handler;
        try
        {
            LocalizationService.Instance.SetLanguage("es");
            LocalizationService.Instance.ResetToSystemLanguage();
        }
        finally
        {
            LocalizationService.Instance.LanguageChanged -= Handler;
        }

        Assert.Equal(2, raised);
    }

    [Fact]
    public void Reset_ForgetsThePreference_AndGoesBackToTheDeviceLanguage()
    {
        // Movil en castellano: se elige ingles y luego «idioma del sistema». Antes se quedaba en
        // ingles, porque el «sistema» se leia de la cultura que la propia app acababa de cambiar.
        SetDevice("es-ES");
        LocalizationService.Instance.SetLanguage("en");

        LocalizationService.Instance.ResetToSystemLanguage();

        Assert.Equal("system", LocalizationService.Instance.GetCurrentLanguageCode());
        Assert.Equal("es", CultureInfo.CurrentUICulture.Name);
    }

    [Theory]
    [InlineData("es", "Spanish")]
    [InlineData("en", "English")]
    [InlineData("fr", "English")]
    [InlineData("", "SystemDefault")]
    public void CurrentLanguageName(string saved, string key)
    {
        var l = LocalizationService.Instance;
        l.SetLanguage("es");
        if (saved == "")
            Preferences.Remove("app_language");
        else
            Preferences.Set("app_language", saved);

        Assert.Equal(l.GetString(key), l.GetCurrentLanguageName());
    }

    [Theory]
    [InlineData("es-ES", "es")]
    [InlineData("en-US", "en")]
    [InlineData("it-IT", "en")]
    public void CurrentLanguageCode_IsTheLanguageOnScreen_EvenWhenFollowingTheSystem(string device, string expected)
    {
        // Sin preferencia guardada el codigo guardado es "system"; la pantalla Acerca de marcaba
        // «English» en un movil en castellano. CurrentLanguageCode da el idioma que se ve.
        SetDevice(device);
        LocalizationService.Instance.ResetToSystemLanguage();

        Assert.Equal("system", LocalizationService.Instance.GetCurrentLanguageCode());
        Assert.Equal(expected, LocalizationService.Instance.CurrentLanguageCode);
    }

    private static LocalizationService NewInstance() =>
        (LocalizationService)Activator.CreateInstance(typeof(LocalizationService), nonPublic: true)!;

    [Theory]
    [InlineData("es", "de-DE", "es")]
    [InlineData("es", "en-US", "es")]
    [InlineData("en", "es-ES", "en")]
    [InlineData("", "es-ES", "es")]
    [InlineData("system", "en-GB", "en")]
    [InlineData("fr", "es-ES", "es")]
    [InlineData("fr", "de-DE", "en")]
    public void Startup_UsesTheSavedLanguage_OrTheDeviceOne(string saved, string device, string expected)
    {
        // Arranque de la app: la preferencia guardada manda; sin ella, o si no se entiende, el
        // idioma del dispositivo (castellano o ingles; cualquier otro, ingles).
        SetDevice(device);
        if (saved != "")
            Preferences.Set("app_language", saved);

        var l = NewInstance();

        Assert.Equal(expected, l.CurrentLanguageCode);
        Assert.Equal(expected, CultureInfo.CurrentUICulture.Name);
        Assert.Equal(Resx(expected == "es" ? "AppResources.es.resx" : "AppResources.resx")["Error"], l.GetString("Error"));
    }

    [Fact]
    public void AvailableLanguages_AreSystemEnglishSpanish()
    {
        var options = LocalizationService.Instance.GetAvailableLanguages();

        Assert.Equal(new[] { "system", "en", "es" }, options.Select(o => o.Code));
        Assert.All(options, o => Assert.False(string.IsNullOrEmpty(o.Name)));
    }
}

public class FileIntentServiceTests
{
    [Fact]
    public void NotifyFileOpened_ReachesSubscribers_AndWorksWithoutThem()
    {
        FileIntentService.NotifyFileOpened("/nobody/listening.txt");

        var received = new List<string>();
        void Handler(string path) => received.Add(path);
        FileIntentService.FileOpened += Handler;
        try
        {
            FileIntentService.NotifyFileOpened("/a.txt");
        }
        finally
        {
            FileIntentService.FileOpened -= Handler;
        }

        Assert.Equal(new[] { "/a.txt" }, received);
    }
}
