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
                Task.Delay(1500).ContinueWith(_ =>
                {
                    System.Diagnostics.Debug.WriteLine($"Processing pending file: {filePath}");
                    // Asegurar que la notificación se ejecute en el hilo principal
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        FileIntentService.NotifyFileOpened(filePath);
                    });
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
                System.Diagnostics.Debug.WriteLine($"Processing URI: {uri}");
                System.Diagnostics.Debug.WriteLine($"URI Scheme: {uri.Scheme}");
                System.Diagnostics.Debug.WriteLine($"URI Path: {uri.Path}");

                // Para URIs file://
                if (uri.Scheme?.Equals("file", StringComparison.OrdinalIgnoreCase) == true)
                {
                    var path = uri.Path;
                    System.Diagnostics.Debug.WriteLine($"File URI path: {path}");
                    return path;
                }

                // Para URIs content://
                if (uri.Scheme?.Equals("content", StringComparison.OrdinalIgnoreCase) == true)
                {
                    // Intentar múltiples métodos para obtener la ruta

                    // Método 1: Usar cursor con _data
                    var cursor = ContentResolver?.Query(uri, null, null, null, null);
                    if (cursor != null)
                    {
                        try
                        {
                            if (cursor.MoveToFirst())
                            {
                                var columnIndex = cursor.GetColumnIndex("_data");
                                if (columnIndex >= 0)
                                {
                                    var path = cursor.GetString(columnIndex);
                                    if (!string.IsNullOrEmpty(path))
                                    {
                                        System.Diagnostics.Debug.WriteLine($"Content URI path from _data: {path}");
                                        return path;
                                    }
                                }

                                // Método 2: Intentar con _display_name para obtener el nombre del archivo
                                var displayNameIndex = cursor.GetColumnIndex("_display_name");
                                if (displayNameIndex >= 0)
                                {
                                    var displayName = cursor.GetString(displayNameIndex);
                                    System.Diagnostics.Debug.WriteLine($"Display name: {displayName}");
                                }
                            }
                        }
                        finally
                        {
                            cursor.Close();
                        }
                    }

                    // Método 3: Para URIs de Downloads, construir la ruta
                    if (uri.ToString().Contains("downloads"))
                    {
                        var fileName = GetFileNameFromUri(uri);
                        if (!string.IsNullOrEmpty(fileName))
                        {
                            var downloadsPath = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads)?.AbsolutePath;
                            if (!string.IsNullOrEmpty(downloadsPath))
                            {
                                var fullPath = Path.Combine(downloadsPath, fileName);
                                System.Diagnostics.Debug.WriteLine($"Constructed downloads path: {fullPath}");
                                return fullPath;
                            }
                        }
                    }

                    // Método 4: Copiar el archivo a un directorio temporal
                    return CopyUriToTempFile(uri);
                }

                return uri.Path;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetRealPathFromUri: {ex.Message}");
                return null;
            }
        }

        private string? GetFileNameFromUri(Android.Net.Uri uri)
        {
            try
            {
                var cursor = ContentResolver?.Query(uri, new[] { "_display_name" }, null, null, null);
                if (cursor != null)
                {
                    try
                    {
                        if (cursor.MoveToFirst())
                        {
                            var displayNameIndex = cursor.GetColumnIndex("_display_name");
                            if (displayNameIndex >= 0)
                            {
                                return cursor.GetString(displayNameIndex);
                            }
                        }
                    }
                    finally
                    {
                        cursor.Close();
                    }
                }

                // Fallback: extraer del URI
                var lastSegment = uri.LastPathSegment;
                return lastSegment;
            }
            catch
            {
                return null;
            }
        }

        private string? CopyUriToTempFile(Android.Net.Uri uri)
        {
            try
            {
                var fileName = GetFileNameFromUri(uri) ?? "temp_file.txt";
                var tempDir = Path.Combine(Android.App.Application.Context.CacheDir?.AbsolutePath ?? "", "temp_files");

                if (!Directory.Exists(tempDir))
                {
                    Directory.CreateDirectory(tempDir);
                }

                var tempFilePath = Path.Combine(tempDir, fileName);

                using var inputStream = ContentResolver?.OpenInputStream(uri);
                if (inputStream != null)
                {
                    using var outputStream = File.Create(tempFilePath);
                    inputStream.CopyTo(outputStream);

                    System.Diagnostics.Debug.WriteLine($"Copied URI to temp file: {tempFilePath}");
                    return tempFilePath;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error copying URI to temp file: {ex.Message}");
            }

            return null;
        }
    }
}