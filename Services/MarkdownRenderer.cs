using Markdig;
using Markdig.Extensions.AutoIdentifiers;
using Markdig.Renderers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace TXTReader.Services;

/// <summary>
/// Markdown a HTML con Markdig (BSD-2-Clause), todo en el dispositivo: las imagenes locales del
/// documento se incrustan como data: y las de internet se convierten en un enlace con su texto
/// alternativo, para no pedir nada a la red al pintar. Fuera de la pagina para poder probarlo sin
/// interfaz (General 8.6).
/// </summary>
public static class MarkdownRenderer
{
    /// <summary>Extensiones que se abren en la vista renderizada por defecto.</summary>
    public static readonly IReadOnlySet<string> Extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".md", ".markdown", ".mdown", ".mkd", ".mkdn", ".mdwn", ".mdtxt", ".mdtext"
    };

    /// <summary>Imagen local mas grande que se incrusta (las mayores se quedan en su texto alternativo).</summary>
    public const long MaxInlineImageBytes = 5 * 1024 * 1024;

    private static readonly Dictionary<string, string> ImageTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".png"] = "image/png",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".gif"] = "image/gif",
        [".webp"] = "image/webp",
        [".bmp"] = "image/bmp",
        [".svg"] = "image/svg+xml",
        [".ico"] = "image/x-icon",
    };

    // Lo que se suele ver en un README: tablas (de barras y de rejilla), tachado y demas enfasis,
    // listas de tareas y con letras, enlaces sueltos, anclas en los titulos como GitHub, notas al pie
    // y listas de definiciones. Sin MediaLinks (incrusta videos de internet) ni atributos genericos.
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UsePipeTables()
        .UseGridTables()
        .UseEmphasisExtras()
        .UseTaskLists()
        .UseAutoLinks()
        .UseAutoIdentifiers(AutoIdentifierOptions.GitHub)
        .UseListExtras()
        .UseFootnotes()
        .UseDefinitionLists()
        .Build();

    /// <summary>¿Es un fichero Markdown por su extension (del nombre visible o de la ruta)?</summary>
    public static bool IsMarkdownFile(string? fileName) =>
        !string.IsNullOrWhiteSpace(fileName) && Extensions.Contains(Path.GetExtension(fileName.Trim()));

    /// <summary>
    /// HTML del cuerpo del documento. <paramref name="baseDirectory"/> es la carpeta del fichero,
    /// para encontrar las imagenes con ruta relativa; sin ella (URIs content://) no se incrusta nada.
    /// </summary>
    public static string ToHtml(string? markdown, string? baseDirectory)
    {
        var document = Markdown.Parse(markdown ?? string.Empty, Pipeline);

        foreach (var link in document.Descendants<LinkInline>().Where(l => l.IsImage).ToList())
            ResolveImage(link, baseDirectory);

        using var writer = new StringWriter();
        var renderer = new HtmlRenderer(writer);
        Pipeline.Setup(renderer);
        renderer.Render(document);
        writer.Flush();
        return writer.ToString();
    }

    /// <summary>
    /// Deja la imagen lista para pintarse sin red: data: se queda; un fichero local que exista se
    /// incrusta; una direccion de internet pasa a ser un enlace con el texto alternativo (se abre al
    /// tocarlo, en el navegador). Lo demas se queda como esta y el visor enseña el texto alternativo.
    /// </summary>
    internal static void ResolveImage(LinkInline link, string? baseDirectory)
    {
        var url = link.Url?.Trim();
        if (string.IsNullOrEmpty(url) || url.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
            return;

        if (Uri.TryCreate(url, UriKind.Absolute, out var absolute) && absolute.Scheme is "http" or "https")
        {
            link.IsImage = false;
            return;
        }

        if (LocalImagePath(url, baseDirectory) is { } path && ToDataUri(path) is { } dataUri)
            link.Url = dataUri;
    }

    /// <summary>Ruta en disco de una imagen local (relativa a la carpeta del documento, absoluta o file://).</summary>
    internal static string? LocalImagePath(string url, string? baseDirectory)
    {
        try
        {
            if (url.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
                return new Uri(url).LocalPath;

            if (Uri.TryCreate(url, UriKind.Absolute, out var other) && other.Scheme.Length > 1 && !Path.IsPathRooted(url))
                return null; // otro esquema (ftp:, content:...): no es un fichero

            // Sin la parte de consulta ni el ancla, y con los %20 y demas ya decodificados.
            var clean = url.Split('?', '#')[0];
            clean = Uri.UnescapeDataString(clean);
            if (Path.IsPathRooted(clean))
                return clean;
            if (string.IsNullOrWhiteSpace(baseDirectory))
                return null;
            return Path.GetFullPath(Path.Combine(baseDirectory, clean));
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>La imagen como data: si existe, es de un tipo conocido y no pasa del limite.</summary>
    internal static string? ToDataUri(string path)
    {
        try
        {
            if (!ImageTypes.TryGetValue(Path.GetExtension(path), out var mime))
                return null;
            var info = new FileInfo(path);
            if (!info.Exists || info.Length > MaxInlineImageBytes)
                return null;
            return $"data:{mime};base64,{Convert.ToBase64String(File.ReadAllBytes(path))}";
        }
        catch (Exception)
        {
            return null;
        }
    }
}
