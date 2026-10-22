namespace TXTReader.Services;

/// <summary>
/// Que hacer cuando se toca un enlace del visor: la pagina nunca navega fuera de si misma; las
/// direcciones web, de correo y de telefono se abren con la app que toque del dispositivo, y el
/// resto (rutas relativas a otros ficheros, esquemas raros) se ignora.
/// </summary>
public static class ReaderLinks
{
    private static readonly HashSet<string> ExternalSchemes = new(StringComparer.OrdinalIgnoreCase)
    {
        "http", "https", "mailto", "tel", "geo"
    };

    /// <summary>
    /// ¿Es la propia pagina del visor (la carga inicial)? Esa navegacion se deja pasar; cualquier
    /// otra se cancela.
    /// </summary>
    public static bool IsViewerPage(string? url) =>
        string.IsNullOrEmpty(url)
        || url.StartsWith("about:", StringComparison.OrdinalIgnoreCase)
        || url.StartsWith("data:", StringComparison.OrdinalIgnoreCase)
        || url.TrimEnd('/').Equals("file:///android_asset", StringComparison.OrdinalIgnoreCase);

    /// <summary>La direccion a abrir fuera de la app, o null si no es un enlace que se abra fuera.</summary>
    public static Uri? ExternalUri(string? url) =>
        !string.IsNullOrWhiteSpace(url)
        && Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri)
        && ExternalSchemes.Contains(uri.Scheme)
            ? uri
            : null;
}
