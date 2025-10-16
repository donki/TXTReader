namespace TXTReader.Services
{
    public static class FileIntentService
    {
        public static event Action<string>? FileOpened;

        public static void NotifyFileOpened(string filePath)
        {
            FileOpened?.Invoke(filePath);
        }
    }
}