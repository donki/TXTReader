namespace TXTReader.Services;

/// <summary>Lo que se pide al selector de ficheros y como se lee lo que devuelve.</summary>
public static class PickedFiles
{
    /// <summary>
    /// Solo ficheros de texto: sin el comodin "*/*", que hacia que el selector mostrara todo (PDF,
    /// imagenes, video...). "text/*" cubre txt, csv, html, markdown, xml y demas subtipos de texto;
    /// se anaden json y xml, que Android reporta con su propio MIME.
    /// "application/octet-stream" se incluye por los .gpx: Android no conoce esa extension (no esta
    /// en su tabla de MIME) y los expone como octet-stream, asi que sin este tipo el selector los
    /// muestra en gris. Tambien recupera los .log, .ini y .cfg que el sistema no sabe clasificar.
    /// Sigue sin colar PDF, imagenes ni video, que si tienen MIME propio.
    /// </summary>
    public static readonly IReadOnlyList<string> AndroidMimeTypes =
        ["text/*", "application/json", "application/xml", "application/gpx+xml", "application/octet-stream"];

    public static FilePickerFileType FileTypes() => new(new Dictionary<DevicePlatform, IEnumerable<string>>
    {
        { DevicePlatform.Android, AndroidMimeTypes },
        { DevicePlatform.WinUI, IntentFileHandler.SupportedExtensions.ToArray() }
    });

    /// <summary>
    /// Ruta que se puede abrir de lo elegido: la propia si el selector la da; si no (proveedores
    /// sin ruta), una copia en la cache con un nombre seguro y unico.
    /// </summary>
    public static async Task<string> GetReadablePathAsync(string? fullPath, string fileName, Func<Task<Stream>> openRead, string cacheRoot)
    {
        if (!string.IsNullOrWhiteSpace(fullPath))
            return fullPath;

        var cacheDirectory = Path.Combine(cacheRoot, "picked_files");
        Directory.CreateDirectory(cacheDirectory);

        var stamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var safeFileName = SafeFileName(fileName);
        if (string.IsNullOrWhiteSpace(safeFileName))
            safeFileName = $"selected_file_{stamp}.txt";

        var cachedPath = Path.Combine(cacheDirectory, $"{stamp}_{safeFileName}");
        await using var inputStream = await openRead();
        await using var outputStream = File.Create(cachedPath);
        await inputStream.CopyToAsync(outputStream);
        return cachedPath;
    }

    /// <summary>
    /// Nombre sin caracteres prohibidos en ningun sistema (los trozos se unen con «_»). Se usa la
    /// lista de Windows, que incluye la de Android/Linux, para que el resultado no dependa de donde
    /// se ejecute.
    /// </summary>
    public static string SafeFileName(string? fileName) =>
        string.Join("_", (fileName ?? string.Empty).Split(InvalidChars, StringSplitOptions.RemoveEmptyEntries));

    private static readonly char[] InvalidChars =
        [.. Path.GetInvalidFileNameChars(), '<', '>', ':', '"', '/', '\\', '|', '?', '*'];
}
