// Doble de TXTReader.Services.MobileLogService: el real escribe en %LOCALAPPDATA%\txtreader_debug.log,
// que en el PC de pruebas seria un fichero ajeno. Aqui guarda los mensajes en memoria.
namespace TXTReader.Services;

public static class MobileLogService
{
    public static List<string> Messages { get; } = new();

    public static Task LogAsync(string message)
    {
        lock (Messages)
            Messages.Add(message);
        return Task.CompletedTask;
    }
}
