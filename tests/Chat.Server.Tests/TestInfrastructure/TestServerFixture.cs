using System;
using System.Configuration;
using System.Net;
using System.Net.Sockets;
using System.ServiceModel;
using Chat.Server.Hosting;

namespace Chat.Server.Tests.TestInfrastructure
{
    public static class TestServerFixture
    {
        private static readonly object _lock = new object();
        private static ChatServiceHost _inProcessHost;
        private static string _pollingUrl;
        private static string _duplexUrl;

        public static string PollingUrl
        {
            get
            {
                EnsureInitialized();
                return _pollingUrl;
            }
        }

        public static string DuplexUrl
        {
            get
            {
                EnsureInitialized();
                return _duplexUrl;
            }
        }

        public static void EnsureInitialized()
        {
            if (_pollingUrl != null && _duplexUrl != null) return;

            lock (_lock)
            {
                if (_pollingUrl != null && _duplexUrl != null) return;

                string configPolling = ConfigurationManager.AppSettings["PollingUrl"] ?? "http://localhost:9000/ChatService/Polling";
                string configDuplex = ConfigurationManager.AppSettings["DuplexUrl"] ?? "net.tcp://localhost:8081/ChatService/Duplex";

                // Check if external server is already listening
                if (IsPortListening(9000) && IsPortListening(8081))
                {
                    _pollingUrl = configPolling;
                    _duplexUrl = configDuplex;
                    return;
                }

                // Otherwise, spin up an in-process test host on dedicated test ports
                int pollingPort = GetFreeTcpPort(9000);
                int duplexPort = GetFreeTcpPort(8081);

                _inProcessHost = new ChatServiceHost("localhost", pollingPort, duplexPort, true, 50);
                _inProcessHost.Start();

                _pollingUrl = _inProcessHost.PollingEndpoint ?? $"http://localhost:{pollingPort}/ChatService/Polling";
                _duplexUrl = _inProcessHost.DuplexEndpoint ?? $"net.tcp://localhost:{duplexPort}/ChatService/Duplex";

                AppDomain.CurrentDomain.ProcessExit += (s, e) => Shutdown();
            }
        }

        public static void Shutdown()
        {
            lock (_lock)
            {
                if (_inProcessHost != null)
                {
                    try { _inProcessHost.Stop(); } catch { }
                    _inProcessHost = null;
                }
            }
        }

        /// <summary>
        /// Returns a <see cref="NetTcpBinding"/> that matches the security and quota settings used
        /// by the in-process test server (SecurityMode.None). Use this instead of <c>new NetTcpBinding()</c>
        /// in all duplex test factories; the default binding uses Transport security and will not
        /// connect to the test server.
        /// </summary>
        public static NetTcpBinding CreateDuplexBinding()
        {
            var binding = new NetTcpBinding();
            binding.Security.Mode = SecurityMode.None;
            binding.MaxBufferSize = 2147483647;
            binding.MaxReceivedMessageSize = 2147483647;
            binding.MaxBufferPoolSize = 2147483647;
            binding.ReaderQuotas.MaxDepth = 2147483647;
            binding.ReaderQuotas.MaxStringContentLength = 2147483647;
            binding.ReaderQuotas.MaxArrayLength = 2147483647;
            binding.ReaderQuotas.MaxBytesPerRead = 2147483647;
            binding.ReaderQuotas.MaxNameTableCharCount = 2147483647;
            return binding;
        }

        private static bool IsPortListening(int port)
        {
            try
            {
                using (var client = new TcpClient())
                {
                    var result = client.BeginConnect("localhost", port, null, null);
                    bool success = result.AsyncWaitHandle.WaitOne(300);
                    if (success && client.Connected)
                    {
                        client.EndConnect(result);
                        return true;
                    }
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }

        private static int GetFreeTcpPort(int preferredPort)
        {
            if (!IsPortListening(preferredPort))
            {
                try
                {
                    var listener = new TcpListener(IPAddress.Loopback, preferredPort);
                    listener.Start();
                    listener.Stop();
                    return preferredPort;
                }
                catch { }
            }

            var fallbackListener = new TcpListener(IPAddress.Loopback, 0);
            fallbackListener.Start();
            int port = ((IPEndPoint)fallbackListener.LocalEndpoint).Port;
            fallbackListener.Stop();
            return port;
        }
    }
}
