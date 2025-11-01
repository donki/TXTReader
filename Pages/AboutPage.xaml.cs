using Microsoft.Maui.Essentials;
using TXTReader.Services;

namespace TXTReader.Pages
{
    public partial class AboutPage : ContentPage
    {
        private const string ContactEmail = "jsoladelarosa@gmail.com";
        private const string KofiUrl = "https://ko-fi.com/josepsola";

        private readonly LocalizationService _localizationService;

        public AboutPage()
        {
            InitializeComponent();
            _localizationService = LocalizationService.Instance;
            _localizationService.LanguageChanged += OnLanguageChanged;
            
            SetupLanguagePicker();
            UpdateTexts();
        }

        private void SetupLanguagePicker()
        {
            var languages = _localizationService.GetAvailableLanguages();
            LanguagePicker.ItemsSource = languages.Select(l => l.Name).ToList();
            
            var currentLanguage = _localizationService.GetCurrentLanguageCode();
            var currentIndex = languages.FindIndex(l => l.Code == currentLanguage);
            if (currentIndex >= 0)
            {
                LanguagePicker.SelectedIndex = currentIndex;
            }
        }

        private void UpdateTexts()
        {
            Title = _localizationService.GetString("AboutTitle");
            VersionLabel.Text = _localizationService.GetString("AppVersion");
            DescriptionLabel.Text = _localizationService.GetString("AppDescription");
            ContactTitleLabel.Text = _localizationService.GetString("ContactTitle");
            ContactInstructionLabel.Text = _localizationService.GetString("ContactInstruction");
            DonationTitleLabel.Text = _localizationService.GetString("DonationTitle");
            DonationButton.Text = _localizationService.GetString("DonationButton");
            DonationDescriptionLabel.Text = _localizationService.GetString("DonationDescription");
            LanguageTitleLabel.Text = _localizationService.GetString("LanguageTitle");
            LanguageDescriptionLabel.Text = _localizationService.GetString("LanguageDescription");
            LegalTitleLabel.Text = _localizationService.GetString("LegalTitle");
            LegalText1Label.Text = _localizationService.GetString("LegalText1");
            LegalText2Label.Text = _localizationService.GetString("LegalText2");
            WarningTextLabel.Text = _localizationService.GetString("WarningText");
            BackButton.Text = _localizationService.GetString("BackButton");
        }

        private void OnLanguageChanged(object? sender, EventArgs e)
        {
            UpdateTexts();
            SetupLanguagePicker();
        }

        private void OnLanguagePickerChanged(object? sender, EventArgs e)
        {
            if (LanguagePicker.SelectedIndex >= 0)
            {
                var languages = _localizationService.GetAvailableLanguages();
                var selectedLanguage = languages[LanguagePicker.SelectedIndex];
                _localizationService.SetLanguage(selectedLanguage.Code);
            }
        }

        private async void OnBackClicked(object? sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        private async void OnContactEmailClicked(object? sender, EventArgs e)
        {
            try
            {
                var appName = "TXT Reader";
                var appVersion = _localizationService.GetString("AppVersion");
                var deviceInfo = $"{DeviceInfo.Platform} {DeviceInfo.VersionString}";
                
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

                    await Email.ComposeAsync(message);
                    return; // Si funciona, salir
                }
                catch (Exception emailEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Email.ComposeAsync failed: {emailEx.Message}");
                }

                // Fallback: usar intent directo de Android
                if (DeviceInfo.Platform == DevicePlatform.Android)
                {
                    var emailUri = $"mailto:{ContactEmail}?subject={Uri.EscapeDataString(subject)}&body={Uri.EscapeDataString(emailBody)}";
                    await Launcher.OpenAsync(emailUri);
                    return;
                }

                // Si nada funciona, mostrar error
                await DisplayAlertAsync("Error", _localizationService.GetString("EmailNotAvailable"), "OK");
            }
            catch (FeatureNotSupportedException)
            {
                // Fallback final: copiar email al portapapeles
                try
                {
                    await Clipboard.SetTextAsync(ContactEmail);
                    await DisplayAlertAsync("Email copiado", $"Email copiado al portapapeles: {ContactEmail}", "OK");
                }
                catch
                {
                    await DisplayAlertAsync("Error", _localizationService.GetString("EmailNotAvailable"), "OK");
                }
            }
            catch (Exception ex)
            {
                // Fallback final: copiar email al portapapeles
                try
                {
                    await Clipboard.SetTextAsync(ContactEmail);
                    await DisplayAlertAsync("Email copiado", $"No se pudo abrir el cliente de correo. Email copiado al portapapeles: {ContactEmail}", "OK");
                }
                catch
                {
                    await DisplayAlertAsync("Error", $"{_localizationService.GetString("EmailError")}: {ex.Message}", "OK");
                }
            }
        }

        private async void OnKofiClicked(object? sender, EventArgs e)
        {
            try
            {
                var uri = new Uri(KofiUrl);
                var browserLaunchOptions = new BrowserLaunchOptions
                {
                    LaunchMode = BrowserLaunchMode.SystemPreferred,
                    TitleMode = BrowserTitleMode.Show,
                    PreferredToolbarColor = Color.FromArgb("#E67E22"),
                    PreferredControlColor = Color.FromArgb("#FFFFFF")
                };

                await Browser.OpenAsync(uri, browserLaunchOptions);
            }
            catch (FeatureNotSupportedException)
            {
                // Fallback: copy URL to clipboard if browser is not available
                try
                {
                    await Clipboard.SetTextAsync(KofiUrl);
                    await DisplayAlertAsync(_localizationService.GetString("BrowserNotAvailableTitle"), 
                        $"{_localizationService.GetString("BrowserNotAvailableMessage")}:\n{KofiUrl}", 
                        "OK");
                }
                catch
                {
                    await DisplayAlertAsync("Error", 
                        $"{_localizationService.GetString("BrowserError")}: {KofiUrl}", 
                        "OK");
                }
            }
            catch (Exception ex)
            {
                // Fallback: copy URL to clipboard on any other error
                try
                {
                    await Clipboard.SetTextAsync(KofiUrl);
                    await DisplayAlertAsync(_localizationService.GetString("LinkErrorTitle"), 
                        $"{_localizationService.GetString("LinkErrorMessage")} ({ex.Message}), {_localizationService.GetString("ClipboardMessage")}.", 
                        "OK");
                }
                catch
                {
                    await DisplayAlertAsync("Error", 
                        $"{_localizationService.GetString("FinalErrorMessage")}: {KofiUrl}", 
                        "OK");
                }
            }
        }
    }
}