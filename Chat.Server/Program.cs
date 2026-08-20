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
                var host = new ChatServiceHost(enablePolling: false);
                host.Start();

                Console.WriteLine("  [+] SERVER ONLINE");
                Console.WriteLine($"  Duplex  : {host.DuplexEndpoint}");
                Console.WriteLine("══════════════════════════════════════════════════════════════════════");
                Console.WriteLine("Press any key to exit...");

                Console.ReadKey();
                host.Stop();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  [!] SERVER STARTUP FAILED");
                Console.WriteLine($"  Error: {ex.Message}");
                Console.WriteLine("══════════════════════════════════════════════════════════════════════");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
        }
    }
}
