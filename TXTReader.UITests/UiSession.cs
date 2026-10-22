using System.Diagnostics;
using System.Net.Http;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Android;
using OpenQA.Selenium.Appium.Enums;

namespace TXTReader.UITests;

/// <summary>
/// Una sesion de Appium para toda la tanda: arranca el servidor si no esta ya escuchando, se
/// conecta al dispositivo y deja la app recien borrada (sin recientes ni preferencias). Al acabar
/// devuelve el tema del sistema al claro, borra los ficheros de prueba y cierra lo que abrio.
/// </summary>
/// <remarks>
/// Variables (todas opcionales): <c>TXT_UDID</c> (por defecto el MuMu, <c>127.0.0.1:16416</c>),
/// <c>TXT_APPIUM_URL</c> (<c>http://127.0.0.1:4723/</c>), <c>TXT_APPIUM</c>
/// (<c>D:\dev\appium\node_modules\.bin\appium.cmd</c>), <c>APPIUM_HOME</c>
/// (<c>D:\dev\appium-home</c>), <c>ANDROID_HOME</c> (<c>D:\dev\android-sdk</c>) y <c>TXT_APK</c>
/// (APK a instalar antes de empezar).
/// </remarks>
public sealed class UiSession : IDisposable
{
    public const string Package = "com.socratic.txtreader";

    /// <summary>Carpeta de la app en el almacenamiento compartido: la lee sin pedir permisos.</summary>
    public const string DeviceFolder = "/sdcard/Android/data/" + Package + "/files";

    public AndroidDriver Driver { get; }

    public string Udid { get; } = Env("TXT_UDID", "127.0.0.1:16416");

    public string ArtifactsDir { get; }

    private readonly Process? _server;
    private readonly string _adb;

    public UiSession()
    {
        var androidHome = Env("ANDROID_HOME", @"D:\dev\android-sdk");
        _adb = Path.Combine(androidHome, "platform-tools", "adb.exe");

        ArtifactsDir = Path.Combine(ProjectDir(), "artifacts", DateTime.Now.ToString("yyyyMMdd-HHmmss"));
        Directory.CreateDirectory(ArtifactsDir);

        var url = new Uri(Env("TXT_APPIUM_URL", "http://127.0.0.1:4723/"));
        if (!ServerAlive(url))
        {
            _server = StartServer(url, androidHome);
        }

        SetNightMode(false);

        var options = new AppiumOptions
        {
            PlatformName = "Android",
            AutomationName = AutomationName.AndroidUIAutomator2,
        };
        options.AddAdditionalAppiumOption("udid", Udid);
        options.AddAdditionalAppiumOption("appPackage", Package);
        options.AddAdditionalAppiumOption("appWaitActivity", "*");
        // noReset=false: Appium hace «pm clear» antes de abrir (sin recientes ni preferencias).
        options.AddAdditionalAppiumOption("noReset", false);
        options.AddAdditionalAppiumOption("fullReset", false);
        options.AddAdditionalAppiumOption("newCommandTimeout", 300);
        options.AddAdditionalAppiumOption("autoGrantPermissions", true);
        options.AddAdditionalAppiumOption("disableWindowAnimation", true);
        if (Environment.GetEnvironmentVariable("TXT_APK") is { Length: > 0 } apk)
        {
            options.App = apk;
        }

        Driver = new AndroidDriver(url, options, TimeSpan.FromMinutes(3));
        Driver.Manage().Timeouts().ImplicitWait = TimeSpan.Zero;
    }

    // ---------------------------------------------------------------- utilidades

    /// <summary>Espera a que aparezca un elemento; null si no llega a tiempo.</summary>
    public AppiumElement? WaitFor(By by, double seconds = 10)
    {
        var until = DateTime.UtcNow.AddSeconds(seconds);
        do
        {
            var found = Driver.FindElements(by);
            if (found.Count > 0)
            {
                return found[0];
            }

            Thread.Sleep(250);
        }
        while (DateTime.UtcNow < until);

        return null;
    }

    public AppiumElement Require(By by, string what, double seconds = 10) =>
        WaitFor(by, seconds) ?? throw new Xunit.Sdk.XunitException($"No aparece {what}.");

    /// <summary>
    /// El AutomationId de MAUI llega a Android como resource-id en los controles y como
    /// content-desc en los botones de la cabecera (ToolbarItem): se busca por los dos.
    /// </summary>
    public static By Id(string automationId) =>
        By.XPath($"//*[@resource-id='{Package}:id/{automationId}' or @content-desc='{automationId}']");

    /// <summary>Un texto que se ve (tambien los del documento dentro del WebView).</summary>
    public static By Text(string text) =>
        By.XPath($"//*[@text='{text}' or @content-desc='{text}']");

    public static By TextStartsWith(string text) =>
        By.XPath($"//*[starts-with(@text,'{text}') or starts-with(@content-desc,'{text}')]");

    /// <summary>Guarda la pantalla en la carpeta de artefactos de la tanda.</summary>
    public void Shot(string name)
    {
        var file = Path.Combine(ArtifactsDir, $"{DateTime.Now:HHmmss}-{name}.png");
        Driver.GetScreenshot().SaveAsFile(file);
    }

    /// <summary>Paquete de la app que esta en primer plano.</summary>
    public string? ForegroundPackage() => Driver.CurrentPackage;

    /// <summary>Copia un fichero de Muestras/ a la carpeta de la app en el dispositivo.</summary>
    public string Push(string sample)
    {
        var local = Path.Combine(AppContext.BaseDirectory, "Muestras", sample);
        Adb($"shell mkdir -p {DeviceFolder}");
        Adb($"push \"{local}\" {DeviceFolder}/{sample}");
        return $"{DeviceFolder}/{sample}";
    }

    /// <summary>Abre un fichero del dispositivo en la app como lo haria otra app («Abrir con»).</summary>
    public void OpenWithApp(string devicePath, string mime) =>
        Adb($"shell am start -a android.intent.action.VIEW -d file://{devicePath} -t {mime} -p {Package}");

    public void SetNightMode(bool dark) => Adb($"shell cmd uimode night {(dark ? "yes" : "no")}");

    /// <summary>
    /// Luminancia (0-255) de un pixel de la pantalla, con la captura en crudo de adb (RGBA tras una
    /// cabecera cuyo tamaño cambia con la version de Android: se deduce del total).
    /// </summary>
    public int Luma(int x, int y)
    {
        var raw = AdbBytes("exec-out screencap");
        var width = BitConverter.ToInt32(raw, 0);
        var height = BitConverter.ToInt32(raw, 4);
        var header = raw.Length - width * height * 4;
        var i = header + (y * width + x) * 4;
        return (raw[i] * 299 + raw[i + 1] * 587 + raw[i + 2] * 114) / 1000;
    }

    public string Adb(string args)
    {
        using var p = Process.Start(AdbStart(args))!;
        var output = p.StandardOutput.ReadToEnd();
        p.WaitForExit(30_000);
        return output;
    }

    private byte[] AdbBytes(string args)
    {
        using var p = Process.Start(AdbStart(args))!;
        using var buffer = new MemoryStream();
        p.StandardOutput.BaseStream.CopyTo(buffer);
        p.WaitForExit(30_000);
        return buffer.ToArray();
    }

    private ProcessStartInfo AdbStart(string args) => new(_adb, $"-s {Udid} {args}")
    {
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true,
    };

    public void Dispose()
    {
        try
        {
            SetNightMode(false);
            Adb($"shell rm -rf {DeviceFolder}");
        }
        catch
        {
            // Si adb no responde no hay mas que hacer; el README avisa de comprobarlo.
        }

        try
        {
            Driver.Quit();
        }
        catch
        {
        }

        if (_server is { HasExited: false })
        {
            _server.Kill(entireProcessTree: true);
        }
    }

    // ---------------------------------------------------------------- servidor

    private static bool ServerAlive(Uri url)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
            return http.GetAsync(new Uri(url, "status")).Result.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private static Process StartServer(Uri url, string androidHome)
    {
        var exe = Env("TXT_APPIUM", @"D:\dev\appium\node_modules\.bin\appium.cmd");
        var psi = new ProcessStartInfo(exe, $"--address {url.Host} --port {url.Port} --log-level warn")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        psi.Environment["APPIUM_HOME"] = Env("APPIUM_HOME", @"D:\dev\appium-home");
        psi.Environment["ANDROID_HOME"] = androidHome;
        psi.Environment["ANDROID_SDK_ROOT"] = androidHome;

        var server = Process.Start(psi)!;
        server.OutputDataReceived += (_, _) => { };
        server.ErrorDataReceived += (_, _) => { };
        server.BeginOutputReadLine();
        server.BeginErrorReadLine();

        var until = DateTime.UtcNow.AddSeconds(60);
        while (DateTime.UtcNow < until)
        {
            if (ServerAlive(url))
            {
                return server;
            }

            Thread.Sleep(500);
        }

        server.Kill(entireProcessTree: true);
        throw new InvalidOperationException($"Appium no arranca en {url} con {exe}.");
    }

    private static string Env(string name, string fallback) =>
        Environment.GetEnvironmentVariable(name) is { Length: > 0 } value ? value : fallback;

    private static string ProjectDir()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "TXTReader.UITests.csproj")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? AppContext.BaseDirectory;
    }
}

[CollectionDefinition(Name)]
public sealed class UiCollection : ICollectionFixture<UiSession>
{
    public const string Name = "Appium";
}

/// <summary>Las pruebas de una clase se ejecutan por orden de nombre (T01, T02...).</summary>
public sealed class ByNameOrderer : Xunit.Sdk.ITestCaseOrderer
{
    public IEnumerable<TTestCase> OrderTestCases<TTestCase>(IEnumerable<TTestCase> testCases)
        where TTestCase : Xunit.Abstractions.ITestCase =>
        testCases.OrderBy(t => t.TestMethod.Method.Name, StringComparer.Ordinal);
}
