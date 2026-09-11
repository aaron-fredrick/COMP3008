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
        private readonly bool _enablePolling;
        private readonly int _maxMessages;

        public string PollingEndpoint { get; private set; }
        public string DuplexEndpoint { get; private set; }
        public bool IsRunning => _serviceHost != null && _serviceHost.State == CommunicationState.Opened;

        public ChatServiceHost(string host = null, int? pollingPort = null, int? duplexPort = null,
                               bool enablePolling = true, int? maxMessages = null)
        {
            _host = host ?? ConfigurationManager.AppSettings["Host"] ?? "localhost";
            _pollingPort = pollingPort ?? int.Parse(ConfigurationManager.AppSettings["PollingPort"] ?? "9000");
            _duplexPort = duplexPort ?? int.Parse(ConfigurationManager.AppSettings["DuplexPort"] ?? "8081");
            _enablePolling = enablePolling;
            _maxMessages = maxMessages
                ?? int.Parse(ConfigurationManager.AppSettings["MaxChannelMessages"] ?? "50");
        }

        public void Start()
        {
            try
            {
                var service = new ChatService(_maxMessages);
                _serviceHost = new ServiceHost(service);

                if (_enablePolling)
                {
                    var pollingBinding = new System.ServiceModel.BasicHttpBinding();
                    pollingBinding.MaxBufferSize = 2147483647;
                    pollingBinding.MaxReceivedMessageSize = 2147483647;
                    pollingBinding.MaxBufferPoolSize = 2147483647;
                    pollingBinding.ReaderQuotas.MaxDepth = 2147483647;
                    pollingBinding.ReaderQuotas.MaxStringContentLength = 2147483647;
                    pollingBinding.ReaderQuotas.MaxArrayLength = 2147483647;
                    pollingBinding.ReaderQuotas.MaxBytesPerRead = 2147483647;
                    pollingBinding.ReaderQuotas.MaxNameTableCharCount = 2147483647;

                    pollingBinding.HostNameComparisonMode = System.ServiceModel.HostNameComparisonMode.Exact;

                    var pollingEndpoint = _serviceHost.AddServiceEndpoint(
                        typeof(IChatService),
                        pollingBinding,
                        $"http://{_host}:{_pollingPort}/ChatService/Polling");
                    PollingEndpoint = pollingEndpoint.Address.ToString();
                }

                var duplexBinding = new System.ServiceModel.NetTcpBinding();
                duplexBinding.Security.Mode = System.ServiceModel.SecurityMode.None;
                duplexBinding.MaxBufferSize = 2147483647;
                duplexBinding.MaxReceivedMessageSize = 2147483647;
                duplexBinding.MaxBufferPoolSize = 2147483647;
                duplexBinding.ReaderQuotas.MaxDepth = 2147483647;
                duplexBinding.ReaderQuotas.MaxStringContentLength = 2147483647;
                duplexBinding.ReaderQuotas.MaxArrayLength = 2147483647;
                duplexBinding.ReaderQuotas.MaxBytesPerRead = 2147483647;
                duplexBinding.ReaderQuotas.MaxNameTableCharCount = 2147483647;

                duplexBinding.HostNameComparisonMode = System.ServiceModel.HostNameComparisonMode.Exact;

                var duplexEndpoint = _serviceHost.AddServiceEndpoint(
                    typeof(IDuplexChatService),
                    duplexBinding,
                    $"net.tcp://{_host}:{_duplexPort}/ChatService/Duplex");

                _serviceHost.Open();
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
