using System.Text;

namespace TXTReader.Services
{
    public class EncodingDetectionService
    {
        public static async Task<(string content, string detectedEncoding)> ReadFileWithEncodingDetectionAsync(string filePathOrUri)
        {
            try
            {
                await MobileLogService.LogAsync($"ReadFileWithEncodingDetectionAsync called with: {filePathOrUri}");
                byte[] bytes;
                
                // Verificar si es una URI de content
                if (filePathOrUri.StartsWith("content://"))
                {
                    await MobileLogService.LogAsync($"Detected content URI, attempting to read: {filePathOrUri}");
                    bytes = await ReadContentUriAsync(filePathOrUri);
                    await MobileLogService.LogAsync($"Successfully read {bytes.Length} bytes from content URI");
                }
                else
                {
                    await MobileLogService.LogAsync($"Detected local file path, reading: {filePathOrUri}");
                    bytes = await File.ReadAllBytesAsync(filePathOrUri);
                    await MobileLogService.LogAsync($"Successfully read {bytes.Length} bytes from local file");
                }
                
                await MobileLogService.LogAsync($"Detecting encoding for {bytes.Length} bytes");
                var encoding = DetectEncoding(bytes);
                await MobileLogService.LogAsync($"Detected encoding: {encoding.EncodingName}");
                
                var content = encoding.GetString(bytes);
                await MobileLogService.LogAsync($"Converted to string, length: {content.Length} characters");
                
                // Limpiar BOM si existe
                if (content.Length > 0 && content[0] == '\uFEFF')
                {
                    content = content.Substring(1);
                    await MobileLogService.LogAsync("Removed BOM from content");
                }
                
                return (content, encoding.EncodingName);
            }
            catch (Exception ex)
            {
                await MobileLogService.LogAsync($"ERROR in ReadFileWithEncodingDetectionAsync: {ex.Message}");
                await MobileLogService.LogAsync($"Stack trace: {ex.StackTrace}");
                
                // Fallback: intentar leer como archivo local con UTF-8
                try
                {
                    if (!filePathOrUri.StartsWith("content://"))
                    {
                        await MobileLogService.LogAsync("Attempting fallback to UTF-8 local file read");
                        var content = await File.ReadAllTextAsync(filePathOrUri, Encoding.UTF8);
                        return (content, "UTF-8");
                    }
                }
                catch (Exception fallbackEx)
                {
                    await MobileLogService.LogAsync($"Fallback also failed: {fallbackEx.Message}");
                }
                
                throw; // Re-lanzar la excepción original
            }
        }

        private static async Task<byte[]> ReadContentUriAsync(string contentUri)
        {
            try
            {
#if ANDROID
                await MobileLogService.LogAsync($"ReadContentUriAsync: Parsing URI: {contentUri}");
                var uri = Android.Net.Uri.Parse(contentUri);
                
                await MobileLogService.LogAsync($"ReadContentUriAsync: Getting context and content resolver");
                var context = Platform.CurrentActivity ?? Android.App.Application.Context;
                var contentResolver = context.ContentResolver;
                
                if (contentResolver == null)
                {
                    throw new InvalidOperationException("ContentResolver is null");
                }
                
                await MobileLogService.LogAsync($"ReadContentUriAsync: Opening input stream for URI: {uri}");
                using var inputStream = contentResolver.OpenInputStream(uri);
                if (inputStream == null)
                {
                    throw new InvalidOperationException("Could not open content URI stream - inputStream is null");
                }
                
                await MobileLogService.LogAsync($"ReadContentUriAsync: Successfully opened input stream, copying to memory");
                using var memoryStream = new MemoryStream();
                await inputStream.CopyToAsync(memoryStream);
                
                var bytes = memoryStream.ToArray();
                await MobileLogService.LogAsync($"ReadContentUriAsync: Successfully read {bytes.Length} bytes from content URI");
                return bytes;
#else
                throw new PlatformNotSupportedException("Content URIs are only supported on Android");
#endif
            }
            catch (Exception ex)
            {
                await MobileLogService.LogAsync($"ERROR reading content URI: {ex.Message}");
                await MobileLogService.LogAsync($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        private static Encoding DetectEncoding(byte[] bytes)
        {
            if (bytes.Length < 2)
                return Encoding.UTF8;

            // Detectar BOM
            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
                return Encoding.UTF8;

            if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
                return Encoding.Unicode; // UTF-16 LE

            if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
                return Encoding.BigEndianUnicode; // UTF-16 BE

            if (bytes.Length >= 4 && bytes[0] == 0xFF && bytes[1] == 0xFE && bytes[2] == 0x00 && bytes[3] == 0x00)
                return Encoding.UTF32; // UTF-32 LE

            if (bytes.Length >= 4 && bytes[0] == 0x00 && bytes[1] == 0x00 && bytes[2] == 0xFE && bytes[3] == 0xFF)
                return new UTF32Encoding(true, true); // UTF-32 BE

            // Heurística para detectar codificación sin BOM
            if (IsValidUTF8(bytes))
                return Encoding.UTF8;

            // Detectar Windows-1252 (ANSI)
            if (ContainsExtendedASCII(bytes))
                return Encoding.GetEncoding("windows-1252");

            // Por defecto, usar UTF-8
            return Encoding.UTF8;
        }

        private static bool IsValidUTF8(byte[] bytes)
        {
            try
            {
                var decoder = Encoding.UTF8.GetDecoder();
                decoder.Fallback = DecoderFallback.ExceptionFallback;
                
                var chars = new char[bytes.Length];
                decoder.GetChars(bytes, 0, bytes.Length, chars, 0, true);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool ContainsExtendedASCII(byte[] bytes)
        {
            foreach (byte b in bytes)
            {
                if (b > 127 && b < 160) // Rango típico de Windows-1252
                    return true;
            }
            return false;
        }
    }
}