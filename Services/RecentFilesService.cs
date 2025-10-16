using System.Text.Json;

namespace TXTReader.Services
{
    public class RecentFilesService
    {
        private const string RecentFilesKey = "recent_files";
        private const int MaxRecentFiles = 5;

        public async Task AddRecentFileAsync(string filePath, string fileName)
        {
            var recentFiles = await GetRecentFilesAsync();
            
            // Remover si ya existe
            recentFiles.RemoveAll(f => f.FilePath == filePath);
            
            // Agregar al inicio
            recentFiles.Insert(0, new RecentFile { FilePath = filePath, FileName = fileName, LastOpened = DateTime.Now });
            
            // Mantener solo los últimos archivos
            if (recentFiles.Count > MaxRecentFiles)
            {
                recentFiles = recentFiles.Take(MaxRecentFiles).ToList();
            }
            
            await SaveRecentFilesAsync(recentFiles);
        }

        public async Task<List<RecentFile>> GetRecentFilesAsync()
        {
            try
            {
                var json = await SecureStorage.GetAsync(RecentFilesKey);
                if (string.IsNullOrEmpty(json))
                    return new List<RecentFile>();

                return JsonSerializer.Deserialize<List<RecentFile>>(json) ?? new List<RecentFile>();
            }
            catch
            {
                return new List<RecentFile>();
            }
        }

        private async Task SaveRecentFilesAsync(List<RecentFile> recentFiles)
        {
            try
            {
                var json = JsonSerializer.Serialize(recentFiles);
                await SecureStorage.SetAsync(RecentFilesKey, json);
            }
            catch
            {
                // Ignorar errores de guardado
            }
        }

        public async Task<List<RecentFile>> GetValidRecentFilesAsync()
        {
            var recentFiles = await GetRecentFilesAsync();
            var validFiles = new List<RecentFile>();
            bool hasChanges = false;

            foreach (var file in recentFiles)
            {
                if (File.Exists(file.FilePath))
                {
                    validFiles.Add(file);
                }
                else
                {
                    hasChanges = true;
                    System.Diagnostics.Debug.WriteLine($"Removing non-existent file from recent files: {file.FilePath}");
                }
            }

            // Si hubo cambios, guardar la lista actualizada
            if (hasChanges)
            {
                await SaveRecentFilesAsync(validFiles);
            }

            return validFiles;
        }

        public async Task RemoveRecentFileAsync(string filePath)
        {
            var recentFiles = await GetRecentFilesAsync();
            var initialCount = recentFiles.Count;
            
            recentFiles.RemoveAll(f => f.FilePath == filePath);
            
            if (recentFiles.Count != initialCount)
            {
                await SaveRecentFilesAsync(recentFiles);
            }
        }

        public Task ClearRecentFilesAsync()
        {
            SecureStorage.Remove(RecentFilesKey);
            return Task.CompletedTask;
        }
    }

    public class RecentFile
    {
        public string FilePath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public DateTime LastOpened { get; set; }
    }
}