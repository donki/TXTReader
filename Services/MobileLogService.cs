using System.Text;

namespace TXTReader.Services
{
    /// <summary>
    /// Registro de depuracion de la apertura de ficheros (LocalApplicationData/txtreader_debug.log).
    /// Nunca falla: un error al escribir se ignora para no estorbar a la app.
    /// </summary>
    public static class MobileLogService
    {
        /// <summary>Fichero del registro (las pruebas lo llevan a una carpeta temporal).</summary>
        public static string LogFilePath { get; set; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "txtreader_debug.log");

        public static async Task LogAsync(string message)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                var logEntry = $"[{timestamp}] {message}{Environment.NewLine}";

                await File.AppendAllTextAsync(LogFilePath, logEntry, Encoding.UTF8);

                // También escribir en Debug para desarrollo
                System.Diagnostics.Debug.WriteLine($"[LOG] {message}");
            }
            catch
            {
                // Ignorar errores de logging para no afectar la funcionalidad principal
            }
        }
    }
}
