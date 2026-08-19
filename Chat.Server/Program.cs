using System;
using System.Threading;
using Chat.Server.Hosting;
using Chat.Server.Logging;

namespace Chat.Server
{
    class Program
    {
        private static ChatServiceHost _serviceHost;
        private static readonly ManualResetEvent _shutdownEvent = new ManualResetEvent(false);

        static void Main(string[] args)
        {
            ServerLogger.Initialize();
            ServerLogger.PrintHeader();

            string host = null;
            int? pollingPort = null;
            int? duplexPort = null;

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--host":
                        if (i + 1 < args.Length)
                        {
                            host = args[++i];
                        }
                        break;
                    case "--polling-port":
                        if (i + 1 < args.Length && int.TryParse(args[++i], out int pp))
                        {
                            pollingPort = pp;
                        }
                        break;
                    case "--duplex-port":
                        if (i + 1 < args.Length && int.TryParse(args[++i], out int dp))
                        {
                            duplexPort = dp;
                        }
                        break;
                    case "--help":
                    case "-h":
                        PrintHelp();
                        return;
                }
            }

            _serviceHost = new ChatServiceHost(host, pollingPort, duplexPort);

            Console.CancelKeyPress += OnCancelKeyPress;

            try
            {
                _serviceHost.Start();
                ServerLogger.Success("SERVER", "STARTED", $"Polling: {_serviceHost.PollingEndpoint}");
                ServerLogger.Success("SERVER", "STARTED", $"Duplex: {_serviceHost.DuplexEndpoint}");
                Console.WriteLine();
                Console.WriteLine("Press Ctrl+C to stop the server...");
                _shutdownEvent.WaitOne();
            }
            catch (Exception ex)
            {
                ServerLogger.Error("SERVER", "STARTUP", ex.Message);
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
            finally
            {
                if (_serviceHost != null)
                {
                    _serviceHost.Stop();
                }
            }
        }

        static void OnCancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            ServerLogger.Request("SERVER", "SHUTDOWN", "Ctrl+C received");

            if (_serviceHost != null)
            {
                _serviceHost.Stop();
            }

            ServerLogger.Request("SERVER", "STOPPED", "Service host closed");
            _shutdownEvent.Set();
            Environment.Exit(0);
        }

        static void PrintHelp()
        {
            Console.WriteLine("Usage: Chat.Server.exe [options]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  --host <host>           Server host for both endpoints (default: from App.config or localhost)");
            Console.WriteLine("  --polling-port <port>   Polling endpoint port (default: from App.config or 8080)");
            Console.WriteLine("  --duplex-port <port>    Duplex endpoint port (default: from App.config or 8081)");
            Console.WriteLine("  -h, --help              Show this help message");
            Console.WriteLine();
            Console.WriteLine("Configuration can also be set in App.config under appSettings.");
        }
    }
}
