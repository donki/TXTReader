namespace TXTReader.Services;

/// <summary>
/// Lo que la app usa del dispositivo (selector de ficheros, correo, navegador, portapapeles,
/// version, dialogos, hilo principal...) pasa por aqui. En la app cada puerta es la de MAUI; las
/// pruebas (constitucion General 8.6) ponen dobles para recorrer las paginas sin dispositivo.
/// </summary>
public static class AppPlatform
{
    private static IFilePicker? _filePicker;
    private static IEmail? _email;
    private static ILauncher? _launcher;
    private static IClipboard? _clipboard;
    private static IBrowser? _browser;
    private static IAppInfo? _appInfo;
    private static IDeviceInfo? _deviceInfo;

    public static IFilePicker FilePicker { get => _filePicker ??= Microsoft.Maui.Storage.FilePicker.Default; set => _filePicker = value; }

    public static IEmail Email { get => _email ??= Microsoft.Maui.ApplicationModel.Communication.Email.Default; set => _email = value; }

    public static ILauncher Launcher { get => _launcher ??= Microsoft.Maui.ApplicationModel.Launcher.Default; set => _launcher = value; }

    public static IClipboard Clipboard { get => _clipboard ??= Microsoft.Maui.ApplicationModel.DataTransfer.Clipboard.Default; set => _clipboard = value; }

    public static IBrowser Browser { get => _browser ??= Microsoft.Maui.ApplicationModel.Browser.Default; set => _browser = value; }

    public static IAppInfo AppInfo { get => _appInfo ??= Microsoft.Maui.ApplicationModel.AppInfo.Current; set => _appInfo = value; }

    public static IDeviceInfo DeviceInfo { get => _deviceInfo ??= Microsoft.Maui.Devices.DeviceInfo.Current; set => _deviceInfo = value; }

    /// <summary>Carpeta de cache de la app (copias de los ficheros elegidos sin ruta propia).</summary>
    public static Func<string> CacheDirectory { get; set; } = () => FileSystem.CacheDirectory;

    /// <summary>
    /// Abre una URI content:// (ficheros que comparten otras apps o la nube). La pone MainActivity
    /// con el ContentResolver de Android; fuera de Android no hay.
    /// </summary>
    public static Func<string, Stream?> OpenContentUri { get; set; } =
        _ => throw new PlatformNotSupportedException("Content URIs are only supported on Android");

    /// <summary>Aviso o confirmacion con el dialogo moderno comun (pagina, titulo, mensaje, aceptar, cancelar).</summary>
    public static Func<Page, string, string, string, string?, Task<bool>> Alert { get; set; } =
        (page, title, message, accept, cancel) => SocShared.ModernDialog.AlertAsync(page, title, message, accept, cancel);

    /// <summary>Ejecuta en el hilo principal.</summary>
    public static Func<Func<Task>, Task> RunOnMainThreadAsync { get; set; } = MainThread.InvokeOnMainThreadAsync;

    /// <summary>Espera (los retrasos de la apertura desde otra app).</summary>
    public static Func<TimeSpan, Task> Delay { get; set; } = time => Task.Delay(time);

    /// <summary>Pagina que esta a la vista (para los avisos que no nacen de una pagina).</summary>
    public static Func<Page?> CurrentPage { get; set; } = () => VisiblePage(Application.Current?.Windows.FirstOrDefault()?.Page);

    /// <summary>
    /// La pagina de contenido que se ve dentro de un Shell o una NavigationPage. El dialogo moderno
    /// solo se puede poner sobre una ContentPage: con el Shell de la ventana no salia nada, y los
    /// avisos de fichero no disponible o de la nube no se veian nunca.
    /// </summary>
    public static Page? VisiblePage(Page? page) => page switch
    {
        Shell shell when shell.CurrentPage is { } current => VisiblePage(current),
        NavigationPage navigation when navigation.CurrentPage is { } current => VisiblePage(current),
        _ => page
    };

    /// <summary>
    /// Oculta la app sin cerrarla (atras en la pantalla principal, Mobile 7). La pone MainActivity
    /// en Android; fuera de Android no hace nada.
    /// </summary>
    public static Action MoveTaskToBack { get; set; } = () => { };
}
