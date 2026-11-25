namespace TXTReader.Services
{
    public static class FileIntentService
    {
        /// <summary>Fichero abierto desde otra app: lo que se lee (ruta o URI) y el nombre que se enseña.</summary>
        public static event Action<string, string>? FileOpened;

        public static void NotifyFileOpened(string filePath, string fileName)
        {
            FileOpened?.Invoke(filePath, fileName);
        }
    }
}
