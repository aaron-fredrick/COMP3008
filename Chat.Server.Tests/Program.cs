using System;
using System.Configuration;
using Chat.Server.Tests.Integration;
using Chat.Server.Tests.Unit;

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
                else if (args[i] == "--help" || args[i] == "-h") return 0;
            }

            Console.WriteLine("======================================================================");
            Console.WriteLine("                COMP3008 STRUCTURED TEST SUITE");
            Console.WriteLine("======================================================================");
            int unitFailures = 0;
            unitFailures += RunUnit("UserManager", UserManagerTests.Run);
            unitFailures += RunUnit("ChannelManager", ChannelManagerTests.Run);
            Console.WriteLine();
            int result;
            if (unitFailures != 0)
            {
                result = 1;
            }
            else
            {
                Console.WriteLine("Starting integration suite...");
                result = DeterministicIntegrationSuite.Run(pollingUrl, duplexUrl);
            }

            // WCF client channels can leave background communication threads alive after
            // the suite completes. This executable is a CI test runner, so terminate with
            // the actual suite status rather than waiting for those threads indefinitely.
            Environment.Exit(result);
            return result;
        }

        private static int RunUnit(string name, Action test)
        {
            try { test(); Console.WriteLine($"[PASS] Unit/{name}"); return 0; }
            catch (Exception ex) { Console.Error.WriteLine($"[FAIL] Unit/{name}: {ex.Message}"); return 1; }
        }
    }
}
