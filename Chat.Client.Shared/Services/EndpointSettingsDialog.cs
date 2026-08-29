using System;
using System.Configuration;
using System.Windows;
using System.Windows.Controls;

namespace Chat.Client.Shared.Services
{
    public sealed class EndpointSettingsDialog : Window
    {
        private readonly ConfigurationService _configurationService;
        private readonly TextBox _hostTextBox;
        private readonly TextBox _pollingPortTextBox;
        private readonly TextBox _duplexPortTextBox;
        private readonly TextBox _pollingIntervalTextBox;

        public EndpointSettingsDialog()
        {
            _configurationService = new ConfigurationService();
            var settings = _configurationService.GetSettings();

            Title = "Connection Settings";
            Width = 420;
            Height = 330;
            ResizeMode = ResizeMode.NoResize;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            var panel = new StackPanel { Margin = new Thickness(20) };
            panel.Children.Add(CreateLabel("Server host"));
            _hostTextBox = CreateTextBox(settings.ServerHost);
            panel.Children.Add(_hostTextBox);

            panel.Children.Add(CreateLabel("Polling port"));
            _pollingPortTextBox = CreateTextBox(settings.PollingPort.ToString());
            panel.Children.Add(_pollingPortTextBox);

            panel.Children.Add(CreateLabel("Duplex port"));
            _duplexPortTextBox = CreateTextBox(settings.DuplexPort.ToString());
            panel.Children.Add(_duplexPortTextBox);

            panel.Children.Add(CreateLabel("Polling refresh (ms)"));
            _pollingIntervalTextBox = CreateTextBox(settings.PollingIntervalMs.ToString());
            panel.Children.Add(_pollingIntervalTextBox);

            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 18, 0, 0)
            };

            var cancelButton = new Button { Content = "Cancel", Width = 80, Margin = new Thickness(0, 0, 8, 0) };
            cancelButton.Click += (sender, args) => DialogResult = false;
            buttons.Children.Add(cancelButton);

            var saveButton = new Button { Content = "Apply", Width = 80 };
            saveButton.Click += SaveButton_Click;
            buttons.Children.Add(saveButton);

            panel.Children.Add(buttons);
            Content = panel;
        }

        private static TextBlock CreateLabel(string text)
        {
            return new TextBlock
            {
                Text = text,
                Margin = new Thickness(0, 6, 0, 3)
            };
        }

        private static TextBox CreateTextBox(string text)
        {
            return new TextBox
            {
                Text = text,
                Height = 26
            };
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            string host = _hostTextBox.Text.Trim();
            int pollingPort;
            int duplexPort;
            int pollingInterval;

            if (string.IsNullOrWhiteSpace(host) || host.Contains("/") || host.Contains("://") || Uri.CheckHostName(host) == UriHostNameType.Unknown)
            {
                MessageBox.Show("Enter a valid server host name or IP address.", "Connection Settings", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!TryParsePort(_pollingPortTextBox.Text, out pollingPort) ||
                !TryParsePort(_duplexPortTextBox.Text, out duplexPort))
            {
                MessageBox.Show("Ports must be whole numbers between 1 and 65535.", "Connection Settings", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(_pollingIntervalTextBox.Text, out pollingInterval) || pollingInterval < 250 || pollingInterval > 60000)
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
    }
}
