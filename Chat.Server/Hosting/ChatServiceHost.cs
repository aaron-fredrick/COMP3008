using System;
using System.ServiceModel;
using System.Configuration;
using Chat.Contracts.ServiceContracts;
using Chat.Server.Services;

namespace Chat.Server.Hosting
{
    public class ChatServiceHost
    {
        private ServiceHost _host;
        private readonly string _pollingHost;
        private readonly int _pollingPort;
        private readonly string _duplexHost;
        private readonly int _duplexPort;

        public ChatServiceHost(string pollingHost = null, int? pollingPort = null, string duplexHost = null, int? duplexPort = null)
        {
            _pollingHost = pollingHost ?? ConfigurationManager.AppSettings["PollingHost"] ?? "localhost";
            _pollingPort = pollingPort ?? int.Parse(ConfigurationManager.AppSettings["PollingPort"] ?? "8080");
            _duplexHost = duplexHost ?? ConfigurationManager.AppSettings["DuplexHost"] ?? "localhost";
            _duplexPort = duplexPort ?? int.Parse(ConfigurationManager.AppSettings["DuplexPort"] ?? "8081");
        }

        public void Start()
        {
            try
            {
                _host = new ServiceHost(typeof(ChatService));

                var pollingBinding = new System.ServiceModel.BasicHttpBinding();
                var pollingEndpoint = _host.AddServiceEndpoint(
                    typeof(IChatService),
                    pollingBinding,
                    $"http://{_pollingHost}:{_pollingPort}/ChatService/Polling");

                var duplexBinding = new System.ServiceModel.NetTcpBinding();
                var duplexEndpoint = _host.AddServiceEndpoint(
                    typeof(IDuplexChatService),
                    duplexBinding,
                    $"net.tcp://{_duplexHost}:{_duplexPort}/ChatService/Duplex");

                _host.Open();
                Console.WriteLine("Chat Service started successfully.");
                Console.WriteLine($"Polling endpoint: {pollingEndpoint.Address}");
                Console.WriteLine($"Duplex endpoint: {duplexEndpoint.Address}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to start service: {ex.Message}");
                throw;
            }
        }

        public void Stop()
        {
            if (_host != null)
            {
                _host.Close();
                _host = null;
                Console.WriteLine("Chat Service stopped.");
            }
        }
    }
}
