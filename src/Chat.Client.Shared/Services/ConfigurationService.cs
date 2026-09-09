using System;
using System.Configuration;

namespace Chat.Client.Shared.Services
{
    public class AppSettings
    {
        public string ServerHost { get; set; }
        public int PollingPort { get; set; }
        public int DuplexPort { get; set; }
        public int PollingIntervalMs { get; set; }
        public string PollingEndpoint { get; set; }
        public string DuplexEndpoint { get; set; }
    }

    public class ConfigurationService
    {
        private readonly AppSettings _settings;

        public ConfigurationService()
        {
            _settings = LoadSettings();
        }

        public AppSettings GetSettings()
        {
            return _settings;
        }

        public void SaveSettings(string serverHost, int pollingPort, int duplexPort, int pollingIntervalMs)
        {
            var configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            SetAppSetting(configuration, "ServerUrl", $"http://{serverHost}:{pollingPort}/ChatService/Polling");
            SetAppSetting(configuration, "DuplexServerUrl", $"net.tcp://{serverHost}:{duplexPort}/ChatService/Duplex");
            SetAppSetting(configuration, "PollingInterval", pollingIntervalMs.ToString());
            configuration.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");

            _settings.ServerHost = serverHost;
            _settings.PollingPort = pollingPort;
            _settings.DuplexPort = duplexPort;
            _settings.PollingIntervalMs = pollingIntervalMs;
            _settings.PollingEndpoint = $"http://{serverHost}:{pollingPort}/ChatService/Polling";
            _settings.DuplexEndpoint = $"net.tcp://{serverHost}:{duplexPort}/ChatService/Duplex";
        }

        private static void SetAppSetting(Configuration configuration, string key, string value)
        {
            if (configuration.AppSettings.Settings[key] == null)
                configuration.AppSettings.Settings.Add(key, value);
            else
                configuration.AppSettings.Settings[key].Value = value;
        }

        private AppSettings LoadSettings()
        {
            string pollingEndpoint = ConfigurationManager.AppSettings["ServerUrl"] ?? "http://localhost:9000/ChatService/Polling";
            string duplexEndpoint = ConfigurationManager.AppSettings["DuplexServerUrl"] ?? "net.tcp://localhost:8081/ChatService/Duplex";

            Uri pollingUri = new Uri(pollingEndpoint);
            Uri duplexUri = new Uri(duplexEndpoint);
            int pollingInterval = ParsePositiveInt(ConfigurationManager.AppSettings["PollingInterval"], 2000);

            return new AppSettings
            {
                ServerHost = pollingUri.Host,
                PollingPort = pollingUri.Port,
                DuplexPort = duplexUri.Port,
                PollingIntervalMs = pollingInterval,
                PollingEndpoint = pollingUri.ToString().TrimEnd('/'),
                DuplexEndpoint = duplexUri.ToString().TrimEnd('/')
            };
        }

        private static int ParsePositiveInt(string value, int fallback)
        {
            int result;
            return int.TryParse(value, out result) && result > 0 ? result : fallback;
        }
    }
}
