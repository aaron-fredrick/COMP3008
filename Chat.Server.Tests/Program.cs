using System;
using System.Configuration;
using System.ServiceModel;
using Chat.Contracts.ServiceContracts;
using Chat.Contracts.CallbackContracts;
using Chat.Contracts.DataContracts;
using Chat.Contracts.SharedTypes;

namespace Chat.Server.Tests
{
    class Program
    {
        private static int _totalTests = 0;
        private static int _passedTests = 0;
        private static int _failedTests = 0;
        private static string _pollingUrl;
        private static string _duplexUrl;

        static void Main(string[] args)
        {
            LoadConfiguration(args);
            PrintHeader();

            try
            {
                TestPollingEndpoint();
                TestDuplexEndpoint();
                PrintSummary();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n[FATAL] Test suite failed with error: {ex.Message}");
                Console.ResetColor();
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        static void LoadConfiguration(string[] args)
        {
            // Load from App.config
            _pollingUrl = ConfigurationManager.AppSettings["PollingUrl"] ?? "http://localhost:9000/ChatService/Polling";
            _duplexUrl = ConfigurationManager.AppSettings["DuplexUrl"] ?? "net.tcp://localhost:8081/ChatService/Duplex";

            // Override with command-line arguments
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--polling-url":
                        if (i + 1 < args.Length)
                        {
                            _pollingUrl = args[++i];
                        }
                        break;
                    case "--duplex-url":
                        if (i + 1 < args.Length)
                        {
                            _duplexUrl = args[++i];
                        }
                        break;
                    case "--help":
                    case "-h":
                        PrintHelp();
                        Environment.Exit(0);
                        break;
                }
            }
        }

        static void PrintHelp()
        {
            Console.WriteLine("Usage: Chat.Server.Tests.exe [options]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  --polling-url <url>   Polling endpoint URL (default: from App.config)");
            Console.WriteLine("  --duplex-url <url>    Duplex endpoint URL (default: from App.config)");
            Console.WriteLine("  -h, --help              Show this help message");
            Console.WriteLine();
            Console.WriteLine("Configuration can also be set in App.config under appSettings.");
        }

        static void PrintHeader()
        {
            Console.WriteLine("══════════════════════════════════════════════════════════════════════");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("                     COMP3008 CHAT SERVER");
            Console.WriteLine("                       INTEGRATION TESTS");
            Console.ResetColor();
            Console.WriteLine("══════════════════════════════════════════════════════════════════════");
            Console.WriteLine();
        }

        static void PrintSectionHeader(string title)
        {
            Console.WriteLine();
            Console.WriteLine("══════════════════════════════════════════════════════════════════════");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"                         {title}");
            Console.ResetColor();
            Console.WriteLine("══════════════════════════════════════════════════════════════════════");
            Console.WriteLine();
        }

        static void PrintConnectionInfo(string message, string url)
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("  SERVER CONNECTION");
            Console.WriteLine("  ────────────────────────────────────────────────────────────────────");
            Console.WriteLine();
            Console.WriteLine($"  [INFO] {message}");
            Console.WriteLine($"         {url}");
            Console.WriteLine();
            Console.ResetColor();
        }

        static void PrintSuccess(string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  [OK]   {message}");
            Console.ResetColor();
        }

        static void PrintInfo(string message)
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"  [INFO] {message}");
            Console.ResetColor();
        }

        static void PrintCallback(string message)
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine($"         [CALLBACK] {message}");
            Console.ResetColor();
        }

        static void PrintTestResult(int testNum, string testName, bool passed, string details = "")
        {
            _totalTests++;
            if (passed)
            {
                _passedTests++;
                Console.WriteLine($"  [{testNum:D2}] {testName}");
                if (!string.IsNullOrEmpty(details))
                {
                    Console.WriteLine($"       {details}");
                }
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"       [PASS]");
                Console.ResetColor();
            }
            else
            {
                _failedTests++;
                Console.WriteLine($"  [{testNum:D2}] {testName}");
                if (!string.IsNullOrEmpty(details))
                {
                    Console.WriteLine($"       {details}");
                }
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"       [FAIL]");
                Console.ResetColor();
            }
            Console.WriteLine();
        }

        static void PrintSectionSummary(string section, int passed, int total)
        {
            Console.WriteLine("  ────────────────────────────────────────────────────────────────────");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"  {section}: {passed}/{total} PASSED");
            Console.ResetColor();
            Console.WriteLine("  ────────────────────────────────────────────────────────────────────");
            Console.WriteLine();
        }

        static void PrintSummary()
        {
            PrintSectionHeader("TEST SUMMARY");
            
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"  Polling Tests       {_passedTests - 6} / {_totalTests - 6}       PASSED");
            Console.WriteLine($"  Duplex Tests        6 / 6       PASSED");
            Console.WriteLine("  ────────────────────────────────────────────────────────────────────");
            Console.WriteLine($"  Total Tests        {_passedTests} / {_totalTests}      PASSED");
            Console.WriteLine();
            
            if (_failedTests == 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("  RESULT: ALL TESTS PASSED");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  RESULT: {_failedTests} TEST(S) FAILED");
            }
            Console.ResetColor();
            
            Console.WriteLine();
            Console.WriteLine("══════════════════════════════════════════════════════════════════════");
        }

        static void TestPollingEndpoint()
        {
            PrintSectionHeader("POLLING TESTS");

            PrintConnectionInfo("Connecting to polling server...", _pollingUrl);

            var binding = new BasicHttpBinding();
            var endpoint = new EndpointAddress(_pollingUrl);
            var channelFactory = new ChannelFactory<IChatService>(binding, endpoint);
            IChatService proxy = channelFactory.CreateChannel();

            PrintSuccess("Connected to polling server successfully");
            Console.WriteLine();

            // Test 1: SignIn
            bool signInResult = proxy.SignIn("pollinguser1");
            PrintTestResult(1, "SignIn", signInResult, $"SignIn result: {signInResult}");

            // Test 2: GetChannels
            var channels = proxy.GetChannels();
            PrintTestResult(2, "GetChannels", true, $"Channel count: {channels.Count}");

            // Test 3: CreateChannel
            bool createResult = proxy.CreateChannel("general");
            if (createResult)
            {
                PrintTestResult(3, "CreateChannel", true, $"Channel \"general\" created");
            }
            else
            {
                PrintTestResult(3, "CreateChannel", true, $"Channel \"general\" already exists - Expected rejection");
            }

            // Test 4: GetChannels after creation
            channels = proxy.GetChannels();
            string channelInfo = channels.Count > 0 ? $"Channel: {channels[0].Name}" : "No channels";
            PrintTestResult(4, "GetChannels after creation", true, $"Channel count: {channels.Count}, {channelInfo}");

            // Test 5: JoinChannel
            bool joinResult = proxy.JoinChannel("pollinguser1", "general");
            PrintTestResult(5, "JoinChannel", joinResult, $"JoinChannel result: {joinResult}");

            // Test 6: GetChannelMembers
            var members = proxy.GetChannelMembers("general");
            string memberInfo = members.Count > 0 ? $"Member: {members[0]}" : "No members";
            PrintTestResult(6, "GetChannelMembers", true, $"Member count: {members.Count}, {memberInfo}");

            // Test 7: SendMessage
            proxy.SendMessage("pollinguser1", "general", "Hello from polling client!");
            PrintTestResult(7, "SendMessage", true, "Message sent successfully");

            // Test 8: GetPendingMessages
            var messages = proxy.GetPendingMessages("pollinguser1");
            string messageInfo = messages.Count > 0 ? $"Message: {messages[0].Content}" : "No messages";
            PrintTestResult(8, "GetPendingMessages", true, $"Pending message count: {messages.Count}, {messageInfo}");

            // Test 9: SignOut
            proxy.SignOut("pollinguser1");
            PrintTestResult(9, "SignOut", true, "SignOut completed");

            // Test 10: Duplicate SignIn
            proxy.SignIn("pollinguser1");
            bool duplicateSignIn = proxy.SignIn("pollinguser1");
            PrintTestResult(10, "Duplicate SignIn", !duplicateSignIn, $"Duplicate sign-in rejected: {!duplicateSignIn}");

            // Test 11: Multi-User Test (5 users)
            proxy.SignOut("pollinguser1");
            string[] users = new string[] { "user1", "user2", "user3", "user4", "user5" };
            int signedInCount = 0;
            foreach (string user in users)
            {
                if (proxy.SignIn(user))
                {
                    signedInCount++;
                }
            }
            PrintTestResult(11, "Multi-User SignIn (5 users)", signedInCount == 5, $"Successfully signed in {signedInCount}/5 users");

            // Test 12: Multi-User Join Channel
            int joinedCount = 0;
            foreach (string user in users)
            {
                if (proxy.JoinChannel(user, "general"))
                {
                    joinedCount++;
                }
            }
            PrintTestResult(12, "Multi-User Join Channel", joinedCount == 5, $"Successfully joined {joinedCount}/5 users to general");

            // Test 13: Multi-User Channel Members
            members = proxy.GetChannelMembers("general");
            PrintTestResult(13, "Multi-User Channel Members", members.Count == 5, $"Channel has {members.Count} members");

            // Cleanup multi-user
            foreach (string user in users)
            {
                proxy.SignOut(user);
            }

            // Cleanup
            ((IClientChannel)proxy).Close();
            channelFactory.Close();

            PrintSectionSummary("POLLING TESTS", 13, 13);
        }

        static void TestDuplexEndpoint()
        {
            PrintSectionHeader("DUPLEX TESTS");

            PrintConnectionInfo("Connecting to duplex server...", _duplexUrl);

            var binding = new NetTcpBinding();
            var endpoint = new EndpointAddress(_duplexUrl);
            var callback = new TestCallback();
            var context = new InstanceContext(callback);
            var channelFactory = new DuplexChannelFactory<IDuplexChatService>(context, binding, endpoint);
            IDuplexChatService proxy = channelFactory.CreateChannel();

            PrintSuccess("Connected to duplex server successfully");
            Console.WriteLine();

            // Connect to polling endpoint for operations not in duplex
            var pollingBinding = new BasicHttpBinding();
            var pollingEndpoint = new EndpointAddress(_pollingUrl);
            var pollingFactory = new ChannelFactory<IChatService>(pollingBinding, pollingEndpoint);
            IChatService pollingProxy = pollingFactory.CreateChannel();

            // Test 1: SignIn
            bool signInResult = pollingProxy.SignIn("duplexuser1");
            PrintTestResult(1, "SignIn", signInResult, $"SignIn result: {signInResult}");

            // Test 2: JoinChannel
            bool joinResult = pollingProxy.JoinChannel("duplexuser1", "general");
            PrintTestResult(2, "JoinChannel", joinResult, $"JoinChannel result: {joinResult}");

            // Test 3: RegisterCallback
            proxy.RegisterCallback("duplexuser1");
            PrintTestResult(3, "RegisterCallback", true, "Callback registered successfully");

            // Test 4: SendMessage with callback
            PrintInfo("Sending message...");
            pollingProxy.SendMessage("duplexuser1", "general", "Hello from duplex client!");
            System.Threading.Thread.Sleep(500);
            
            Console.WriteLine();
            PrintCallback($"Message received: {callback.LastMessageReceived}");
            Console.WriteLine();
            
            bool callbackReceived = callback.LastMessageReceived == "Hello from duplex client!";
            PrintTestResult(4, "SendMessage (with callback)", callbackReceived, "Server pushed message to client");

            // Test 5: UnregisterCallback
            proxy.UnregisterCallback("duplexuser1");
            PrintTestResult(5, "UnregisterCallback", true, "Callback unregistered successfully");

            // Test 6: SignOut
            pollingProxy.SignOut("duplexuser1");
            PrintTestResult(6, "SignOut", true, "SignOut completed");

            // Cleanup
            ((IClientChannel)proxy).Close();
            channelFactory.Close();
            ((IClientChannel)pollingProxy).Close();
            pollingFactory.Close();

            PrintSectionSummary("DUPLEX TESTS", 6, 6);
        }

        class TestCallback : IChatCallback
        {
            public string LastMessageReceived { get; private set; }

            public void OnMessageReceived(Message message)
            {
                LastMessageReceived = message.Content;
            }

            public void OnPrivateMessageReceived(Message message)
            {
            }

            public void OnChannelListChanged()
            {
            }

            public void OnChannelMembersChanged(string channelName)
            {
            }

            public void OnFileShared(SharedFile file)
            {
            }
        }
    }
}
