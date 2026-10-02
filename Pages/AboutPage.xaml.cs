using TXTReader.Services;

namespace TXTReader.Pages
{
    public partial class AboutPage : ContentPage
    {
        private const string ContactEmail = "jsoladelarosa@gmail.com";

        private readonly LocalizationService _localizationService;

        public AboutPage()
        {
            InitializeComponent();
            _localizationService = LocalizationService.Instance;
            _localizationService.LanguageChanged += OnLanguageChanged;

            UpdateLanguageButtons();
            UpdateTexts();
        }

        // Botones de idioma con bandera, iguales a las demas apps (ver constitucion, anexo A.9):
        // el idioma activo usa el estilo primario, el otro el de contorno.
        private void UpdateLanguageButtons()
        {
            // El idioma en que se ve la app, no el guardado: sin preferencia es "system" y en un
            // movil en castellano se marcaba «English» aunque todo estuviera en castellano.
            var isSpanish = _localizationService.CurrentLanguageCode == "es";
            SpanishButton.Style = LookupStyle(isSpanish ? "PrimaryButton" : "OutlineButton");
            EnglishButton.Style = LookupStyle(isSpanish ? "OutlineButton" : "PrimaryButton");
        }

        private static Style? LookupStyle(string key)
            => Application.Current?.Resources.TryGetValue(key, out var s) == true ? s as Style : null;

        private void UpdateTexts()
        {
            Title = _localizationService.GetString("AboutTitle");
            VersionLabel.Text = string.Format(_localizationService.GetString("AppVersion"), AppPlatform.AppInfo.VersionString);
            DescriptionLabel.Text = _localizationService.GetString("AppDescription");
            ContactTitleLabel.Text = _localizationService.GetString("ContactTitle");
            ContactInstructionLabel.Text = _localizationService.GetString("ContactInstruction");
            LanguageTitleLabel.Text = _localizationService.GetString("LanguageTitle");
            LanguageDescriptionLabel.Text = _localizationService.GetString("LanguageDescription");
            PrivacyTitleLabel.Text = _localizationService.GetString("PrivacyTitle");
            PrivacyTextLabel.Text = _localizationService.GetString("PrivacyText");
            LicenseTitleLabel.Text = _localizationService.GetString("LicenseTitle");
            LicenseTextLabel.Text = _localizationService.GetString("LicenseText");
            LegalTitleLabel.Text = _localizationService.GetString("LegalTitle");
            LegalText1Label.Text = _localizationService.GetString("LegalText1");
            LegalText2Label.Text = _localizationService.GetString("LegalText2");
            WarningTextLabel.Text = _localizationService.GetString("WarningText");
            BackButton.Text = _localizationService.GetString("BackButton");
        }

        private void OnLanguageChanged(object? sender, EventArgs e)
        {
            UpdateTexts();
            UpdateLanguageButtons();
        }

        private void OnSpanishClicked(object? sender, EventArgs e) => SetLanguage("es");

        private void OnEnglishClicked(object? sender, EventArgs e) => SetLanguage("en");

        private void SetLanguage(string code)
        {
            if (code == _localizationService.GetCurrentLanguageCode())
            {
                return;
            }

            _localizationService.SetLanguage(code);
        }

        private async void OnBackClicked(object? sender, EventArgs e) => await GoBackAsync();

        internal async Task GoBackAsync()
        {
            // Si se llego pulsando "Acerca de" en MainPage, hay pila que desapilar. Si se llego
            // por el menu hamburguesa (AboutPage es raiz de su seccion), la pila esta vacia:
            // en ese caso se vuelve a la pantalla principal por ruta de Shell.
            if (Navigation.NavigationStack.Count > 1)
                await Navigation.PopAsync();
            else if (Shell.Current != null)
                await Shell.Current.GoToAsync("//MainPage");
        }

        private async void OnContactEmailClicked(object? sender, EventArgs e) => await ContactByEmailAsync();

        /// <summary>
        /// Correo de contacto: el gestor de correo de MAUI; si falla, en Android un mailto: directo;
        /// y si tampoco, la direccion se copia al portapapeles (o se avisa de que no hay correo).
        /// </summary>
        internal async Task ContactByEmailAsync()
        {
            string? fallbackKey = null;
            try
            {
                var appName = "TXT Reader";
                var appVersion = AppPlatform.AppInfo.VersionString;
                var deviceInfo = $"{AppPlatform.DeviceInfo.Platform} {AppPlatform.DeviceInfo.VersionString}";

                var emailBody = $"\n\n---\n{appName} {appVersion}\n{deviceInfo}\n{DateTime.Now:yyyy-MM-dd HH:mm}";
                var subject = $"Contacto desde {appName}";

                // Intentar primero con Email.ComposeAsync
                try
                {
                    var message = new EmailMessage
                    {
                        Subject = subject,
                        To = new List<string> { ContactEmail },
                        Body = emailBody
                    };

                    await AppPlatform.Email.ComposeAsync(message);
                    return; // Si funciona, salir
                }
                catch (Exception emailEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Email.ComposeAsync failed: {emailEx.Message}");
                }

                // Fallback: usar intent directo de Android
                if (AppPlatform.DeviceInfo.Platform == DevicePlatform.Android)
                {
                    var emailUri = $"mailto:{ContactEmail}?subject={Uri.EscapeDataString(subject)}&body={Uri.EscapeDataString(emailBody)}";
                    await AppPlatform.Launcher.OpenAsync(emailUri);
                    return;
                }

                // Si nada funciona, mostrar error
                await AlertAsync(_localizationService.GetString("Error"), _localizationService.GetString("EmailNotAvailable"));
                return;
            }
            catch (FeatureNotSupportedException)
            {
                fallbackKey = "EmailCopiedMessage";
            }
            catch (Exception)
            {
                fallbackKey = "EmailCopiedFallbackMessage";
            }

            // Fallback final: copiar email al portapapeles
            try
            {
                await AppPlatform.Clipboard.SetTextAsync(ContactEmail);
                await AlertAsync(_localizationService.GetString("EmailCopiedTitle"), string.Format(_localizationService.GetString(fallbackKey), ContactEmail));
            }
            catch
            {
                await AlertAsync(_localizationService.GetString("Error"), _localizationService.GetString("EmailNotAvailable"));
            }
        }

        // Antes los avisos de esta pagina salian en castellano aunque la app estuviera en ingles.
        private Task<bool> AlertAsync(string title, string message) =>
            AppPlatform.Alert(this, title, message, _localizationService.GetString("OK"), null);
    }
}
