using Microsoft.Extensions.Logging;

namespace TXTReader
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>();

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