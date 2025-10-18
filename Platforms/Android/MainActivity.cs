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
                    _ = MobileLogService.LogAsync($"OnResume: Processing pending file: {filePath}");
                    
                    // Verificar que el archivo existe antes de procesarlo (solo para archivos locales)
                    if (filePath.StartsWith("content://") || File.Exists(filePath))
                    {
                        if (!filePath.StartsWith("content://"))
                        {
                            System.Diagnostics.Debug.WriteLine($"File exists, size: {new FileInfo(filePath).Length} bytes");
                            _ = MobileLogService.LogAsync($"OnResume: Local file exists, size: {new FileInfo(filePath).Length} bytes");
                        }
                        else
                        {
                            _ = MobileLogService.LogAsync($"OnResume: Content URI detected, proceeding without file check");
                        }
                        
                        // Asegurar que la notificación se ejecute en el hilo principal
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
                                await Microsoft.Maui.Controls.Application.Current.MainPage.DisplayAlert(
                                    "Archivo no disponible", 
                                    "No se pudo crear una copia temporal del archivo.\n\nEsto puede ocurrir con archivos de almacenamiento en la nube que no están disponibles sin conexión.\n\nIntenta descargar el archivo localmente primero.", 
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
                    
                    var filePath = GetRealPathFromUri(intent.Data);
                    System.Diagnostics.Debug.WriteLine($"GetRealPathFromUri returned: '{filePath}'");
                    _ = MobileLogService.LogAsync($"HandleIntent: GetRealPathFromUri returned: '{filePath}'");
                    
                    if (!string.IsNullOrEmpty(filePath))
                    {
                        // Verificar si es un archivo soportado
                        string extension;
                        
                        if (filePath.StartsWith("content://"))
                        {
                            // Para URIs de content, extraer la extensión del nombre del archivo en la URI
                            var uri = Android.Net.Uri.Parse(filePath);
                            var lastSegment = uri.LastPathSegment ?? "";
                            
                            // Buscar .txt, .log, etc. en el último segmento de la URI
                            var dotIndex = lastSegment.LastIndexOf('.');
                            extension = dotIndex >= 0 ? lastSegment.Substring(dotIndex).ToLowerInvariant() : "";
                            
                            _ = MobileLogService.LogAsync($"HandleIntent: Content URI - LastPathSegment: '{lastSegment}', Extension: '{extension}'");
                        }
                        else
                        {
                            // Para archivos locales, usar Path.GetExtension normal
                            extension = Path.GetExtension(filePath)?.ToLowerInvariant() ?? "";
                        }
                        
                        var supportedExtensions = new[] { ".txt", ".log", ".json", ".xml", ".csv", ".md", ".ini", ".cfg", ".conf" };

                        if (supportedExtensions.Contains(extension) || string.IsNullOrEmpty(extension))
                        {
                            System.Diagnostics.Debug.WriteLine($"Intent received for file: {filePath} (extension: {extension})");
                            System.Diagnostics.Debug.WriteLine($"Setting _pendingFilePath to: '{filePath}'");
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
                        // Intentar mostrar un mensaje de error al usuario
                        MainThread.BeginInvokeOnMainThread(async () =>
                        {
                            await Task.Delay(2000); // Esperar a que la app esté lista
                            if (Microsoft.Maui.Controls.Application.Current?.MainPage != null)
                            {
                                var uriString = intent.Data?.ToString()?.ToLower() ?? "";
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
                                    
                                await Microsoft.Maui.Controls.Application.Current.MainPage.DisplayAlert(title, message, "OK");
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
                    _ = MobileLogService.LogAsync($"GetRealPathFromUri: Processing content URI: {uri}");
                    
                    // Para servicios en la nube (OneDrive, Google Drive, Dropbox), usar URI directamente
                    if (uri.Authority?.Contains("skydrive") == true || 
                        uri.Authority?.Contains("onedrive") == true ||
                        uri.Authority?.Contains("drive.google") == true ||
                        uri.Authority?.Contains("dropbox") == true)
                    {
                        _ = MobileLogService.LogAsync($"GetRealPathFromUri: Cloud service detected, returning URI directly: {uri}");
                        return uri.ToString();
                    }
                    
                    // Para archivos locales, intentar múltiples métodos para obtener la ruta

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
                                        _ = MobileLogService.LogAsync($"GetRealPathFromUri: Found local path: {path}");
                                        return path;
                                    }
                                }

                                // Método 2: Para archivos locales, intentar con _display_name solo para logging
                                var displayNameIndex = cursor.GetColumnIndex("_display_name");
                                if (displayNameIndex >= 0)
                                {
                                    var displayName = cursor.GetString(displayNameIndex);
                                    System.Diagnostics.Debug.WriteLine($"Display name: {displayName}");
                                    _ = MobileLogService.LogAsync($"GetRealPathFromUri: Display name found: {displayName}");
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

                    // Método 4: Intentar usar la URI de content directamente (como hace Edge)
                    System.Diagnostics.Debug.WriteLine("Attempting to use content URI directly");
                    _ = MobileLogService.LogAsync($"GetRealPathFromUri: Returning content URI directly: {uri}");
                    return uri.ToString();
                    
                    // Método 5: Copiar el archivo a un directorio temporal (fallback)
                    // var tempFilePath = CopyUriToTempFile(uri);
                    // System.Diagnostics.Debug.WriteLine($"CopyUriToTempFile returned: {tempFilePath}");
                    // return tempFilePath;
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
            const long MAX_FILE_SIZE = 50 * 1024 * 1024; // 50 MB límite máximo
            
            try
            {
                System.Diagnostics.Debug.WriteLine($"Attempting to copy URI to temp file: {uri}");
                System.Diagnostics.Debug.WriteLine($"URI Authority: {uri.Authority}");
                System.Diagnostics.Debug.WriteLine($"URI Host: {uri.Host}");
                
                var fileName = GetFileNameFromUri(uri) ?? $"temp_file_{DateTime.Now.Ticks}.txt";
                System.Diagnostics.Debug.WriteLine($"File name: {fileName}");
                
                // Usar el directorio de archivos de la aplicación en lugar del cache
                var tempDir = Path.Combine(Android.App.Application.Context.FilesDir?.AbsolutePath ?? "", "temp_files");

                if (!Directory.Exists(tempDir))
                {
                    Directory.CreateDirectory(tempDir);
                    System.Diagnostics.Debug.WriteLine($"Created temp directory: {tempDir}");
                }

                var tempFilePath = Path.Combine(tempDir, fileName);
                System.Diagnostics.Debug.WriteLine($"Target temp file path: {tempFilePath}");

                // Eliminar archivo existente si existe
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                    System.Diagnostics.Debug.WriteLine("Deleted existing temp file");
                }

                // Intentar obtener el tamaño del archivo primero
                long fileSize = -1;
                try
                {
                    var cursor = ContentResolver?.Query(uri, new[] { "_size" }, null, null, null);
                    if (cursor != null)
                    {
                        try
                        {
                            if (cursor.MoveToFirst())
                            {
                                var sizeIndex = cursor.GetColumnIndex("_size");
                                if (sizeIndex >= 0)
                                {
                                    fileSize = cursor.GetLong(sizeIndex);
                                    System.Diagnostics.Debug.WriteLine($"File size from cursor: {fileSize} bytes");
                                }
                            }
                        }
                        finally
                        {
                            cursor.Close();
                        }
                    }
                }
                catch (Exception sizeEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Could not get file size: {sizeEx.Message}");
                }

                // Verificar límite de tamaño si se pudo obtener
                if (fileSize > 0 && fileSize > MAX_FILE_SIZE)
                {
                    System.Diagnostics.Debug.WriteLine($"File too large: {fileSize} bytes (max: {MAX_FILE_SIZE})");
                    
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        await Task.Delay(500);
                        if (Microsoft.Maui.Controls.Application.Current?.MainPage != null)
                        {
                            var sizeMB = fileSize / (1024.0 * 1024.0);
                            await Microsoft.Maui.Controls.Application.Current.MainPage.DisplayAlert(
                                "Archivo demasiado grande", 
                                $"El archivo es demasiado grande ({sizeMB:F1} MB).\n\nTamaño máximo permitido: 50 MB\n\nPara archivos grandes, usa un editor de texto especializado.", 
                                "OK");
                        }
                    });
                    
                    return null;
                }

                // Intentar diferentes métodos para acceder al contenido
                System.IO.Stream? inputStream = null;
                
                try
                {
                    // Método 1: OpenInputStream directo (funciona con Google Drive, Dropbox, etc.)
                    inputStream = ContentResolver?.OpenInputStream(uri);
                    System.Diagnostics.Debug.WriteLine($"OpenInputStream result: {inputStream != null}");
                }
                catch (Exception ex1)
                {
                    System.Diagnostics.Debug.WriteLine($"OpenInputStream failed: {ex1.Message}");
                    
                    // Método 2: Intentar con FileDescriptor (para algunos proveedores)
                    try
                    {
                        var parcelFileDescriptor = ContentResolver?.OpenFileDescriptor(uri, "r");
                        if (parcelFileDescriptor != null)
                        {
                            var fileDescriptor = parcelFileDescriptor.FileDescriptor;
                            var javaInputStream = new Java.IO.FileInputStream(fileDescriptor);
                            inputStream = new Android.Runtime.InputStreamInvoker(javaInputStream);
                            System.Diagnostics.Debug.WriteLine("Successfully opened with FileDescriptor");
                        }
                    }
                    catch (Exception ex2)
                    {
                        System.Diagnostics.Debug.WriteLine($"FileDescriptor method failed: {ex2.Message}");
                        
                        // Método 3: Intentar con diferentes modos para OneDrive
                        if (uri.ToString().ToLower().Contains("onedrive") || uri.Authority?.Contains("skydrive") == true)
                        {
                            try
                            {
                                System.Diagnostics.Debug.WriteLine("Attempting OneDrive-specific access methods");
                                
                                // Intentar con modo "rw" en lugar de "r"
                                var parcelFd = ContentResolver?.OpenFileDescriptor(uri, "rw");
                                if (parcelFd != null)
                                {
                                    var fd = parcelFd.FileDescriptor;
                                    var javaStream = new Java.IO.FileInputStream(fd);
                                    inputStream = new Android.Runtime.InputStreamInvoker(javaStream);
                                    System.Diagnostics.Debug.WriteLine("OneDrive: Successfully opened with rw mode");
                                }
                            }
                            catch (Exception ex3)
                            {
                                System.Diagnostics.Debug.WriteLine($"OneDrive rw mode failed: {ex3.Message}");
                                
                                // Método 4: Intentar con AssetFileDescriptor
                                try
                                {
                                    var assetFd = ContentResolver?.OpenAssetFileDescriptor(uri, "r");
                                    if (assetFd != null)
                                    {
                                        var stream = assetFd.CreateInputStream();
                                        if (stream != null)
                                        {
                                            inputStream = stream;
                                            System.Diagnostics.Debug.WriteLine("OneDrive: Successfully opened with AssetFileDescriptor");
                                        }
                                    }
                                }
                                catch (Exception ex4)
                                {
                                    System.Diagnostics.Debug.WriteLine($"OneDrive AssetFileDescriptor failed: {ex4.Message}");
                                }
                            }
                        }
                    }
                }

                if (inputStream != null)
                {
                    System.Diagnostics.Debug.WriteLine("Successfully opened input stream");
                    
                    using (inputStream)
                    using (var outputStream = File.Create(tempFilePath))
                    {
                        // Copiar con límite de tamaño para evitar archivos enormes
                        var buffer = new byte[8192];
                        long totalBytesRead = 0;
                        int bytesRead;
                        
                        while ((bytesRead = inputStream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            totalBytesRead += bytesRead;
                            
                            // Verificar límite durante la copia
                            if (totalBytesRead > MAX_FILE_SIZE)
                            {
                                System.Diagnostics.Debug.WriteLine($"File exceeded size limit during copy: {totalBytesRead} bytes");
                                
                                MainThread.BeginInvokeOnMainThread(async () =>
                                {
                                    await Task.Delay(500);
                                    if (Microsoft.Maui.Controls.Application.Current?.MainPage != null)
                                    {
                                        await Microsoft.Maui.Controls.Application.Current.MainPage.DisplayAlert(
                                            "Archivo demasiado grande", 
                                            "El archivo excede el límite de 50 MB durante la carga.\n\nPara archivos grandes, usa un editor especializado.", 
                                            "OK");
                                    }
                                });
                                
                                if (File.Exists(tempFilePath))
                                {
                                    File.Delete(tempFilePath);
                                }
                                return null;
                            }
                            
                            outputStream.Write(buffer, 0, bytesRead);
                        }
                        
                        outputStream.Flush();
                    }

                    // Esperar un poco para asegurar que el archivo se escribió completamente
                    System.Threading.Thread.Sleep(200);

                    var fileInfo = new FileInfo(tempFilePath);
                    System.Diagnostics.Debug.WriteLine($"Copied URI to temp file: {tempFilePath} (Size: {fileInfo.Length} bytes)");
                    
                    if (fileInfo.Length > 0 && File.Exists(tempFilePath))
                    {
                        // Verificar que podemos leer el archivo
                        try
                        {
                            using var testStream = File.OpenRead(tempFilePath);
                            var testByte = testStream.ReadByte();
                            System.Diagnostics.Debug.WriteLine($"File is readable, first byte: {testByte}");
                            return tempFilePath;
                        }
                        catch (Exception readEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"Cannot read temp file: {readEx.Message}");
                            if (File.Exists(tempFilePath))
                            {
                                File.Delete(tempFilePath);
                            }
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Warning: Temp file is empty or doesn't exist");
                        if (File.Exists(tempFilePath))
                        {
                            File.Delete(tempFilePath);
                        }
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Failed to open input stream with all methods");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error copying URI to temp file: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            return null;
        }
    }
}