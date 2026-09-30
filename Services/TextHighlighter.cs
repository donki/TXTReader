using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace TXTReader.Services;

/// <summary>
/// Turns the text of a document into safe HTML with every occurrence of the search term wrapped in
/// &lt;mark&gt;. Kept apart from the view so it can be tested without the interface (General 8.6).
/// </summary>
public static class TextHighlighter
{
    /// <summary>Longest time the search may take before the text is shown without highlights.</summary>
    public static readonly TimeSpan SearchTimeout = TimeSpan.FromSeconds(1);

    /// <summary>
    /// HTML-encodes <paramref name="text"/> and marks each match of <paramref name="term"/>
    /// (literal, not a regular expression). The matches are found in the original text and each
    /// piece is encoded separately: searching the already encoded text would miss "&amp;", "&lt;"
    /// or quotes and would break entities when the term is part of one ("amp" inside "&amp;amp;").
    /// On a timeout or any regex failure the text is returned encoded but without highlights.
    /// </summary>
    public static string ToHighlightedHtml(string? text, string? term, bool caseSensitive, TimeSpan? timeout = null)
    {
        text ??= string.Empty;
        if (string.IsNullOrEmpty(term))
            return WebUtility.HtmlEncode(text);

        var options = RegexOptions.Multiline | RegexOptions.CultureInvariant;
        if (!caseSensitive)
            options |= RegexOptions.IgnoreCase;

        try
        {
            var regex = new Regex(Regex.Escape(term), options, timeout ?? SearchTimeout);
            var html = new StringBuilder(text.Length + 64);
            var last = 0;

            foreach (Match match in regex.Matches(text))
            {
                html.Append(WebUtility.HtmlEncode(text[last..match.Index]));
                html.Append("<mark>").Append(WebUtility.HtmlEncode(match.Value)).Append("</mark>");
                last = match.Index + match.Length;
            }

            html.Append(WebUtility.HtmlEncode(text[last..]));
            return html.ToString();
        }
        catch (RegexMatchTimeoutException)
        {
            // Documento muy grande o termino muy frecuente: el texto SIN resaltar en lugar de tumbar
            // la app (crash observado en Galaxy S10+/Android 12 al teclear).
            return WebUtility.HtmlEncode(text);
        }
    }
}
