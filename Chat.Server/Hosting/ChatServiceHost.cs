using System;
using System.ServiceModel;
using System.Configuration;
using Chat.Contracts.ServiceContracts;
using Chat.Server.Services;

namespace Chat.Server.Hosting
{
    public class ChatServiceHost
    {
        private ServiceHost _serviceHost;
        private readonly string _host;
        private readonly int _pollingPort;
        private readonly int _duplexPort;

        public string PollingEndpoint { get; private set; }
        public string DuplexEndpoint { get; private set; }

        public ChatServiceHost(string host = null, int? pollingPort = null, int? duplexPort = null)
        {
            _host = host ?? ConfigurationManager.AppSettings["Host"] ?? "localhost";
            _pollingPort = pollingPort ?? int.Parse(ConfigurationManager.AppSettings["PollingPort"] ?? "8080");
            _duplexPort = duplexPort ?? int.Parse(ConfigurationManager.AppSettings["DuplexPort"] ?? "8081");
        }

        public void Start()
        {
            try
            {
                _serviceHost = new ServiceHost(typeof(ChatService));

                var pollingBinding = new System.ServiceModel.BasicHttpBinding();
                var pollingEndpoint = _serviceHost.AddServiceEndpoint(
                    typeof(IChatService),
                    pollingBinding,
                    $"http://{_host}:{_pollingPort}/ChatService/Polling");

                var duplexBinding = new System.ServiceModel.NetTcpBinding();
                var duplexEndpoint = _serviceHost.AddServiceEndpoint(
                    typeof(IDuplexChatService),
                    duplexBinding,
                    $"net.tcp://{_host}:{_duplexPort}/ChatService/Duplex");

                _serviceHost.Open();
                PollingEndpoint = pollingEndpoint.Address.ToString();
                DuplexEndpoint = duplexEndpoint.Address.ToString();
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to start service: {ex.Message}", ex);
            }
        }

        public void Stop()
        {
            if (_serviceHost != null)
            {
                try
                {
                    if (_serviceHost.State == CommunicationState.Opened)
                    {
                        _serviceHost.Close();
                    }
                    else if (_serviceHost.State == CommunicationState.Faulted)
                    {
                        _serviceHost.Abort();
                    }
                    _serviceHost = null;
                }
                catch (Exception ex)
                {
                    _serviceHost = null;
                    throw new Exception($"Error stopping service: {ex.Message}", ex);
                }
            }
        }
    }
}
