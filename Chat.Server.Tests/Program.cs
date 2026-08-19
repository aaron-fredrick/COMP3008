using System;
using System.ServiceModel;
using Chat.Contracts.ServiceContracts;
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

            string serverUrl = "http://localhost:9000/ChatService/Polling";

            Console.WriteLine($"Connecting to server: {serverUrl}");

            try
            {
                var binding = new BasicHttpBinding();
                var endpoint = new EndpointAddress(serverUrl);
                var channelFactory = new ChannelFactory<IChatService>(binding, endpoint);
                IChatService proxy = channelFactory.CreateChannel();

                Console.WriteLine("Connected to server successfully.\n");

                // Test 1: SignIn
                Console.WriteLine("Test 1: SignIn");
                bool signInResult = proxy.SignIn("testuser1");
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
                bool joinResult = proxy.JoinChannel("testuser1", "general");
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
                proxy.SendMessage("testuser1", "general", "Hello, world!");
                Console.WriteLine("  - Message sent successfully");

                // Test 8: GetPendingMessages
                Console.WriteLine("\nTest 8: GetPendingMessages");
                var messages = proxy.GetPendingMessages("testuser1");
                Console.WriteLine($"  - Pending message count: {messages.Count}");
                if (messages.Count > 0)
                {
                    Console.WriteLine($"  - Message content: {messages[0].Content}");
                }

                // Test 9: SignOut
                Console.WriteLine("\nTest 9: SignOut");
                proxy.SignOut("testuser1");
                Console.WriteLine("  - SignOut completed");

                // Cleanup
                Console.WriteLine("\nClosing connection...");
                ((IClientChannel)proxy).Close();
                channelFactory.Close();

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
    }
}
