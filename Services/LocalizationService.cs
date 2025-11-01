using System.Globalization;
using System.Resources;
using TXTReader.Resources.Strings;

namespace TXTReader.Services
{
    public class LocalizationService
    {
        private static LocalizationService? _instance;
        private readonly ResourceManager _resourceManager;
        private CultureInfo _currentCulture;

        public static LocalizationService Instance => _instance ??= new LocalizationService();

        public event EventHandler? LanguageChanged;

        private LocalizationService()
        {
            try
            {
                _resourceManager = new ResourceManager(typeof(AppResources));
                
                // Debug: Log system language detection
                var systemLang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
                System.Diagnostics.Debug.WriteLine($"LocalizationService: System language detected: {systemLang}");
                
                var savedLang = Preferences.Get("app_language", "");
                System.Diagnostics.Debug.WriteLine($"LocalizationService: Saved language preference: '{savedLang}'");
                
                // If no preference is saved or it's "system", use system language
                if (string.IsNullOrEmpty(savedLang) || savedLang == "system")
                {
                    _currentCulture = GetSystemLanguage();
                    System.Diagnostics.Debug.WriteLine($"LocalizationService: Using system language: {_currentCulture.Name}");
                }
                else
                {
                    _currentCulture = GetSavedLanguage() ?? GetSystemLanguage();
                    System.Diagnostics.Debug.WriteLine($"LocalizationService: Using saved language: {_currentCulture.Name}");
                }
                
                SetCurrentCulture(_currentCulture);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LocalizationService: Error during initialization: {ex.Message}");
                // Fallback to English if there's any error
                _currentCulture = new CultureInfo("en");
                _resourceManager = new ResourceManager(typeof(AppResources));
            }
        }

        public string GetString(string key)
        {
            try
            {
                if (_resourceManager == null || _currentCulture == null)
                {
                    System.Diagnostics.Debug.WriteLine($"LocalizationService: ResourceManager or Culture is null for key: {key}");
                    return key;
                }
                return _resourceManager.GetString(key, _currentCulture) ?? key;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LocalizationService: Error getting string for key '{key}': {ex.Message}");
                return key;
            }
        }

        public void SetLanguage(string languageCode)
        {
            var culture = languageCode switch
            {
                "es" => new CultureInfo("es"),
                "en" => new CultureInfo("en"),
                "system" => GetSystemLanguage(),
                _ => new CultureInfo("en")
            };

            _currentCulture = culture;
            SetCurrentCulture(culture);
            SaveLanguagePreference(languageCode);
            LanguageChanged?.Invoke(this, EventArgs.Empty);
        }

        public string GetCurrentLanguageCode()
        {
            var saved = Preferences.Get("app_language", "");
            if (string.IsNullOrEmpty(saved))
            {
                return "system";
            }
            return saved;
        }

        public string GetCurrentLanguageName()
        {
            var code = GetCurrentLanguageCode();
            return code switch
            {
                "es" => GetString("Spanish"),
                "en" => GetString("English"),
                "system" => GetString("SystemDefault"),
                _ => GetString("English")
            };
        }

        private CultureInfo GetSystemLanguage()
        {
            var systemLanguage = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            
            // Support Spanish and English, fallback to English
            return systemLanguage switch
            {
                "es" => new CultureInfo("es"),
                "en" => new CultureInfo("en"),
                _ => new CultureInfo("en") // Default fallback
            };
        }

        private CultureInfo? GetSavedLanguage()
        {
            var savedLanguage = Preferences.Get("app_language", "system");
            System.Diagnostics.Debug.WriteLine($"LocalizationService: GetSavedLanguage returned: {savedLanguage}");
            
            return savedLanguage switch
            {
                "es" => new CultureInfo("es"),
                "en" => new CultureInfo("en"),
                "system" => null, // Will use system language
                _ => null // Default to system language
            };
        }

        private void SaveLanguagePreference(string languageCode)
        {
            Preferences.Set("app_language", languageCode);
        }

        private void SetCurrentCulture(CultureInfo culture)
        {
            try
            {
                if (culture != null)
                {
                    CultureInfo.CurrentCulture = culture;
                    CultureInfo.CurrentUICulture = culture;
                    Thread.CurrentThread.CurrentCulture = culture;
                    Thread.CurrentThread.CurrentUICulture = culture;
                    System.Diagnostics.Debug.WriteLine($"LocalizationService: Culture set to: {culture.Name}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LocalizationService: Error setting culture: {ex.Message}");
            }
        }

        public List<LanguageOption> GetAvailableLanguages()
        {
            return new List<LanguageOption>
            {
                new("system", GetString("SystemDefault")),
                new("en", GetString("English")),
                new("es", GetString("Spanish"))
            };
        }

        public void ResetToSystemLanguage()
        {
            Preferences.Remove("app_language");
            _currentCulture = GetSystemLanguage();
            SetCurrentCulture(_currentCulture);
            LanguageChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public record LanguageOption(string Code, string Name);
}