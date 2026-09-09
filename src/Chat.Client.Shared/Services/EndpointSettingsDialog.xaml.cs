using System;
using System.Configuration;
using System.Windows;
using System.Windows.Controls;
using Chat.Client.Shared.Controls;

namespace Chat.Client.Shared.Services
{
    public partial class EndpointSettingsDialog : Window
    {
        private readonly ConfigurationService _configurationService;
        private WindowResizer _windowResizer;

        public EndpointSettingsDialog()
        {
            InitializeComponent();
            _windowResizer = new WindowResizer(this);
            _configurationService = new ConfigurationService();
            
            var settings = _configurationService.GetSettings();
            HostTextBox.Text = settings.ServerHost;
            PollingPortTextBox.Text = settings.PollingPort.ToString();
            DuplexPortTextBox.Text = settings.DuplexPort.ToString();
            PollingIntervalTextBox.Text = settings.PollingIntervalMs.ToString();

            string clientType = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name ?? "";

            if (clientType.Contains("Polling"))
            {
                DuplexPortLabel.Visibility = Visibility.Collapsed;
                DuplexPortTextBox.Visibility = Visibility.Collapsed;
                this.Height = 310;
            }
            else if (clientType.Contains("Duplex"))
            {
                PollingPortLabel.Visibility = Visibility.Collapsed;
                PollingPortTextBox.Visibility = Visibility.Collapsed;
                PollingIntervalLabel.Visibility = Visibility.Collapsed;
                PollingIntervalTextBox.Visibility = Visibility.Collapsed;
                this.Height = 240;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            string host = HostTextBox.Text.Trim();
            int pollingPort;
            int duplexPort;
            int pollingInterval;

            if (string.IsNullOrWhiteSpace(host) || host.Contains("/") || host.Contains("://") || Uri.CheckHostName(host) == UriHostNameType.Unknown)
            {
                MessageBox.Show("Enter a valid server host name or IP address.", "Connection Settings", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!TryParsePort(PollingPortTextBox.Text, out pollingPort) ||
                !TryParsePort(DuplexPortTextBox.Text, out duplexPort))
            {
                MessageBox.Show("Ports must be whole numbers between 1 and 65535.", "Connection Settings", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(PollingIntervalTextBox.Text, out pollingInterval) || pollingInterval < 250 || pollingInterval > 60000)
            {
                MessageBox.Show("Polling refresh must be between 250 and 60000 milliseconds.", "Connection Settings", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                _configurationService.SaveSettings(host, pollingPort, duplexPort, pollingInterval);
                DialogResult = true;
            }
            catch (ConfigurationException ex)
            {
                MessageBox.Show("The connection settings could not be saved.\n\n" + ex.Message,
                    "Connection Settings", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static bool TryParsePort(string text, out int port)
        {
            return int.TryParse(text, out port) && port >= 1 && port <= 65535;
        }

        protected override void OnClosed(EventArgs e)
        {
            _windowResizer?.Dispose();
            base.OnClosed(e);
        }
    }
}
