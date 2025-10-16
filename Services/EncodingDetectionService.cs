using System.Text;

namespace TXTReader.Services
{
    public class EncodingDetectionService
    {
        public static async Task<(string content, string detectedEncoding)> ReadFileWithEncodingDetectionAsync(string filePath)
        {
            try
            {
                // Leer los primeros bytes para detectar BOM
                var bytes = await File.ReadAllBytesAsync(filePath);
                var encoding = DetectEncoding(bytes);
                
                var content = encoding.GetString(bytes);
                
                // Limpiar BOM si existe
                if (content.Length > 0 && content[0] == '\uFEFF')
                {
                    content = content.Substring(1);
                }
                
                return (content, encoding.EncodingName);
            }
            catch (Exception)
            {
                // Fallback a UTF-8
                var content = await File.ReadAllTextAsync(filePath, Encoding.UTF8);
                return (content, "UTF-8");
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