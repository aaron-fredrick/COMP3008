using System;
using System.Configuration;
using Chat.Server.Tests.Integration;

namespace Chat.Server.Tests
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            string pollingUrl = ConfigurationManager.AppSettings["PollingUrl"] ?? "http://localhost:9000/ChatService/Polling";
            string duplexUrl = ConfigurationManager.AppSettings["DuplexUrl"] ?? "net.tcp://localhost:8081/ChatService/Duplex";

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--polling-url" && i + 1 < args.Length) pollingUrl = args[++i];
                else if (args[i] == "--duplex-url" && i + 1 < args.Length) duplexUrl = args[++i];
                else if (args[i] == "--help" || args[i] == "-h")
                {
                    Console.WriteLine("Usage: Chat.Server.Tests.exe [--polling-url <url>] [--duplex-url <url>]");
                    return 0;
                }
            }

            Console.WriteLine("======================================================================");
            Console.WriteLine("             COMP3008 STRUCTURED INTEGRATION TESTS");
            Console.WriteLine("======================================================================");
            Console.WriteLine($"Polling: {pollingUrl}");
            Console.WriteLine($"Duplex:  {duplexUrl}");
            Console.WriteLine();

            try
            {
                return IntegrationTestSuite.Run(pollingUrl, duplexUrl);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("[FATAL] Integration test runner failed.");
                Console.Error.WriteLine(ex);
                return 1;
            }
        }
    }
}
