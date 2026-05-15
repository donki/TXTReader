using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Provider;
using TXTReader.Services;

namespace TXTReader
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, Exported = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
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
                    _ = MobileLogService.LogAsync($"OnResume: Processing pending file: {filePath}");

                    if (IsContentUri(filePath) || File.Exists(filePath))
                    {
                        if (!IsContentUri(filePath))
                        {
                            System.Diagnostics.Debug.WriteLine($"File exists, size: {new FileInfo(filePath).Length} bytes");
                            _ = MobileLogService.LogAsync($"OnResume: Local file exists, size: {new FileInfo(filePath).Length} bytes");
                        }
                        else
                        {
                            _ = MobileLogService.LogAsync("OnResume: Content URI detected, proceeding without file check");
                        }

                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            _ = MobileLogService.LogAsync($"OnResume: Notifying FileIntentService with: {filePath}");
                            FileIntentService.NotifyFileOpened(filePath);
                        });
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"File does not exist: {filePath}");
                        _ = MobileLogService.LogAsync($"OnResume: File does not exist: {filePath}");
                        MainThread.BeginInvokeOnMainThread(async () =>
                        {
                            await Task.Delay(500);
                            if (Microsoft.Maui.Controls.Application.Current?.MainPage != null)
                            {
                                await Microsoft.Maui.Controls.Application.Current.MainPage.DisplayAlertAsync(
                                    "Archivo no disponible",
                                    "No se pudo acceder al archivo seleccionado.\n\nEsto puede ocurrir con archivos de almacenamiento en la nube que no están disponibles sin conexión.\n\nIntenta descargar el archivo localmente primero.",
                                    "OK");
                            }
                        });
                    }
                });
            }
        }

        private void HandleIntent(Intent? intent)
        {
            try
            {
                if (intent?.Action == Intent.ActionView && intent.Data != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Handling intent with URI: {intent.Data}");
                    _ = MobileLogService.LogAsync($"HandleIntent: Received URI: {intent.Data}");

                    var filePath = GetReadableFileReference(intent.Data);
                    var fileName = GetFileNameFromUri(intent.Data);
                    System.Diagnostics.Debug.WriteLine($"GetReadableFileReference returned: '{filePath}'");
                    _ = MobileLogService.LogAsync($"HandleIntent: GetReadableFileReference returned: '{filePath}'");

                    if (!string.IsNullOrEmpty(filePath))
                    {
                        var extension = GetExtension(fileName, filePath);
                        var supportedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                        {
                            ".txt", ".log", ".json", ".xml", ".csv", ".md", ".ini", ".cfg", ".conf"
                        };

                        if (supportedExtensions.Contains(extension) || string.IsNullOrEmpty(extension))
                        {
                            System.Diagnostics.Debug.WriteLine($"Intent received for file: {filePath} (extension: {extension})");
                            _ = MobileLogService.LogAsync($"HandleIntent: Setting _pendingFilePath to: '{filePath}' (extension: {extension})");
                            _pendingFilePath = filePath;
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"Unsupported file type: {extension}");
                            _ = MobileLogService.LogAsync($"HandleIntent: Unsupported file type: {extension}");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Failed to get file path from URI");
                        MainThread.BeginInvokeOnMainThread(async () =>
                        {
                            await Task.Delay(2000);
                            if (Microsoft.Maui.Controls.Application.Current?.MainPage != null)
                            {
                                var uriString = intent.Data?.ToString()?.ToLowerInvariant() ?? "";
                                string message;
                                string title;

                                if (uriString.Contains("onedrive"))
                                {
                                    title = "OneDrive - Acceso restringido";
                                    message = "OneDrive no permite acceso directo. Descarga el archivo primero:\n\n1. Abre el archivo en OneDrive\n2. Toca los 3 puntos (...)\n3. Selecciona 'Descargar'\n4. Abre desde Descargas";
                                }
                                else if (uriString.Contains("drive.google"))
                                {
                                    title = "Google Drive - Error de acceso";
                                    message = "No se pudo acceder al archivo de Google Drive.\n\nIntenta:\n1. Asegúrate de tener conexión a internet\n2. Verifica que tienes permisos para el archivo\n3. Descarga el archivo localmente si persiste el problema";
                                }
                                else if (uriString.Contains("dropbox"))
                                {
                                    title = "Dropbox - Error de acceso";
                                    message = "No se pudo acceder al archivo de Dropbox.\n\nIntenta:\n1. Verifica tu conexión a internet\n2. Asegúrate de que el archivo esté sincronizado\n3. Abre el archivo en Dropbox y selecciona 'Exportar'";
                                }
                                else
                                {
                                    title = "Archivo no accesible";
                                    message = "No se pudo acceder al archivo desde el almacenamiento en la nube.\n\nIntenta descargar el archivo localmente primero.";
                                }

                                await Microsoft.Maui.Controls.Application.Current.MainPage.DisplayAlertAsync(title, message, "OK");
                            }
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error handling intent: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        private string? GetReadableFileReference(Android.Net.Uri uri)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Processing URI: {uri}");
                System.Diagnostics.Debug.WriteLine($"URI Scheme: {uri.Scheme}");
                System.Diagnostics.Debug.WriteLine($"URI Path: {uri.Path}");

                if (uri.Scheme?.Equals("file", StringComparison.OrdinalIgnoreCase) == true)
                {
                    var path = uri.Path;
                    System.Diagnostics.Debug.WriteLine($"File URI path: {path}");
                    return path;
                }

                if (uri.Scheme?.Equals("content", StringComparison.OrdinalIgnoreCase) == true)
                {
                    _ = MobileLogService.LogAsync($"GetReadableFileReference: Returning content URI directly: {uri}");
                    return uri.ToString();
                }

                return uri.Path;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetReadableFileReference: {ex.Message}");
                return null;
            }
        }

        private string? GetFileNameFromUri(Android.Net.Uri uri)
        {
            try
            {
                var cursor = ContentResolver?.Query(uri, new[] { IOpenableColumns.DisplayName }, null, null, null);
                if (cursor != null)
                {
                    try
                    {
                        if (cursor.MoveToFirst())
                        {
                            var displayNameIndex = cursor.GetColumnIndex(IOpenableColumns.DisplayName);
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

                return uri.LastPathSegment;
            }
            catch
            {
                return null;
            }
        }

        private static string GetExtension(string? fileName, string filePath)
        {
            var candidate = !string.IsNullOrWhiteSpace(fileName) ? fileName : filePath;
            return Path.GetExtension(candidate)?.ToLowerInvariant() ?? string.Empty;
        }

        private static bool IsContentUri(string value)
        {
            return value.StartsWith("content://", StringComparison.OrdinalIgnoreCase);
        }
    }
}
