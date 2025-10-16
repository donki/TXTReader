using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using TXTReader.Services;

namespace TXTReader
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    [IntentFilter(new[] { Intent.ActionView }, Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable }, DataMimeType = "text/*")]
    [IntentFilter(new[] { Intent.ActionView }, Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable }, DataMimeType = "application/json")]
    [IntentFilter(new[] { Intent.ActionView }, Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable }, DataMimeType = "application/xml")]
    public class MainActivity : MauiAppCompatActivity
    {
        private string? _pendingFilePath;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            HandleIntent(Intent);
        }

        protected override void OnNewIntent(Intent? intent)
        {
            base.OnNewIntent(intent);
            if (intent != null)
            {
                HandleIntent(intent);
            }
        }

        protected override void OnResume()
        {
            base.OnResume();
            
            // Si hay un archivo pendiente, procesarlo después de que la aplicación esté completamente cargada
            if (!string.IsNullOrEmpty(_pendingFilePath))
            {
                var filePath = _pendingFilePath;
                _pendingFilePath = null;
                
                // Retrasar la notificación para asegurar que la aplicación esté lista
                Task.Delay(1000).ContinueWith(_ =>
                {
                    System.Diagnostics.Debug.WriteLine($"Processing pending file: {filePath}");
                    FileIntentService.NotifyFileOpened(filePath);
                });
            }
        }

        private void HandleIntent(Intent? intent)
        {
            try
            {
                if (intent?.Action == Intent.ActionView && intent.Data != null)
                {
                    var filePath = GetRealPathFromUri(intent.Data);
                    if (!string.IsNullOrEmpty(filePath))
                    {
                        // Verificar si es un archivo soportado
                        var extension = Path.GetExtension(filePath)?.ToLowerInvariant();
                        var supportedExtensions = new[] { ".txt", ".log", ".json", ".xml", ".csv", ".md", ".ini", ".cfg", ".conf" };
                        
                        if (supportedExtensions.Contains(extension) || string.IsNullOrEmpty(extension))
                        {
                            System.Diagnostics.Debug.WriteLine($"Intent received for file: {filePath} (extension: {extension})");
                            _pendingFilePath = filePath;
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"Unsupported file type: {extension}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error handling intent: {ex.Message}");
            }
        }

        private string? GetRealPathFromUri(Android.Net.Uri uri)
        {
            try
            {
                if (uri.Scheme?.Equals("file", StringComparison.OrdinalIgnoreCase) == true)
                {
                    return uri.Path;
                }
                
                // Para content:// URIs, intentar obtener la ruta real
                var cursor = ContentResolver?.Query(uri, null, null, null, null);
                if (cursor != null)
                {
                    cursor.MoveToFirst();
                    var columnIndex = cursor.GetColumnIndex("_data");
                    if (columnIndex >= 0)
                    {
                        var path = cursor.GetString(columnIndex);
                        cursor.Close();
                        return path;
                    }
                    cursor.Close();
                }
                
                return uri.Path;
            }
            catch
            {
                return null;
            }
        }
    }
}