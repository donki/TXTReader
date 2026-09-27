using Microsoft.Extensions.Logging;
using TXTReader.Services;

namespace TXTReader
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            // Gestor global de excepciones (constitucion General 6.12): lo primero, antes de crear
            // nada. Registra en AppDataDirectory/crash.log y avisa en el idioma de la app sin cerrarla.
            SocShared.CrashGuard.Install("TXT Reader");

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

            // Comprobacion de version al arrancar (constitucion, seccion 15).
            builder.Services.AddSingleton<UpdateService>();

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