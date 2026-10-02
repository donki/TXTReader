using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Provider;
using AndroidX.Core.View;
using TXTReader.Services;
using AndroidView = Android.Views.View;

namespace TXTReader
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, Exported = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    [IntentFilter(new[] { Intent.ActionView }, Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable }, DataMimeType = "text/*")]
    [IntentFilter(new[] { Intent.ActionView }, Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable }, DataMimeType = "application/json")]
    [IntentFilter(new[] { Intent.ActionView }, Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable }, DataMimeType = "application/xml")]
    // Los .gpx son XML: se aceptan por su MIME propio cuando la app que comparte lo declara.
    // Android no lo hace por si solo (no conoce la extension), asi que la via habitual es el
    // selector de ficheros, que ya los admite.
    [IntentFilter(new[] { Intent.ActionView }, Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable }, DataMimeType = "application/gpx+xml")]
    public class MainActivity : MauiAppCompatActivity
    {
        // La logica de la apertura desde otra app (que se lee, que extensiones, avisos) vive en
        // IntentFileHandler, que tiene pruebas; aqui solo queda lo que es de Android.
        private readonly IntentFileHandler _intentFiles = new();

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            AppPlatform.MoveTaskToBack = () => MoveTaskToBack(true);
            AppPlatform.OpenContentUri = uri =>
                (ContentResolver ?? throw new InvalidOperationException("ContentResolver is null"))
                    .OpenInputStream(Android.Net.Uri.Parse(uri)!);
            ApplySystemBarInsets();
            HandleIntent(Intent);
        }

        // Android 15 dibuja de borde a borde: separa el contenido del reloj y de la barra inferior.
        private void ApplySystemBarInsets()
        {
            var content = FindViewById(global::Android.Resource.Id.Content);
            if (content is null) return;
            content.SetBackgroundColor(global::Android.Graphics.Color.ParseColor("#2A1CB8")); // indigo de marca
            ViewCompat.SetOnApplyWindowInsetsListener(content, new SystemBarInsetsListener());
            var controller = Window is not null ? WindowCompat.GetInsetsController(Window, Window.DecorView) : null;
            if (controller is not null)
            {
                controller.AppearanceLightStatusBars = false;
                controller.AppearanceLightNavigationBars = false;
            }
        }

        private class SystemBarInsetsListener : Java.Lang.Object, IOnApplyWindowInsetsListener
        {
            public WindowInsetsCompat OnApplyWindowInsets(AndroidView? view, WindowInsetsCompat? insets)
            {
                var consumed = WindowInsetsCompat.Consumed!;
                if (view is null || insets is null) return consumed;
                var bars = insets.GetInsets(WindowInsetsCompat.Type.SystemBars() | WindowInsetsCompat.Type.DisplayCutout());
                if (bars is not null) view.SetPadding(bars.Left, bars.Top, bars.Right, bars.Bottom);
                return consumed;
            }
        }

        protected override void OnNewIntent(Intent? intent)
        {
            base.OnNewIntent(intent);
            HandleIntent(intent);
        }

        protected override void OnResume()
        {
            base.OnResume();
            _ = _intentFiles.ProcessPendingAsync();
        }

        private void HandleIntent(Intent? intent)
        {
            if (intent?.Action != Intent.ActionView || intent.Data is not { } uri)
                return;
            _ = _intentFiles.HandleViewIntentAsync(uri.ToString() ?? string.Empty, uri.Scheme, uri.Path, GetDisplayName(uri));
        }

        // Nombre visible del fichero (el de la columna DISPLAY_NAME del proveedor o, si no, el
        // ultimo segmento de la URI): de el sale la extension.
        private string? GetDisplayName(Android.Net.Uri uri)
        {
            try
            {
                var cursor = ContentResolver?.Query(uri, new[] { IOpenableColumns.DisplayName }, null, null, null);
                try
                {
                    var index = cursor?.GetColumnIndex(IOpenableColumns.DisplayName) ?? -1;
                    if (cursor != null && index >= 0 && cursor.MoveToFirst())
                        return cursor.GetString(index);
                }
                finally
                {
                    cursor?.Close();
                }
                return uri.LastPathSegment;
            }
            catch
            {
                return null;
            }
        }
    }
}
