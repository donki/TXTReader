using System.Reflection;
using Microsoft.Maui.Devices;
using TXTReader.Pages;
using TXTReader.Services;

namespace TXTReader.Tests;

/// <summary>
/// Las paginas con su XAML real, sin dispositivo: textos en los dos idiomas, botones, navegacion y
/// avisos. Lo que pediria el dispositivo (selector, correo, navegador...) lo responden los dobles.
/// </summary>
public sealed class MainPageTests : IDisposable
{
    private readonly AppHarness _h = new();

    public void Dispose() => _h.Dispose();

    private static T Field<T>(object page, string name) =>
        (T)page.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(page)!;

    private static void Raise(object target, string handler, params object?[] args) =>
        target.GetType().GetMethod(handler, BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(target, args);

    private (MainPage Page, NavigationPage Nav) Open()
    {
        var page = new MainPage();
        var nav = new NavigationPage(page);
        _h.CurrentPage = nav;
        return (page, nav);
    }

    [Fact]
    public void Texts_FollowTheLanguage()
    {
        var (page, _) = Open();
        var changed = new List<string?>();
        ((System.ComponentModel.INotifyPropertyChanged)page).PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        Assert.Equal("TXT Reader", page.Title);
        Assert.Equal("Select File", Field<Button>(page, "SelectFileBtn").Text);
        Assert.Equal("Recent Files", Field<Label>(page, "RecentFilesLabel").Text);
        Assert.Equal("About", Field<Button>(page, "AboutBtn").Text);
        Assert.Equal("No recent files", page.NoRecentFilesText);

        LocalizationService.Instance.SetLanguage("es");

        Assert.Equal("Seleccionar Archivo", Field<Button>(page, "SelectFileBtn").Text);
        Assert.Equal("Acerca de", Field<Button>(page, "AboutBtn").Text);
        Assert.Contains(nameof(MainPage.NoRecentFilesText), changed);
    }

    [Fact]
    public async Task RecentFiles_ListOnlyFilesThatStillExist()
    {
        var kept = _h.Temp.File("kept.txt", "a");
        var service = new RecentFilesService();
        await service.AddRecentFileAsync(_h.Temp.Combine("gone.txt"), "gone.txt");
        await service.AddRecentFileAsync(kept, "kept.txt");

        var (page, _) = Open();
        await page.LoadRecentFiles();

        Assert.Equal(new[] { "kept.txt" }, page.RecentFiles.Select(f => f.FileName));
        Assert.Same(page, page.BindingContext);
    }

    [Fact]
    public async Task PickingAFile_OpensTheReader_AndRemembersIt()
    {
        var file = _h.Temp.File("notas.txt", "hola mundo");
        _h.Picker.Result = new FileResult(file);
        var (page, nav) = Open();

        await page.SelectFileAsync();

        var reader = Assert.IsType<TextReaderPage>(nav.CurrentPage);
        await reader.Loading;
        Assert.Equal("notas.txt", reader.Title);
        Assert.Equal("Select a text file", _h.Picker.LastOptions!.PickerTitle);
        Assert.Equal(new[] { file }, page.RecentFiles.Select(f => f.FilePath));
        Assert.Empty(_h.Alerts);
    }

    [Fact]
    public void SelectButton_RunsThePicker()
    {
        var (page, _) = Open();

        Field<Button>(page, "SelectFileBtn").SendClicked();

        Assert.NotNull(_h.Picker.LastOptions);
        Assert.Empty(_h.Alerts);   // se cancelo el selector: nada que hacer
    }

    [Fact]
    public async Task PickerError_IsShown()
    {
        _h.Picker.Throws = new InvalidOperationException("sin permiso");
        var (page, _) = Open();

        await page.SelectFileAsync();

        var shown = Assert.Single(_h.Alerts);
        Assert.Equal("Error", shown.Title);
        Assert.Equal("Error selecting file: sin permiso", shown.Message);
    }

    [Fact]
    public async Task OpeningAMissingFile_SaysSo()
    {
        var (page, nav) = Open();

        await page.OpenFile(_h.Temp.Combine("no.txt"), "no.txt");

        Assert.Equal("The file does not exist or cannot be accessed.", Assert.Single(_h.Alerts).Message);
        Assert.Same(page, nav.CurrentPage);
    }

    [Fact]
    public async Task OpeningFails_TheErrorIsShown()
    {
        var file = _h.Temp.File("a.txt");
        var (page, _) = Open();
        // Sin los estilos de la app el lector no se puede construir: el fallo se avisa.
        Application.Current = null;

        await page.OpenFile(file, "a.txt");

        Assert.StartsWith("Error opening file: ", Assert.Single(_h.Alerts).Message);
    }

    [Fact]
    public async Task RecentFile_OpensIt_OrForgetsIt_WhenItIsGone()
    {
        var kept = _h.Temp.File("kept.txt", "a");
        var service = new RecentFilesService();
        await service.AddRecentFileAsync(kept, "kept.txt");
        var (page, nav) = Open();

        await page.OpenRecentAsync(null);
        Assert.Same(page, nav.CurrentPage);

        await page.OpenRecentAsync(new RecentFile { FilePath = kept, FileName = "kept.txt" });
        Assert.IsType<TextReaderPage>(nav.CurrentPage);
        await nav.PopAsync();

        File.Delete(kept);
        await page.OpenRecentAsync(new RecentFile { FilePath = kept, FileName = "kept.txt" });

        var shown = Assert.Single(_h.Alerts);
        Assert.Equal("File deleted", shown.Title);
        Assert.Empty(page.RecentFiles);
        Assert.Empty(await service.GetRecentFilesAsync());
    }

    [Fact]
    public void TappingARecentFile_UsesTheRowContext()
    {
        var file = _h.Temp.File("t.txt", "a");
        var (page, nav) = Open();

        Raise(page, "OnRecentFileSelected", new Grid { BindingContext = new RecentFile { FilePath = file, FileName = "t.txt" } }, new TappedEventArgs(null));

        Assert.IsType<TextReaderPage>(nav.CurrentPage);
    }

    [Fact]
    public async Task FileFromAnotherApp_IsOpened_WithoutReloadingTheList()
    {
        var file = _h.Temp.File("compartido.log", "x");
        var (page, nav) = Open();

        FileIntentService.NotifyFileOpened(file);
        await TestLog.WaitForAsync("Navigation to TextReaderPage completed");

        var reader = Assert.IsType<TextReaderPage>(nav.CurrentPage);
        Assert.Equal("compartido.log", reader.Title);
        Assert.Empty(page.RecentFiles);   // la lista no se recarga para un intent ...
        Assert.Single(await new RecentFilesService().GetRecentFilesAsync());   // ... pero se recuerda
    }

    [Fact]
    public async Task FileFromAnotherApp_Errors_AreLogged()
    {
        var (page, _) = Open();
        AppPlatform.RunOnMainThreadAsync = _ => throw new InvalidOperationException("sin hilo");

        page.OnFileOpenedFromIntent("/x.txt");

        Assert.Contains("ERROR in FileOpened event: sin hilo", await TestLog.WaitForAsync("sin hilo"));
    }

    [Fact]
    public async Task Appearing_ReloadsTheList_AndChecksForUpdates()
    {
        var asked = 0;
        var (page, _) = Open();
        page.UpdateServiceProvider = () => new UpdateService(_ => { asked++; return Task.FromResult("{}"); });

        await page.OnAppearingAsync();
        page.UpdateServiceProvider = () => null;
        await page.OnAppearingAsync();

        Assert.Equal(1, asked);
    }

    [Fact]
    public void Appearing_WithoutServices_DoesNotFail()
    {
        var (page, _) = Open();

        page.GetType().GetMethod("OnAppearing", BindingFlags.NonPublic | BindingFlags.Instance, Type.EmptyTypes)!.Invoke(page, null);

        Assert.Null(page.UpdateServiceProvider());
    }

    [Fact]
    public void AboutButton_OpensAbout()
    {
        var (page, nav) = Open();

        Field<Button>(page, "AboutBtn").SendClicked();

        Assert.IsType<AboutPage>(nav.CurrentPage);
    }
}

public sealed class TextReaderPageTests : IDisposable
{
    private readonly AppHarness _h = new();

    public void Dispose() => _h.Dispose();

    private static T Field<T>(object page, string name) =>
        (T)page.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(page)!;

    [Fact]
    public async Task ShowsTheFile_WithTheThemeColors_AndSearchFollowsTheEntry()
    {
        var file = _h.Temp.File("a.txt", "uno dos uno");

        var page = new TextReaderPage(file, "a.txt");
        await page.Loading;

        var viewer = Field<TXTReader.Controls.SelectableHighlightedTextView>(page, "ContentViewer");
        Assert.Equal("a.txt", page.Title);
        Assert.Equal("uno dos uno", viewer.Text);
        Assert.Equal(14, Field<Slider>(page, "ZoomSlider").Value);
        Assert.Equal("Search in text...", Field<Entry>(page, "SearchEntry").Placeholder);
        Assert.Matches("^#[0-9A-F]{6,8}$", viewer.HighlightBackgroundColor);
        Assert.NotEqual("#000000", viewer.HighlightBackgroundColor);

        Field<Entry>(page, "SearchEntry").Text = "uno";
        Assert.Equal("uno", viewer.SearchTerm);

        LocalizationService.Instance.SetLanguage("es");
        Assert.Equal("Buscar en el texto...", Field<Entry>(page, "SearchEntry").Placeholder);
    }

    [Fact]
    public async Task Slider_ZoomsTheText()
    {
        var page = new TextReaderPage(_h.Temp.File("a.txt", "x"), "a.txt");
        await page.Loading;

        Field<Slider>(page, "ZoomSlider").Value = 28;

        Assert.Equal(2.0, Field<TXTReader.Controls.SelectableHighlightedTextView>(page, "ContentViewer").Zoom);
    }

    [Fact]
    public async Task WithoutTheAppColors_FallsBackToBlack()
    {
        Application.Current = AppHarness.AppWithout("HighlightBackground", "Black");

        var page = new TextReaderPage(_h.Temp.File("a.txt", "x"), "a.txt");
        await page.Loading;

        var viewer = Field<TXTReader.Controls.SelectableHighlightedTextView>(page, "ContentViewer");
        Assert.Equal("#000000", viewer.HighlightTextColor);
        Assert.Equal("#000000", viewer.HighlightBackgroundColor);
    }

    [Fact]
    public async Task UnreadableFile_SaysSo_AndGoesBack()
    {
        var root = new ContentPage();
        var nav = new NavigationPage(root);
        var page = new TextReaderPage(_h.Temp.Combine("no.txt"), "no.txt");
        await nav.PushAsync(page);
        await page.Loading;

        // La carga empezo antes de apilarse: se repite ya apilada para ver la vuelta atras.
        var again = new TextReaderPage(_h.Temp.Combine("no.txt"), "no.txt");
        await nav.PushAsync(again);
        await again.Loading;

        Assert.All(_h.Alerts, a => Assert.StartsWith("Could not load file: ", a.Message));
        Assert.NotEmpty(_h.Alerts);
    }
}

public sealed class AboutPageTests : IDisposable
{
    private readonly AppHarness _h = new();

    public void Dispose() => _h.Dispose();

    private static T Field<T>(object page, string name) =>
        (T)page.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(page)!;

    private static Style? AppStyle(string key) =>
        Application.Current!.Resources.TryGetValue(key, out var s) ? (Style)s : null;

    [Fact]
    public void Texts_Version_AndTheLanguageButtons()
    {
        var page = new AboutPage();

        Assert.Equal("About", page.Title);
        Assert.Contains("2026.10.01.0", Field<Label>(page, "VersionLabel").Text);
        Assert.Same(AppStyle("PrimaryButton"), Field<Button>(page, "EnglishButton").Style);
        Assert.Same(AppStyle("OutlineButton"), Field<Button>(page, "SpanishButton").Style);

        Field<Button>(page, "SpanishButton").SendClicked();

        Assert.Equal("es", LocalizationService.Instance.CurrentLanguageCode);
        Assert.Equal("Acerca de", page.Title);
        Assert.Same(AppStyle("PrimaryButton"), Field<Button>(page, "SpanishButton").Style);
        Assert.False(string.IsNullOrEmpty(Field<Label>(page, "WarningTextLabel").Text));

        // Pulsar el idioma que ya esta no hace nada; el otro vuelve al ingles.
        Field<Button>(page, "SpanishButton").SendClicked();
        Assert.Equal("es", Preferences.Get("app_language", ""));
        Field<Button>(page, "EnglishButton").SendClicked();
        Assert.Equal("About", page.Title);
    }

    [Fact]
    public async Task Contact_ComposesAnEmail()
    {
        await new AboutPage().ContactByEmailAsync();

        var mail = _h.Email.Sent!;
        Assert.Equal(new[] { "jsoladelarosa@gmail.com" }, mail.To);
        Assert.Equal("Contacto desde TXT Reader", mail.Subject);
        Assert.Contains("TXT Reader 2026.10.01.0", mail.Body);
        Assert.Contains("Android 16", mail.Body);
        Assert.Empty(_h.Alerts);
    }

    [Fact]
    public void ContactButton_ComposesAnEmail()
    {
        var page = new AboutPage();
        var named = new[] { "SpanishButton", "EnglishButton", "BackButton" }.Select(n => Field<Button>(page, n)).ToList();
        var button = page.GetVisualTreeDescendants().OfType<Button>().Single(b => !named.Contains(b));

        button.SendClicked();

        Assert.NotNull(_h.Email.Sent);
    }

    [Fact]
    public async Task WithoutEmailApp_OnAndroid_UsesMailto()
    {
        _h.Email.Throws = new InvalidOperationException("sin correo");

        await new AboutPage().ContactByEmailAsync();

        var uri = Assert.Single(_h.Launcher.Opened);
        Assert.StartsWith("mailto:jsoladelarosa@gmail.com?subject=Contacto%20desde%20TXT%20Reader&body=", uri.OriginalString);
    }

    [Fact]
    public async Task WithoutEmailApp_ElsewhereSaysSo_InTheAppLanguage()
    {
        LocalizationService.Instance.SetLanguage("es");
        _h.Email.Throws = new InvalidOperationException("sin correo");
        _h.DeviceInfo.Platform = DevicePlatform.WinUI;

        await new AboutPage().ContactByEmailAsync();

        var shown = Assert.Single(_h.Alerts);
        Assert.Equal("Error", shown.Title);
        Assert.Equal("El cliente de correo no está disponible en este dispositivo.", shown.Message);
        Assert.Equal("OK", shown.Accept);
    }

    [Fact]
    public async Task NotSupported_CopiesTheAddress()
    {
        _h.Email.Throws = new InvalidOperationException("sin correo");
        _h.Launcher.Throws = new FeatureNotSupportedException();

        await new AboutPage().ContactByEmailAsync();

        Assert.Equal("jsoladelarosa@gmail.com", _h.Clipboard.Text);
        var shown = Assert.Single(_h.Alerts);
        Assert.Equal("Email copied", shown.Title);
        Assert.Equal("Email copied to the clipboard: jsoladelarosa@gmail.com", shown.Message);
    }

    [Fact]
    public async Task OtherFailure_CopiesTheAddress_AndExplains()
    {
        _h.Email.Throws = new InvalidOperationException("sin correo");
        _h.Launcher.Throws = new InvalidOperationException("sin navegador");

        await new AboutPage().ContactByEmailAsync();

        Assert.Equal("jsoladelarosa@gmail.com", _h.Clipboard.Text);
        Assert.StartsWith("The email app could not be opened.", Assert.Single(_h.Alerts).Message);
    }

    [Fact]
    public async Task WithoutClipboard_SaysThereIsNoEmail()
    {
        _h.Email.Throws = new InvalidOperationException("sin correo");
        _h.Launcher.Throws = new FeatureNotSupportedException();
        _h.Clipboard.Throws = new InvalidOperationException("sin portapapeles");

        await new AboutPage().ContactByEmailAsync();

        Assert.Equal("Email client is not available on this device.", Assert.Single(_h.Alerts).Message);
    }

    [Fact]
    public async Task Back_PopsWhenStacked()
    {
        var nav = new NavigationPage(new ContentPage());
        var page = new AboutPage();
        await nav.PushAsync(page);

        await page.GoBackAsync();

        Assert.IsNotType<AboutPage>(nav.CurrentPage);
    }

    [Fact]
    public async Task Back_FromTheMenu_GoesHome()
    {
        var shell = AppShellTests.ShowShell(_h.App);
        shell.CurrentItem = Field<FlyoutItem>(shell, "AboutFlyoutItem");
        var page = new AboutPage();

        await page.GoBackAsync();
        Field<Button>(page, "BackButton").SendClicked();

        Assert.Same(Field<FlyoutItem>(shell, "HomeFlyoutItem"), shell.CurrentItem);
    }

    [Fact]
    public async Task Back_WithoutShell_DoesNothing()
    {
        var page = new AboutPage();

        await page.GoBackAsync();

        Assert.Null(Shell.Current);
    }
}

public sealed class AppShellTests : IDisposable
{
    /// <summary>La ventana de la app con su Shell (como al arrancar).</summary>
    internal static AppShell ShowShell(App app) => (AppShell)((Window)((IApplication)app).CreateWindow(null)).Page!;

    private readonly AppHarness _h = new();

    public void Dispose() => _h.Dispose();

    private static T Field<T>(object page, string name) =>
        (T)page.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(page)!;

    [Fact]
    public void Menu_IsLocalized_AndShowsTheVersion()
    {
        var shell = new AppShell();

        Assert.Equal("Home", Field<FlyoutItem>(shell, "HomeFlyoutItem").Title);
        Assert.Equal("About", Field<FlyoutItem>(shell, "AboutFlyoutItem").Title);
        Assert.Equal("v2026.10.01.0", Field<Label>(shell, "VersionLabel").Text);

        LocalizationService.Instance.SetLanguage("es");

        Assert.Equal("Inicio", Field<FlyoutItem>(shell, "HomeFlyoutItem").Title);
    }

    [Fact]
    public void Back_ClosesTheMenu_ThenGoesHome_ThenHidesTheApp()
    {
        var shell = AppShellTests.ShowShell(_h.App);

        shell.FlyoutIsPresented = true;
        Assert.True(shell.SendBackButtonPressed());
        Assert.False(shell.FlyoutIsPresented);

        shell.CurrentItem = Field<FlyoutItem>(shell, "AboutFlyoutItem");
        Assert.True(shell.SendBackButtonPressed());
        Assert.Same(Field<FlyoutItem>(shell, "HomeFlyoutItem"), shell.CurrentItem);
        Assert.Equal(0, _h.MovedToBack);

        Assert.True(shell.SendBackButtonPressed());
        Assert.Equal(1, _h.MovedToBack);
    }

    [Fact]
    public async Task Back_WithAPageOnTop_PopsIt()
    {
        var shell = AppShellTests.ShowShell(_h.App);
        await shell.Navigation.PushAsync(new ContentPage());

        shell.SendBackButtonPressed();

        Assert.Equal(0, _h.MovedToBack);
    }
}

public sealed class AppStartupTests : IDisposable
{
    private readonly AppHarness _h = new();

    public void Dispose() => _h.Dispose();

    [Fact]
    public void App_HasTheStyles_AndOpensTheShell()
    {
        Assert.True(_h.App.Resources.TryGetValue("Primary", out _));

        var window = ((IApplication)_h.App).CreateWindow(null);

        var shell = Assert.IsType<AppShell>(((Window)window).Page);
        // La pagina a la vista por defecto es la de contenido de la primera ventana.
        Assert.Same(AppPlatform.VisiblePage(shell), AppPlatformDefaults.CurrentPage());
    }

    [Fact]
    public void MauiProgram_RegistersTheServices()
    {
        var app = MauiProgram.CreateMauiApp();

        Assert.Same(LocalizationService.Instance, app.Services.GetService(typeof(LocalizationService)));
        Assert.NotNull(app.Services.GetService(typeof(RecentFilesService)));
        Assert.NotNull(app.Services.GetService(typeof(UpdateService)));
    }

    [Fact]
    public async Task Defaults_WaitForReal_AndHideNothing()
    {
        await AppPlatformDefaults.Delay(TimeSpan.Zero);
        AppPlatformDefaults.MoveTaskToBack();   // fuera de Android no hace nada

        Application.Current = null;
        Assert.Null(AppPlatformDefaults.CurrentPage());
        Assert.Throws<PlatformNotSupportedException>(() => AppPlatformDefaults.OpenContentUri("content://x"));
    }

    [Fact]
    public async Task VisiblePage_IsTheContentPageOnScreen()
    {
        // El dialogo moderno solo se pone sobre una ContentPage: con el Shell no se veia nada.
        var root = new ContentPage();
        var top = new ContentPage();
        var nav = new NavigationPage(root);
        await nav.PushAsync(top);
        var shell = AppShellTests.ShowShell(_h.App);
        var home = shell.CurrentPage;

        Assert.Same(top, AppPlatform.VisiblePage(nav));
        Assert.Same(root, AppPlatform.VisiblePage(root));
        Assert.Null(AppPlatform.VisiblePage(null));
        Assert.Same(home is null ? shell : home, AppPlatform.VisiblePage(shell));
        await shell.Navigation.PushAsync(new ContentPage { Title = "encima" });
        Assert.Equal("encima", AppPlatform.VisiblePage(shell)!.Title);
    }

    [Fact]
    public void DefaultAlert_IsTheModernDialog()
    {
        var host = new Grid();
        var page = new ContentPage { Content = host };

        // Sin dispositivo no hay animaciones: la entrada animada falla, pero el dialogo ya esta puesto.
        Assert.ThrowsAny<ArgumentException>(() => { _ = AppPlatformDefaults.Alert(page, "Titulo", "Mensaje", "OK", null); });

        Assert.Contains(host.Children, c => c is Grid g && g.StyleId == "__modernDialogOverlay");
    }
}
