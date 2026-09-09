using System;
using System.Configuration;
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
                int? maxMessagesOverride = ParseMaxMessages(args);

                ServerLogger.Initialize();
                var host = new ChatServiceHost(enablePolling: true, maxMessages: maxMessagesOverride);
                host.Start();

                int resolvedMax = maxMessagesOverride
                    ?? int.Parse(ConfigurationManager.AppSettings["MaxChannelMessages"] ?? "50");

                Console.WriteLine("  [+] SERVER ONLINE");
                Console.WriteLine($"  Polling : {host.PollingEndpoint}");
                Console.WriteLine($"  Duplex  : {host.DuplexEndpoint}");
                Console.WriteLine($"  Msg cap : {resolvedMax} per channel" +
                                  (maxMessagesOverride.HasValue ? " (CLI override)" : " (config)"));
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

        private static int? ParseMaxMessages(string[] args)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "--max-messages" && int.TryParse(args[i + 1], out int value) && value > 0)
                    return value;
            }
            return null;
        }
    }
}
