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

        private AppSettings LoadSettings()
        {
            var settings = new AppSettings
            {
                ServerHost = ConfigurationManager.AppSettings["Host"] ?? "localhost",
                PollingPort = int.Parse(ConfigurationManager.AppSettings["PollingPort"] ?? "8080"),
                DuplexPort = int.Parse(ConfigurationManager.AppSettings["DuplexPort"] ?? "8081"),
                PollingIntervalMs = int.Parse(ConfigurationManager.AppSettings["PollingInterval"] ?? "2000")
            };

            // Build endpoints from host and ports
            settings.PollingEndpoint = $"http://{settings.ServerHost}:{settings.PollingPort}/ChatService/Polling";
            settings.DuplexEndpoint = $"net.tcp://{settings.ServerHost}:{settings.DuplexPort}/ChatService/Duplex";

            return settings;
        }
    }
}
