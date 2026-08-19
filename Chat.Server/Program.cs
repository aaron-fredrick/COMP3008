using System;
using Chat.Server.Hosting;

namespace Chat.Server
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Chat Server - COMP3008");
            Console.WriteLine("========================");

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

            var serviceHost = new ChatServiceHost(host, pollingPort, duplexPort);

            try
            {
                serviceHost.Start();
                Console.WriteLine("\nPress any key to stop the server...");
                Console.ReadKey();
                serviceHost.Stop();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nError: {ex.Message}");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
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
