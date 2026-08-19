using System;
using System.IO;
using Chat.Server.Hosting;

namespace Chat.Server
{
    class Program
    {
        private static readonly string LogFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ChatServer.log");
        private static ChatServiceHost _serviceHost;
        private static readonly object _logLock = new object();

        static void Main(string[] args)
        {
            SetupLogging();
            LogInfo("Chat Server - COMP3008");
            LogInfo("========================");

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
                LogInfo("Server started successfully");
                LogInfo($"Polling endpoint: {_serviceHost.PollingEndpoint}");
                LogInfo($"Duplex endpoint: {_serviceHost.DuplexEndpoint}");
                LogInfo("Press Ctrl+C to stop the server...");
                Console.ReadLine();
            }
            catch (Exception ex)
            {
                LogError($"Error: {ex.Message}");
                LogError($"Stack Trace: {ex.StackTrace}");
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
            LogInfo("Shutdown signal received (Ctrl+C)");
            Console.WriteLine("\nShutting down server...");

            if (_serviceHost != null)
            {
                _serviceHost.Stop();
            }

            LogInfo("Server stopped");
            Environment.Exit(0);
        }

        static void SetupLogging()
        {
            try
            {
                var logDir = Path.GetDirectoryName(LogFilePath);
                if (!string.IsNullOrEmpty(logDir) && !Directory.Exists(logDir))
                {
                    Directory.CreateDirectory(logDir);
                }

                File.WriteAllText(LogFilePath, $"=== Chat Server Log - {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC ===\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not initialize logging: {ex.Message}");
            }
        }

        static void LogInfo(string message)
        {
            string logMessage = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC] [INFO] {message}";
            lock (_logLock)
            {
                try
                {
                    File.AppendAllText(LogFilePath, logMessage + Environment.NewLine);
                }
                catch { }
            }
            Console.WriteLine(message);
        }

        static void LogError(string message)
        {
            string logMessage = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC] [ERROR] {message}";
            lock (_logLock)
            {
                try
                {
                    File.AppendAllText(LogFilePath, logMessage + Environment.NewLine);
                }
                catch { }
            }
            Console.WriteLine(message);
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
