namespace TXTReader.Services;

/// <summary>
/// Apertura de un fichero que llega de otra app (intent ACTION_VIEW). MainActivity solo saca de la
/// URI de Android sus partes (texto, esquema, ruta y nombre visible) y llama aqui: que referencia
/// se puede leer, si la extension es de texto, el aviso de la nube cuando no hay nada que leer y,
/// al volver la actividad a primer plano, el aviso a MainPage (FileIntentService) para abrirlo.
/// </summary>
public class IntentFileHandler
{
    /// <summary>Extensiones que se abren; sin extension tambien (muchas URIs content:// no la llevan).</summary>
    public static readonly IReadOnlySet<string> SupportedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".txt", ".log", ".json", ".xml", ".gpx", ".csv", ".md", ".ini", ".cfg", ".conf"
    };

    /// <summary>Espera antes de abrir lo pendiente, para que la app este cargada del todo.</summary>
    public static readonly TimeSpan PendingDelay = TimeSpan.FromMilliseconds(1500);

    /// <summary>Espera antes del aviso de la nube, para que haya pagina sobre la que enseñarlo.</summary>
    public static readonly TimeSpan CloudHintDelay = TimeSpan.FromMilliseconds(2000);

    /// <summary>Espera antes del aviso de fichero que ya no existe.</summary>
    public static readonly TimeSpan MissingFileDelay = TimeSpan.FromMilliseconds(500);

    private readonly LocalizationService _loc = LocalizationService.Instance;

    /// <summary>Fichero recibido que se abrira cuando la actividad vuelva a primer plano.</summary>
    public string? PendingFilePath { get; private set; }

    public static bool IsContentUri(string value) =>
        value.StartsWith("content://", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Lo que se puede leer de la URI: la ruta si es file://, la propia URI si es content:// (se lee
    /// con el ContentResolver) y, en otro caso, la ruta tal cual.
    /// </summary>
    public static string? GetReadableReference(string uri, string? scheme, string? path)
    {
        if (string.Equals(scheme, "file", StringComparison.OrdinalIgnoreCase))
            return path;
        if (string.Equals(scheme, "content", StringComparison.OrdinalIgnoreCase))
            return uri;
        return path;
    }

    /// <summary>Extension en minusculas del nombre visible o, si no lo hay, de la ruta.</summary>
    public static string GetExtension(string? fileName, string filePath)
    {
        var candidate = !string.IsNullOrWhiteSpace(fileName) ? fileName : filePath;
        return Path.GetExtension(candidate).ToLowerInvariant();
    }

    public static bool IsSupported(string extension) =>
        string.IsNullOrEmpty(extension) || SupportedExtensions.Contains(extension);

    /// <summary>
    /// Clave del consejo cuando la nube no da el fichero. Se mira la URI para elegirlo; el texto que
    /// ve el usuario sale de los recursos (constitucion Web 4).
    /// </summary>
    public static string CloudHintKey(string uri)
    {
        var lower = uri.ToLowerInvariant();
        if (lower.Contains("onedrive")) return "CloudOneDrive";
        if (lower.Contains("drive.google")) return "CloudGoogleDrive";
        if (lower.Contains("dropbox")) return "CloudStorage";
        return "CloudGeneric";
    }

    /// <summary>
    /// Intent de ver un fichero: si se puede leer y es de texto, queda pendiente; si no hay nada
    /// que leer, se enseña el consejo de la nube.
    /// </summary>
    public async Task HandleViewIntentAsync(string uri, string? scheme, string? path, string? displayName)
    {
        try
        {
            _ = MobileLogService.LogAsync($"HandleIntent: Received URI: {uri}");
            var filePath = GetReadableReference(uri, scheme, path);
            _ = MobileLogService.LogAsync($"HandleIntent: GetReadableFileReference returned: '{filePath}'");

            if (string.IsNullOrEmpty(filePath))
            {
                await AppPlatform.Delay(CloudHintDelay);
                var key = CloudHintKey(uri);
                await ShowAlertAsync(_loc.GetString(key + "Title"), _loc.GetString(key + "Message"));
                return;
            }

            var extension = GetExtension(displayName, filePath);
            if (IsSupported(extension))
            {
                _ = MobileLogService.LogAsync($"HandleIntent: Setting _pendingFilePath to: '{filePath}' (extension: {extension})");
                PendingFilePath = filePath;
            }
            else
            {
                _ = MobileLogService.LogAsync($"HandleIntent: Unsupported file type: {extension}");
            }
        }
        catch (Exception ex)
        {
            _ = MobileLogService.LogAsync($"HandleIntent: ERROR - {ex.Message}");
        }
    }

    /// <summary>
    /// Al volver a primer plano: abre lo pendiente (aviso a MainPage) o, si el fichero local ya no
    /// existe, lo dice.
    /// </summary>
    public async Task ProcessPendingAsync()
    {
        var filePath = PendingFilePath;
        if (string.IsNullOrEmpty(filePath))
            return;
        PendingFilePath = null;

        try
        {
            await AppPlatform.Delay(PendingDelay);
            _ = MobileLogService.LogAsync($"OnResume: Processing pending file: {filePath}");

            if (IsContentUri(filePath) || File.Exists(filePath))
            {
                await AppPlatform.RunOnMainThreadAsync(() =>
                {
                    _ = MobileLogService.LogAsync($"OnResume: Notifying FileIntentService with: {filePath}");
                    FileIntentService.NotifyFileOpened(filePath);
                    return Task.CompletedTask;
                });
            }
            else
            {
                _ = MobileLogService.LogAsync($"OnResume: File does not exist: {filePath}");
                await AppPlatform.Delay(MissingFileDelay);
                // Antes este aviso salia siempre en castellano, aunque la app estuviera en ingles.
                await ShowAlertAsync(_loc.GetString("CloudGenericTitle"), _loc.GetString("CloudGenericMessage"));
            }
        }
        catch (Exception ex)
        {
            _ = MobileLogService.LogAsync($"OnResume: ERROR - {ex.Message}");
        }
    }

    private Task ShowAlertAsync(string title, string message) =>
        AppPlatform.RunOnMainThreadAsync(async () =>
        {
            var page = AppPlatform.CurrentPage();
            if (page != null)
                await AppPlatform.Alert(page, title, message, _loc.GetString("OK"), null);
        });
}
