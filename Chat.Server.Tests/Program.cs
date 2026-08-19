using System;
using System.ServiceModel;
using Chat.Contracts.ServiceContracts;
using Chat.Contracts.CallbackContracts;
using Chat.Contracts.DataContracts;
using Chat.Contracts.SharedTypes;

namespace Chat.Server.Tests
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Chat Server Integration Test");
            Console.WriteLine("============================\n");

            try
            {
                TestPollingEndpoint();
                TestDuplexEndpoint();
                Console.WriteLine("\n=== All tests completed successfully ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nTest failed with error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        static void TestPollingEndpoint()
        {
            Console.WriteLine("=== POLLING ENDPOINT TESTS ===\n");

            string serverUrl = "http://localhost:9000/ChatService/Polling";
            Console.WriteLine($"Connecting to polling server: {serverUrl}");

            var binding = new BasicHttpBinding();
            var endpoint = new EndpointAddress(serverUrl);
            var channelFactory = new ChannelFactory<IChatService>(binding, endpoint);
            IChatService proxy = channelFactory.CreateChannel();

            Console.WriteLine("Connected to polling server successfully.\n");

            // Test 1: SignIn
            Console.WriteLine("Test 1: SignIn");
            bool signInResult = proxy.SignIn("pollinguser1");
            Console.WriteLine($"  - SignIn result: {signInResult}");

            // Test 2: GetChannels (should be empty initially)
            Console.WriteLine("\nTest 2: GetChannels");
            var channels = proxy.GetChannels();
            Console.WriteLine($"  - Channel count: {channels.Count}");

            // Test 3: CreateChannel
            Console.WriteLine("\nTest 3: CreateChannel");
            bool createResult = proxy.CreateChannel("general");
            Console.WriteLine($"  - CreateChannel result: {createResult}");

            // Test 4: GetChannels (should have 1 channel)
            Console.WriteLine("\nTest 4: GetChannels after creation");
            channels = proxy.GetChannels();
            Console.WriteLine($"  - Channel count: {channels.Count}");
            if (channels.Count > 0)
            {
                Console.WriteLine($"  - Channel name: {channels[0].Name}");
            }

            // Test 5: JoinChannel
            Console.WriteLine("\nTest 5: JoinChannel");
            bool joinResult = proxy.JoinChannel("pollinguser1", "general");
            Console.WriteLine($"  - JoinChannel result: {joinResult}");

            // Test 6: GetChannelMembers
            Console.WriteLine("\nTest 6: GetChannelMembers");
            var members = proxy.GetChannelMembers("general");
            Console.WriteLine($"  - Member count: {members.Count}");
            if (members.Count > 0)
            {
                Console.WriteLine($"  - Member: {members[0]}");
            }

            // Test 7: SendMessage
            Console.WriteLine("\nTest 7: SendMessage");
            proxy.SendMessage("pollinguser1", "general", "Hello from polling client!");
            Console.WriteLine("  - Message sent successfully");

            // Test 8: GetPendingMessages
            Console.WriteLine("\nTest 8: GetPendingMessages");
            var messages = proxy.GetPendingMessages("pollinguser1");
            Console.WriteLine($"  - Pending message count: {messages.Count}");
            if (messages.Count > 0)
            {
                Console.WriteLine($"  - Message content: {messages[0].Content}");
            }

            // Test 9: SignOut
            Console.WriteLine("\nTest 9: SignOut");
            proxy.SignOut("pollinguser1");
            Console.WriteLine("  - SignOut completed");

            // Cleanup
            Console.WriteLine("\nClosing polling connection...");
            ((IClientChannel)proxy).Close();
            channelFactory.Close();

            Console.WriteLine("\n=== POLLING TESTS PASSED ===\n");
        }

        static void TestDuplexEndpoint()
        {
            Console.WriteLine("=== DUPLEX ENDPOINT TESTS ===\n");

            string serverUrl = "net.tcp://localhost:8081/ChatService/Duplex";
            Console.WriteLine($"Connecting to duplex server: {serverUrl}");

            var binding = new NetTcpBinding();
            var endpoint = new EndpointAddress(serverUrl);
            var callback = new TestCallback();
            var context = new InstanceContext(callback);
            var channelFactory = new DuplexChannelFactory<IDuplexChatService>(context, binding, endpoint);
            IDuplexChatService proxy = channelFactory.CreateChannel();

            Console.WriteLine("Connected to duplex server successfully.\n");

            // Test 1: RegisterCallback
            Console.WriteLine("Test 1: RegisterCallback");
            proxy.RegisterCallback("duplexuser1");
            Console.WriteLine("  - Callback registered successfully");

            // Test 2: SignIn (via polling endpoint since duplex doesn't have it)
            Console.WriteLine("\nTest 2: SignIn (via polling endpoint)");
            var pollingBinding = new BasicHttpBinding();
            var pollingEndpoint = new EndpointAddress("http://localhost:9000/ChatService/Polling");
            var pollingFactory = new ChannelFactory<IChatService>(pollingBinding, pollingEndpoint);
            IChatService pollingProxy = pollingFactory.CreateChannel();
            bool signInResult = pollingProxy.SignIn("duplexuser1");
            Console.WriteLine($"  - SignIn result: {signInResult}");

            // Test 3: JoinChannel
            Console.WriteLine("\nTest 3: JoinChannel");
            bool joinResult = pollingProxy.JoinChannel("duplexuser1", "general");
            Console.WriteLine($"  - JoinChannel result: {joinResult}");

            // Test 4: SendMessage (should trigger callback)
            Console.WriteLine("\nTest 4: SendMessage (should trigger callback)");
            Console.WriteLine("  - Sending message...");
            pollingProxy.SendMessage("duplexuser1", "general", "Hello from duplex client!");
            System.Threading.Thread.Sleep(500); // Wait for callback
            Console.WriteLine($"  - Callback received: {callback.LastMessageReceived}");

            // Test 5: UnregisterCallback
            Console.WriteLine("\nTest 5: UnregisterCallback");
            proxy.UnregisterCallback("duplexuser1");
            Console.WriteLine("  - Callback unregistered successfully");

            // Test 6: SignOut
            Console.WriteLine("\nTest 6: SignOut");
            pollingProxy.SignOut("duplexuser1");
            Console.WriteLine("  - SignOut completed");

            // Cleanup
            Console.WriteLine("\nClosing duplex connection...");
            ((IClientChannel)proxy).Close();
            channelFactory.Close();
            ((IClientChannel)pollingProxy).Close();
            pollingFactory.Close();

            Console.WriteLine("\n=== DUPLEX TESTS PASSED ===\n");
        }

        class TestCallback : IChatCallback
        {
            public string LastMessageReceived { get; private set; }

            public void OnMessageReceived(Message message)
            {
                LastMessageReceived = message.Content;
                Console.WriteLine($"  [CALLBACK] Message received: {message.Content}");
            }

            public void OnPrivateMessageReceived(Message message)
            {
                Console.WriteLine($"  [CALLBACK] Private message received: {message.Content}");
            }

            public void OnChannelListChanged()
            {
                Console.WriteLine("  [CALLBACK] Channel list changed");
            }

            public void OnChannelMembersChanged(string channelName)
            {
                Console.WriteLine($"  [CALLBACK] Channel members changed: {channelName}");
            }

            public void OnFileShared(SharedFile file)
            {
                Console.WriteLine($"  [CALLBACK] File shared: {file.FileName}");
            }
        }
    }
}
