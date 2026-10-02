using System.Runtime.CompilerServices;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.ApplicationModel.Communication;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Devices;
using TXTReader.Services;

namespace TXTReader.Tests;

// Dobles de lo que la app usa del dispositivo (Services/AppPlatform.cs). Registran lo que se les
// pide y responden lo que diga la prueba; ninguno abre nada de verdad.

public sealed class FakeFilePicker : IFilePicker
{
    public FileResult? Result { get; set; }
    public Exception? Throws { get; set; }
    public PickOptions? LastOptions { get; private set; }

    public Task<FileResult?> PickAsync(PickOptions? options = null)
    {
        LastOptions = options;
        if (Throws is not null) throw Throws;
        return Task.FromResult(Result);
    }

    public Task<IEnumerable<FileResult?>> PickMultipleAsync(PickOptions? options = null) =>
        throw new NotSupportedException();
}

public sealed class FakeEmail : IEmail
{
    public Exception? Throws { get; set; }
    public EmailMessage? Sent { get; private set; }
    public bool IsComposeSupported => true;

    public Task ComposeAsync(EmailMessage? message)
    {
        if (Throws is not null) throw Throws;
        Sent = message;
        return Task.CompletedTask;
    }
}

public sealed class FakeLauncher : ILauncher
{
    public Exception? Throws { get; set; }
    public List<Uri> Opened { get; } = new();

    public Task<bool> CanOpenAsync(Uri uri) => Task.FromResult(true);

    public Task<bool> OpenAsync(Uri uri)
    {
        if (Throws is not null) throw Throws;
        Opened.Add(uri);
        return Task.FromResult(true);
    }

    public Task<bool> OpenAsync(OpenFileRequest request) => throw new NotSupportedException();

    public Task<bool> TryOpenAsync(Uri uri) => OpenAsync(uri);
}

public sealed class FakeClipboard : IClipboard
{
    public Exception? Throws { get; set; }
    public string? Text { get; private set; }
    public bool HasText => Text is not null;

    public event EventHandler<EventArgs>? ClipboardContentChanged { add { } remove { } }

    public Task SetTextAsync(string? text)
    {
        if (Throws is not null) throw Throws;
        Text = text;
        return Task.CompletedTask;
    }

    public Task<string?> GetTextAsync() => Task.FromResult(Text);
}

public sealed class FakeBrowser : IBrowser
{
    public List<Uri> Opened { get; } = new();

    public Task<bool> OpenAsync(Uri uri, BrowserLaunchOptions options)
    {
        Opened.Add(uri);
        return Task.FromResult(true);
    }
}

public sealed class FakeAppInfo : IAppInfo
{
    public string PackageName => "com.socratic.txtreader";
    public string Name => "TXT Reader";
    public string VersionString { get; set; } = "2026.10.01.0";
    public Version Version => new(2026, 10, 1, 0);
    public string BuildString => "2026100100";
    public void ShowSettingsUI() { }
    public AppTheme RequestedTheme => AppTheme.Light;
    public AppPackagingModel PackagingModel => AppPackagingModel.Packaged;
    public LayoutDirection RequestedLayoutDirection => LayoutDirection.LeftToRight;
}

public sealed class FakeDeviceInfo : IDeviceInfo
{
    public string Model => "Prueba";
    public string Manufacturer => "sOCratic";
    public string Name => "Prueba";
    public string VersionString => "16";
    public Version Version => new(16, 0);
    public DevicePlatform Platform { get; set; } = DevicePlatform.Android;
    public DeviceIdiom Idiom => DeviceIdiom.Phone;
    public DeviceType DeviceType => DeviceType.Virtual;
}

/// <summary>Un aviso que la app quiso enseñar.</summary>
public sealed record Shown(Page Page, string Title, string Message, string Accept, string? Cancel);

/// <summary>
/// Estado comun de cada prueba que toca la app entera: dobles nuevos, preferencias vacias, idioma
/// ingles, App de verdad (con sus estilos) como Application.Current y registro en una carpeta
/// temporal. Las pruebas van en serie (EncodingDetectionTests.cs), asi que se puede compartir.
/// </summary>
public sealed class AppHarness : IDisposable
{
    public FakeFilePicker Picker { get; } = new();
    public FakeEmail Email { get; } = new();
    public FakeLauncher Launcher { get; } = new();
    public FakeClipboard Clipboard { get; } = new();
    public FakeBrowser Browser { get; } = new();
    public FakeAppInfo AppInfo { get; } = new();
    public FakeDeviceInfo DeviceInfo { get; } = new();
    public List<Shown> Alerts { get; } = new();
    public List<TimeSpan> Delays { get; } = new();
    public TempFolder Temp { get; } = new();

    /// <summary>Lo que responde el aviso (true = aceptar).</summary>
    public bool AlertAnswer { get; set; }

    public int MovedToBack { get; private set; }

    public Page? CurrentPage { get; set; }

    public App App { get; }

    public AppHarness(string language = "en")
    {
        Preferences.Clear();
        SecureStorage.Clear();
        TestLog.Reset();
        ResetLocalization();
        LocalizationService.Instance.SetLanguage(language);
        ClearFileIntentSubscribers();

        AppPlatform.FilePicker = Picker;
        AppPlatform.Email = Email;
        AppPlatform.Launcher = Launcher;
        AppPlatform.Clipboard = Clipboard;
        AppPlatform.Browser = Browser;
        AppPlatform.AppInfo = AppInfo;
        AppPlatform.DeviceInfo = DeviceInfo;
        AppPlatform.CacheDirectory = () => Temp.Combine("cache");
        AppPlatform.Alert = (page, title, message, accept, cancel) =>
        {
            Alerts.Add(new Shown(page, title, message, accept, cancel));
            return Task.FromResult(AlertAnswer);
        };
        AppPlatform.RunOnMainThreadAsync = action => action();
        AppPlatform.Delay = time => { Delays.Add(time); return Task.CompletedTask; };
        AppPlatform.CurrentPage = () => CurrentPage;
        AppPlatform.MoveTaskToBack = () => MovedToBack++;
        AppPlatform.OpenContentUri = _ => throw new PlatformNotSupportedException("Content URIs are only supported on Android");

        App = new App();
        Application.Current = App;
    }

    /// <summary>Una Application con los recursos de la app, salvo los indicados, que no son lo que se espera.</summary>
    public static Application AppWithout(params string[] keys)
    {
        var bare = new Application();
        foreach (var dictionary in new App().Resources.MergedDictionaries)
            bare.Resources.MergedDictionaries.Add(dictionary);
        foreach (var key in keys)
            bare.Resources[key] = "ausente";
        return bare;
    }

    /// <summary>Otra instancia del servicio de idiomas (es un singleton con estado).</summary>
    public static void ResetLocalization() =>
        typeof(LocalizationService).GetField("_instance", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .SetValue(null, null);

    /// <summary>Quita las paginas que se suscribieron en otras pruebas.</summary>
    public static void ClearFileIntentSubscribers() =>
        typeof(FileIntentService).GetField("FileOpened", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .SetValue(null, null);

    public void Dispose()
    {
        ClearFileIntentSubscribers();
        AppPlatformDefaults.Restore();
        Application.Current = null;
        Temp.Dispose();
    }
}

/// <summary>El registro de depuracion de la app va a un fichero temporal propio de las pruebas.</summary>
public static class TestLog
{
    public static readonly string Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"txt-tests-log-{Environment.ProcessId}.log");

    [ModuleInitializer]
    internal static void Init() => MobileLogService.LogFilePath = Path;

    public static void Reset()
    {
        MobileLogService.LogFilePath = Path;
        try { File.Delete(Path); } catch (IOException) { }
    }

    /// <summary>Lo escrito; los registros van sin esperar (fire and forget), asi que se espera un poco.</summary>
    public static async Task<string> WaitForAsync(string fragment)
    {
        for (var i = 0; i < 100; i++)
        {
            try
            {
                if (File.Exists(Path))
                {
                    var text = await File.ReadAllTextAsync(Path);
                    if (text.Contains(fragment)) return text;
                }
            }
            catch (IOException) { }
            await Task.Delay(20);
        }
        return File.Exists(Path) ? await File.ReadAllTextAsync(Path) : string.Empty;
    }
}

/// <summary>Las puertas de AppPlatform tal como las deja la app, para volver a ellas tras cada prueba.</summary>
public static class AppPlatformDefaults
{
    public static Func<string, Stream?> OpenContentUri { get; private set; } = null!;
    public static Func<Page, string, string, string, string?, Task<bool>> Alert { get; private set; } = null!;
    public static Func<TimeSpan, Task> Delay { get; private set; } = null!;
    public static Func<Page?> CurrentPage { get; private set; } = null!;
    public static Action MoveTaskToBack { get; private set; } = null!;
    public static Func<string> CacheDirectory { get; private set; } = null!;
    public static Func<Func<Task>, Task> RunOnMainThreadAsync { get; private set; } = null!;

    [ModuleInitializer]
    internal static void Capture()
    {
        OpenContentUri = AppPlatform.OpenContentUri;
        Alert = AppPlatform.Alert;
        Delay = AppPlatform.Delay;
        CurrentPage = AppPlatform.CurrentPage;
        MoveTaskToBack = AppPlatform.MoveTaskToBack;
        CacheDirectory = AppPlatform.CacheDirectory;
        RunOnMainThreadAsync = AppPlatform.RunOnMainThreadAsync;
    }

    public static void Restore()
    {
        AppPlatform.OpenContentUri = OpenContentUri;
        AppPlatform.Alert = Alert;
        AppPlatform.Delay = Delay;
        AppPlatform.CurrentPage = CurrentPage;
        AppPlatform.MoveTaskToBack = MoveTaskToBack;
        AppPlatform.CacheDirectory = CacheDirectory;
        AppPlatform.RunOnMainThreadAsync = RunOnMainThreadAsync;
    }
}

/// <summary>
/// Hilo principal de mentira: lo que se le manda se ejecuta en el acto. MAUI lo necesita para los
/// enlaces entre controles (x:Reference) cuando no hay dispositivo.
/// </summary>
public sealed class ImmediateDispatcher : IDispatcher, IDispatcherProvider
{
    [ModuleInitializer]
    internal static void Install() => DispatcherProvider.SetCurrent(new ImmediateDispatcher());

    public IDispatcher? GetForCurrentThread() => this;

    public bool IsDispatchRequired => false;

    public bool Dispatch(Action action)
    {
        action();
        return true;
    }

    public bool DispatchDelayed(TimeSpan delay, Action action) => Dispatch(action);

    public IDispatcherTimer CreateTimer() => throw new NotSupportedException();
}
