using System.Text;

namespace TXTReader.Services
{
    public static class MobileLogService
    {
        private static readonly string LogFilePath = Path.Combine(
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

        public static async Task<string> ReadLogsAsync()
        {
            try
            {
                if (File.Exists(LogFilePath))
                {
                    return await File.ReadAllTextAsync(LogFilePath, Encoding.UTF8);
                }
                return "No hay logs disponibles.";
            }
            catch (Exception ex)
            {
                return $"Error leyendo logs: {ex.Message}";
            }
        }

        public static async Task ClearLogsAsync()
        {
            try
            {
                if (File.Exists(LogFilePath))
                {
                    File.Delete(LogFilePath);
                }
                await LogAsync("=== LOGS CLEARED ===");
            }
            catch (Exception ex)
            {
                await LogAsync($"Error clearing logs: {ex.Message}");
            }
        }

        public static string GetLogFilePath()
        {
            return LogFilePath;
        }
    }
}