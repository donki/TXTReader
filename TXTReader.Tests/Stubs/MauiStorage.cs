// Dobles en memoria de lo que la app usa de Microsoft.Maui.Storage, con las mismas firmas, para
// probar sin MAUI ni dispositivo (y sin tocar las preferencias reales de nadie).
namespace Microsoft.Maui.Storage;

public static class Preferences
{
    private static readonly Dictionary<string, string> Store = new();

    public static void Clear() => Store.Clear();

    public static string Get(string key, string defaultValue) => Store.TryGetValue(key, out var v) ? v : defaultValue;

    public static void Set(string key, string value) => Store[key] = value;

    public static void Remove(string key) => Store.Remove(key);
}

public static class SecureStorage
{
    private static readonly Dictionary<string, string> Store = new();

    /// <summary>Si no es null, la siguiente lectura o escritura falla con esta excepcion (Keystore roto).</summary>
    public static Exception? FailWith { get; set; }

    public static void Clear()
    {
        Store.Clear();
        FailWith = null;
    }

    public static Task<string?> GetAsync(string key)
    {
        if (FailWith is not null) throw FailWith;
        return Task.FromResult(Store.TryGetValue(key, out var v) ? v : null);
    }

    public static Task SetAsync(string key, string value)
    {
        if (FailWith is not null) throw FailWith;
        Store[key] = value;
        return Task.CompletedTask;
    }

    public static bool Remove(string key) => Store.Remove(key);
}
