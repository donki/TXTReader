using TXTReader.Services;

namespace TXTReader.Pages
{
    public partial class LogViewerPage : ContentPage
    {
        public LogViewerPage()
        {
            InitializeComponent();
            _ = LoadLogs();
        }

        private async Task LoadLogs()
        {
            try
            {
                var logs = await MobileLogService.ReadLogsAsync();
                LogLabel.Text = logs;
            }
            catch (Exception ex)
            {
                LogLabel.Text = $"Error cargando logs: {ex.Message}";
            }
        }

        private async void OnRefreshClicked(object? sender, EventArgs e)
        {
            await LoadLogs();
        }

        private async void OnClearClicked(object? sender, EventArgs e)
        {
            try
            {
                await MobileLogService.ClearLogsAsync();
                await LoadLogs();
                await DisplayAlert("Logs", "Logs limpiados correctamente", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error limpiando logs: {ex.Message}", "OK");
            }
        }
    }
}