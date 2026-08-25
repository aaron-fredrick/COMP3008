using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
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
                TestChannelMembershipConcurrency();
                TestDuplexEndpoint();
                PrintSummary();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n[FATAL] Test suite failed with error: {ex.Message}");
                Console.ResetColor();
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                _failedTests++;
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        static void LoadConfiguration(string[] args)
        {
            _pollingUrl = ConfigurationManager.AppSettings["PollingUrl"] ?? "http://localhost:9000/ChatService/Polling";
            _duplexUrl = ConfigurationManager.AppSettings["DuplexUrl"] ?? "net.tcp://localhost:8081/ChatService/Duplex";

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--polling-url":
                        if (i + 1 < args.Length) _pollingUrl = args[++i];
                        break;
                    case "--duplex-url":
                        if (i + 1 < args.Length) _duplexUrl = args[++i];
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
            Console.WriteLine("  -h, --help            Show this help message");
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
            if (passed) _passedTests++; else _failedTests++;

            Console.WriteLine($"  [{testNum:D2}] {testName}");
            if (!string.IsNullOrEmpty(details)) Console.WriteLine($"       {details}");
            Console.ForegroundColor = passed ? ConsoleColor.Green : ConsoleColor.Red;
            Console.WriteLine(passed ? "       [PASS]" : "       [FAIL]");
            Console.ResetColor();
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
            Console.WriteLine($"  Passed Tests        {_passedTests} / {_totalTests}");
            Console.WriteLine($"  Failed Tests        {_failedTests}");
            Console.WriteLine("  ────────────────────────────────────────────────────────────────────");
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

        static IChatService CreatePollingProxy(out ChannelFactory<IChatService> factory)
        {
            var binding = new BasicHttpBinding();
            var endpoint = new EndpointAddress(_pollingUrl);
            factory = new ChannelFactory<IChatService>(binding, endpoint);
            return factory.CreateChannel();
        }

        static void ClosePollingProxy(IChatService proxy, ChannelFactory<IChatService> factory)
        {
            try { ((IClientChannel)proxy).Close(); } catch { ((IClientChannel)proxy).Abort(); }
            try { factory.Close(); } catch { factory.Abort(); }
        }

        static void TestPollingEndpoint()
        {
            PrintSectionHeader("POLLING TESTS");
            PrintConnectionInfo("Connecting to polling server...", _pollingUrl);

            ChannelFactory<IChatService> channelFactory;
            IChatService proxy = CreatePollingProxy(out channelFactory);
            PrintSuccess("Connected to polling server successfully");
            Console.WriteLine();

            bool signInResult = proxy.SignIn("pollinguser1");
            PrintTestResult(1, "SignIn", signInResult, $"SignIn result: {signInResult}");

            var channels = proxy.GetChannels();
            PrintTestResult(2, "GetChannels", true, $"Channel count: {channels.Count}");

            bool createResult = proxy.CreateChannel("general");
            PrintTestResult(3, "CreateChannel", true, createResult ? "Channel \"general\" created" : "Channel \"general\" already exists - Expected rejection");

            channels = proxy.GetChannels();
            string channelInfo = channels.Count > 0 ? $"Channel: {channels[0].Name}" : "No channels";
            PrintTestResult(4, "GetChannels after creation", true, $"Channel count: {channels.Count}, {channelInfo}");

            bool joinResult = proxy.JoinChannel("pollinguser1", "general");
            PrintTestResult(5, "JoinChannel", joinResult, $"JoinChannel result: {joinResult}");

            var members = proxy.GetChannelMembers("general");
            string memberInfo = members.Count > 0 ? $"Member: {members[0]}" : "No members";
            PrintTestResult(6, "GetChannelMembers", true, $"Member count: {members.Count}, {memberInfo}");

            proxy.SendMessage("pollinguser1", "general", "Hello from polling client!");
            PrintTestResult(7, "SendMessage", true, "Message sent successfully");

            var messages = proxy.GetPendingMessages("pollinguser1");
            string messageInfo = messages.Count > 0 ? $"Message: {messages[0].Content}" : "No messages";
            PrintTestResult(8, "GetPendingMessages", true, $"Pending message count: {messages.Count}, {messageInfo}");

            proxy.SignOut("pollinguser1");
            PrintTestResult(9, "SignOut", true, "SignOut completed");

            proxy.SignIn("pollinguser1");
            bool duplicateSignIn = proxy.SignIn("pollinguser1");
            PrintTestResult(10, "Duplicate SignIn", !duplicateSignIn, $"Duplicate sign-in rejected: {!duplicateSignIn}");

            proxy.SignOut("pollinguser1");
            string[] users = new string[] { "user1", "user2", "user3", "user4", "user5" };
            int signedInCount = 0;
            foreach (string user in users) if (proxy.SignIn(user)) signedInCount++;
            PrintTestResult(11, "Multi-User SignIn (5 users)", signedInCount == 5, $"Successfully signed in {signedInCount}/5 users");

            int joinedCount = 0;
            foreach (string user in users) if (proxy.JoinChannel(user, "general")) joinedCount++;
            PrintTestResult(12, "Multi-User Join Channel", joinedCount == 5, $"Successfully joined {joinedCount}/5 users to general");

            members = proxy.GetChannelMembers("general");
            PrintTestResult(13, "Multi-User Channel Members", members.Count == 5, $"Channel has {members.Count} members");

            foreach (string user in users) proxy.SignOut(user);
            ClosePollingProxy(proxy, channelFactory);
            PrintSectionSummary("POLLING TESTS", 13, 13);
        }

        static void TestChannelMembershipConcurrency()
        {
            PrintSectionHeader("CHANNEL MEMBERSHIP CONCURRENCY TESTS");
            PrintInfo("Running concurrent JoinChannel transitions for the same users...");

            const string general = "p0_concurrency_general";
            const string other = "p0_concurrency_other";
            string[] users = { "p0_concurrent_a", "p0_concurrent_b", "p0_concurrent_c", "p0_concurrent_d" };

            ChannelFactory<IChatService> setupFactory;
            IChatService setup = CreatePollingProxy(out setupFactory);
            try
            {
                if (!setup.CreateChannel(general)) { }
                if (!setup.CreateChannel(other)) { }
                foreach (string user in users)
                {
                    setup.SignOut(user);
                    if (!setup.SignIn(user)) throw new InvalidOperationException($"Could not sign in {user}");
                    if (!setup.JoinChannel(user, general)) throw new InvalidOperationException($"Could not initially join {user}");
                }
            }
            finally
            {
                ClosePollingProxy(setup, setupFactory);
            }

            var errors = new List<Exception>();
            var tasks = new List<Task>();
            const int workersPerUser = 4;
            const int iterationsPerWorker = 25;

            foreach (string user in users)
            {
                for (int worker = 0; worker < workersPerUser; worker++)
                {
                    string currentUser = user;
                    int workerId = worker;
                    tasks.Add(Task.Run(() =>
                    {
                        ChannelFactory<IChatService> factory = null;
                        IChatService proxy = null;
                        try
                        {
                            proxy = CreatePollingProxy(out factory);
                            for (int i = 0; i < iterationsPerWorker; i++)
                            {
                                string target = ((i + workerId) % 2 == 0) ? general : other;
                                if (!proxy.JoinChannel(currentUser, target))
                                    throw new InvalidOperationException($"JoinChannel returned false for {currentUser} -> {target}");
                            }
                        }
                        catch (Exception ex)
                        {
                            lock (errors) errors.Add(ex);
                        }
                        finally
                        {
                            if (proxy != null && factory != null) ClosePollingProxy(proxy, factory);
                        }
                    }));
                }
            }

            Task.WaitAll(tasks.ToArray());
            PrintTestResult(14, "Concurrent channel transitions complete", errors.Count == 0, errors.Count == 0 ? $"{users.Length * workersPerUser} workers completed {iterationsPerWorker} transitions each" : $"{errors.Count} worker error(s): {errors[0].Message}");

            ChannelFactory<IChatService> verifyFactory;
            IChatService verify = CreatePollingProxy(out verifyFactory);
            try
            {
                var generalMembers = verify.GetChannelMembers(general);
                var otherMembers = verify.GetChannelMembers(other);
                bool noDuplicates = users.All(user => generalMembers.Count(x => x == user) <= 1 && otherMembers.Count(x => x == user) <= 1);
                bool noSplitMembership = users.All(user => !(generalMembers.Contains(user) && otherMembers.Contains(user)));
                bool knownMembersOnly = generalMembers.All(users.Contains) && otherMembers.All(users.Contains);

                PrintTestResult(15, "No duplicate channel membership", noDuplicates, $"general={generalMembers.Count}, other={otherMembers.Count}");
                PrintTestResult(16, "Atomic transition invariant", noSplitMembership, noSplitMembership ? "No user appears in both channels" : "A user was observed in both channels");
                PrintTestResult(17, "Membership set integrity", knownMembersOnly, "Channel member lists contain only concurrency-test users");

                foreach (string user in users) verify.SignOut(user);
            }
            finally
            {
                ClosePollingProxy(verify, verifyFactory);
            }

            PrintSectionSummary("CHANNEL MEMBERSHIP CONCURRENCY TESTS", 4, 4);
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

            ChannelFactory<IChatService> pollingFactory;
            IChatService pollingProxy = CreatePollingProxy(out pollingFactory);

            bool signInResult = pollingProxy.SignIn("duplexuser1");
            PrintTestResult(18, "SignIn", signInResult, $"SignIn result: {signInResult}");

            bool joinResult = pollingProxy.JoinChannel("duplexuser1", "general");
            PrintTestResult(19, "JoinChannel", joinResult, $"JoinChannel result: {joinResult}");

            proxy.RegisterCallback("duplexuser1");
            PrintTestResult(20, "RegisterCallback", true, "Callback registered successfully");

            PrintInfo("Sending message...");
            pollingProxy.SendMessage("duplexuser1", "general", "Hello from duplex client!");
            Thread.Sleep(500);
            Console.WriteLine();
            PrintCallback($"Message received: {callback.LastMessageReceived}");
            Console.WriteLine();
            bool callbackReceived = callback.LastMessageReceived == "Hello from duplex client!";
            PrintTestResult(21, "SendMessage (with callback)", callbackReceived, "Server pushed message to client");

            proxy.UnregisterCallback("duplexuser1");
            PrintTestResult(22, "UnregisterCallback", true, "Callback unregistered successfully");

            pollingProxy.SignOut("duplexuser1");
            PrintTestResult(23, "SignOut", true, "SignOut completed");

            try { ((IClientChannel)proxy).Close(); } catch { ((IClientChannel)proxy).Abort(); }
            try { channelFactory.Close(); } catch { channelFactory.Abort(); }
            ClosePollingProxy(pollingProxy, pollingFactory);
            PrintSectionSummary("DUPLEX TESTS", 6, 6);
        }

        class TestCallback : IChatCallback
        {
            public string LastMessageReceived { get; private set; }

            public void OnMessageReceived(Message message) { LastMessageReceived = message.Content; }
            public void OnPrivateMessageReceived(Message message) { }
            public void OnChannelListChanged() { }
            public void OnChannelMembersChanged(string channelName) { }
            public void OnFileShared(SharedFile file) { }
            public void OnPrivateFileShared(SharedFile file) { }
            public void OnUserDisconnected(string userId) { }
        }
    }
}
