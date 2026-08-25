using System;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using Chat.Contracts.CallbackContracts;
using Chat.Contracts.DataContracts;
using Chat.Contracts.ServiceContracts;
using Chat.Contracts.SharedTypes;
using Chat.Server.Tests.TestInfrastructure;

namespace Chat.Server.Tests.Integration
{
    internal static class IntegrationTestSuite
    {
        public static int Run(string pollingUrl, string duplexUrl)
        {
            int passed = 0;
            int failed = 0;
            RunTest("Polling message delivery", () => PollingMessageDelivery(pollingUrl), ref passed, ref failed);
            RunTest("Polling join boundary", () => PollingJoinBoundary(pollingUrl), ref passed, ref failed);
            RunTest("Duplicate sign-in", () => DuplicateSignIn(pollingUrl), ref passed, ref failed);
            RunTest("Channel membership invariant", () => ChannelMembershipInvariant(pollingUrl), ref passed, ref failed);
            RunTest("Duplex callback delivery", () => DuplexCallbackDelivery(pollingUrl, duplexUrl), ref passed, ref failed);
            Console.WriteLine();
            Console.WriteLine($"Structured integration tests: {passed} passed, {failed} failed");
            return failed == 0 ? 0 : 1;
        }

        private static void RunTest(string name, Action test, ref int passed, ref int failed)
        {
            try
            {
                test();
                passed++;
                Console.WriteLine($"[PASS] {name}");
            }
            catch (Exception ex)
            {
                failed++;
                Console.WriteLine($"[FAIL] {name}: {ex.Message}");
            }
        }

        private static void PollingMessageDelivery(string url)
        {
            ChannelFactory<IChatService> senderFactory = CreatePollingFactory(url);
            ChannelFactory<IChatService> receiverFactory = CreatePollingFactory(url);
            IChatService sender = senderFactory.CreateChannel();
            IChatService receiver = receiverFactory.CreateChannel();
            const string senderId = "p0_struct_sender";
            const string receiverId = "p0_struct_receiver";
            const string channel = "general";
            const string content = "p0-structured-public-message";
            try
            {
                TestAssert.True(sender.SignIn(senderId), "Sender sign-in failed");
                TestAssert.True(receiver.SignIn(receiverId), "Receiver sign-in failed");
                sender.CreateChannel(channel);
                TestAssert.True(sender.JoinChannel(senderId, channel), "Sender join failed");
                TestAssert.True(receiver.JoinChannel(receiverId, channel), "Receiver join failed");
                sender.SendMessage(senderId, channel, content);
                var pending = receiver.GetPendingMessages(receiverId);
                TestAssert.Contains(pending, m => m.SenderId == senderId && m.Content == content, "Receiver did not receive the public message");
            }
            finally
            {
                SafeSignOut(sender, senderId);
                SafeSignOut(receiver, receiverId);
                Close(sender, senderFactory);
                Close(receiver, receiverFactory);
            }
        }

        private static void PollingJoinBoundary(string url)
        {
            ChannelFactory<IChatService> firstFactory = CreatePollingFactory(url);
            ChannelFactory<IChatService> secondFactory = CreatePollingFactory(url);
            IChatService first = firstFactory.CreateChannel();
            IChatService second = secondFactory.CreateChannel();
            const string firstId = "p0_boundary_sender";
            const string secondId = "p0_boundary_joiner";
            const string channel = "general";
            const string oldMessage = "p0-before-join";
            try
            {
                TestAssert.True(first.SignIn(firstId), "First sign-in failed");
                TestAssert.True(first.JoinChannel(firstId, channel), "First join failed");
                first.SendMessage(firstId, channel, oldMessage);
                TestAssert.True(second.SignIn(secondId), "Second sign-in failed");
                TestAssert.True(second.JoinChannel(secondId, channel), "Second join failed");
                var pendingAfterJoin = second.GetPendingMessages(secondId);
                TestAssert.False(pendingAfterJoin.Any(m => m.Content == oldMessage), "Pre-join message was replayed");
                first.SendMessage(firstId, channel, "p0-after-join");
                var pending = second.GetPendingMessages(secondId);
                TestAssert.Contains(pending, m => m.Content == "p0-after-join", "Post-join message was not delivered");
            }
            finally
            {
                SafeSignOut(first, firstId);
                SafeSignOut(second, secondId);
                Close(first, firstFactory);
                Close(second, secondFactory);
            }
        }

        private static void DuplicateSignIn(string url)
        {
            ChannelFactory<IChatService> factory = CreatePollingFactory(url);
            IChatService client = factory.CreateChannel();
            const string userId = "p0_duplicate_user";
            try
            {
                SafeSignOut(client, userId);
                TestAssert.True(client.SignIn(userId), "Initial sign-in failed");
                TestAssert.False(client.SignIn(userId), "Duplicate sign-in was accepted");
            }
            finally
            {
                SafeSignOut(client, userId);
                Close(client, factory);
            }
        }

        private static void ChannelMembershipInvariant(string url)
        {
            ChannelFactory<IChatService> factory = CreatePollingFactory(url);
            IChatService client = factory.CreateChannel();
            const string userId = "p0_membership_user";
            const string firstChannel = "p0_struct_channel_a";
            const string secondChannel = "p0_struct_channel_b";
            try
            {
                client.CreateChannel(firstChannel);
                client.CreateChannel(secondChannel);
                SafeSignOut(client, userId);
                TestAssert.True(client.SignIn(userId), "Sign-in failed");
                TestAssert.True(client.JoinChannel(userId, firstChannel), "First join failed");
                TestAssert.True(client.JoinChannel(userId, secondChannel), "Second join failed");
                var firstMembers = client.GetChannelMembers(firstChannel);
                var secondMembers = client.GetChannelMembers(secondChannel);
                TestAssert.False(firstMembers.Contains(userId), "User remained in previous channel");
                TestAssert.True(secondMembers.Contains(userId), "User was not moved to second channel");
            }
            finally
            {
                SafeSignOut(client, userId);
                Close(client, factory);
            }
        }

        private static void DuplexCallbackDelivery(string pollingUrl, string duplexUrl)
        {
            ChannelFactory<IChatService> pollingFactory = CreatePollingFactory(pollingUrl);
            IChatService polling = pollingFactory.CreateChannel();
            var callback = new TestCallback();
            var duplexFactory = new DuplexChannelFactory<IDuplexChatService>(new InstanceContext(callback), new NetTcpBinding(), new EndpointAddress(duplexUrl));
            IDuplexChatService duplex = duplexFactory.CreateChannel();
            const string userId = "p0_duplex_user";
            const string channel = "general";
            const string content = "p0-duplex-message";
            try
            {
                SafeSignOut(polling, userId);
                TestAssert.True(polling.SignIn(userId), "Duplex test sign-in failed");
                TestAssert.True(polling.JoinChannel(userId, channel), "Duplex test join failed");
                duplex.RegisterCallback(userId);

                // RegisterCallback is one-way. Establish registration through an observable
                // callback before sending the message rather than relying on an arbitrary sleep.
                polling.CreateChannel("p0_callback_probe");
                WaitUntil(() => callback.ChannelListChanged, 3000, "Duplex callback registration was not observed");
                polling.SendMessage(userId, channel, content);
                WaitUntil(() => callback.LastMessage != null, 3000, "Duplex message callback was not received");
                TestAssert.Equal(content, callback.LastMessage.Content, "Duplex callback contained the wrong message");
                duplex.UnregisterCallback(userId);
            }
            finally
            {
                SafeSignOut(polling, userId);
                try { ((IClientChannel)duplex).Close(); } catch { ((IClientChannel)duplex).Abort(); }
                try { duplexFactory.Close(); } catch { duplexFactory.Abort(); }
                Close(polling, pollingFactory);
            }
        }

        private static void WaitUntil(Func<bool> condition, int timeoutMs, string failureMessage)
        {
            var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            while (DateTime.UtcNow < deadline)
            {
                if (condition()) return;
                Thread.Sleep(25);
            }
            throw new InvalidOperationException(failureMessage);
        }

        private static ChannelFactory<IChatService> CreatePollingFactory(string url)
        {
            return new ChannelFactory<IChatService>(new BasicHttpBinding(), new EndpointAddress(url));
        }

        private static void SafeSignOut(IChatService proxy, string userId)
        {
            try { proxy.SignOut(userId); } catch { }
        }

        private static void Close(IChatService proxy, ChannelFactory<IChatService> factory)
        {
            try { ((IClientChannel)proxy).Close(); } catch { ((IClientChannel)proxy).Abort(); }
            try { factory.Close(); } catch { factory.Abort(); }
        }

        private sealed class TestCallback : IChatCallback
        {
            private readonly object _sync = new object();
            public bool ChannelListChanged { get; private set; }
            public Message LastMessage { get; private set; }

            public void OnChannelListChanged() { lock (_sync) ChannelListChanged = true; }
            public void OnChannelMembersChanged(string channelName) { }
            public void OnMessageReceived(Message message) { lock (_sync) LastMessage = message; }
            public void OnPrivateMessageReceived(Message message) { }
            public void OnFileShared(SharedFile file) { }
            public void OnPrivateFileShared(SharedFile file) { }
            public void OnUserDisconnected(string userId) { }
        }
    }
}
