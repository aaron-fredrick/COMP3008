using System;
using System.ServiceModel;
using Chat.Contracts.ServiceContracts;
using Chat.Server.Services;

namespace Chat.Server.Hosting
{
    public class ChatServiceHost
    {
        private ServiceHost _host;

        public void Start()
        {
            try
            {
                _host = new ServiceHost(typeof(ChatService));

                var pollingBinding = new System.ServiceModel.BasicHttpBinding();
                var pollingEndpoint = _host.AddServiceEndpoint(
                    typeof(IChatService),
                    pollingBinding,
                    "http://localhost:8080/ChatService/Polling");

                var duplexBinding = new System.ServiceModel.NetTcpBinding();
                var duplexEndpoint = _host.AddServiceEndpoint(
                    typeof(IDuplexChatService),
                    duplexBinding,
                    "net.tcp://localhost:8081/ChatService/Duplex");

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
