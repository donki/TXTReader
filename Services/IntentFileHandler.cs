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
        ".txt", ".log", ".json", ".xml", ".gpx", ".csv", ".md", ".markdown", ".ini", ".cfg", ".conf"
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

    /// <summary>Extension que corresponde a un tipo MIME de texto con formato (no la de texto plano).</summary>
    public static string? ExtensionForMime(string? mimeType) => mimeType?.Split(';')[0].Trim().ToLowerInvariant() switch
    {
        "text/markdown" or "text/x-markdown" => ".md",
        "application/json" or "text/json" => ".json",
        "text/csv" or "text/comma-separated-values" => ".csv",
        "application/xml" or "text/xml" => ".xml",
        "application/gpx+xml" => ".gpx",
        _ => null,
    };

    /// <summary>
    /// El nombre que se enseña y del que sale el formato: el nombre visible que da la otra app o, si
    /// no lo da, el final de la ruta; sin extension, la de su tipo MIME (un .md compartido como
    /// content://media/.../1000070244 se veia con el numero de titulo y sin la vista de Markdown).
    /// </summary>
    public static string FileNameFor(string? displayName, string filePath, string? mimeType)
    {
        var name = !string.IsNullOrWhiteSpace(displayName) ? displayName.Trim() : Path.GetFileName(filePath.TrimEnd('/'));
        if (string.IsNullOrEmpty(Path.GetExtension(name)) && ExtensionForMime(mimeType) is { } extension)
            name += extension;
        return name;
    }

    /// <summary>Nombre que se enseña del fichero pendiente.</summary>
    public string? PendingFileName { get; private set; }

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
    public async Task HandleViewIntentAsync(string uri, string? scheme, string? path, string? displayName, string? mimeType = null)
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

            var fileName = FileNameFor(displayName, filePath, mimeType);
            var extension = GetExtension(fileName, filePath);
            if (IsSupported(extension))
            {
                _ = MobileLogService.LogAsync($"HandleIntent: Setting _pendingFilePath to: '{filePath}' (extension: {extension})");
                PendingFilePath = filePath;
                PendingFileName = fileName;
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
        var fileName = PendingFileName ?? Path.GetFileName(filePath);
        PendingFilePath = null;
        PendingFileName = null;

        try
        {
            await AppPlatform.Delay(PendingDelay);
            _ = MobileLogService.LogAsync($"OnResume: Processing pending file: {filePath}");

            if (IsContentUri(filePath) || File.Exists(filePath))
            {
                await AppPlatform.RunOnMainThreadAsync(() =>
                {
                    _ = MobileLogService.LogAsync($"OnResume: Notifying FileIntentService with: {filePath}");
                    FileIntentService.NotifyFileOpened(filePath, fileName);
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
