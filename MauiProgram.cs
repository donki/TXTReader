using Microsoft.Extensions.Logging;
using TXTReader.Services;

namespace TXTReader
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>();

            // Initialize localization service
            _ = LocalizationService.Instance;

            // Dependency injection registration (constitucion.md, seccion 4).
            // NOTA: los servicios de utilidad estaticos (Encoding, FileIntent, MobileLog)
            // se migraran a inyeccion como mejora incremental planificada (seccion 13).
            builder.Services.AddSingleton(LocalizationService.Instance);
            builder.Services.AddSingleton<RecentFilesService>();

#if DEBUG
            builder.Services.AddLogging(logging =>
            {
                logging.AddDebug();
            });
#endif

            return builder.Build();
        }
    }
}