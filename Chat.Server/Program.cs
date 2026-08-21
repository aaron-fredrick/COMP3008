using System;
using Chat.Server.Hosting;
using Chat.Server.Logging;

namespace Chat.Server
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("══════════════════════════════════════════════════════════════════════");
            Console.WriteLine("  COMP3008 CHAT SERVER");
            Console.WriteLine("══════════════════════════════════════════════════════════════════════");

            try
            {
                ServerLogger.Initialize();
                var host = new ChatServiceHost(enablePolling: true);
                host.Start();

                Console.WriteLine("  [+] SERVER ONLINE");
                Console.WriteLine($"  Polling : {host.PollingEndpoint}");
                Console.WriteLine($"  Duplex  : {host.DuplexEndpoint}");
                Console.WriteLine("══════════════════════════════════════════════════════════════════════");
                Console.WriteLine("Press Ctrl+C to exit...");

                Console.CancelKeyPress += (sender, e) =>
                {
                    e.Cancel = true;
                    host.Stop();
                };

                while (host.IsRunning)
                {
                    System.Threading.Thread.Sleep(100);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  [!] SERVER STARTUP FAILED");
                Console.WriteLine($"  Error: {ex.Message}");
                Console.WriteLine("══════════════════════════════════════════════════════════════════════");
                Console.ReadKey();
            }
        }
    }
}
