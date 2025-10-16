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

        private void HandleIntent(Intent? intent)
        {
            if (intent?.Action == Intent.ActionView && intent.Data != null)
            {
                var filePath = GetRealPathFromUri(intent.Data);
                if (!string.IsNullOrEmpty(filePath))
                {
                    // Pasar el archivo a la aplicación MAUI
                    FileIntentService.NotifyFileOpened(filePath);
                }
            }
        }

        private string? GetRealPathFromUri(Android.Net.Uri uri)
        {
            try
            {
                if (uri.Scheme?.Equals("file", StringComparison.OrdinalIgnoreCase) == true)
                {
                    return uri.Path;
                }
                
                // Para content:// URIs, intentar obtener la ruta real
                var cursor = ContentResolver?.Query(uri, null, null, null, null);
                if (cursor != null)
                {
                    cursor.MoveToFirst();
                    var columnIndex = cursor.GetColumnIndex("_data");
                    if (columnIndex >= 0)
                    {
                        var path = cursor.GetString(columnIndex);
                        cursor.Close();
                        return path;
                    }
                    cursor.Close();
                }
                
                return uri.Path;
            }
            catch
            {
                return null;
            }
        }
    }
}