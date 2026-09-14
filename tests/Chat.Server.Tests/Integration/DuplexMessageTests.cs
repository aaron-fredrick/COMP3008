using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Chat.Contracts.CallbackContracts;
using Chat.Contracts.DataContracts;
using Chat.Contracts.ServiceContracts;
using Chat.Contracts.SharedTypes;
using Chat.Server.Tests.TestInfrastructure;

namespace Chat.Server.Tests.Integration
{
    [TestClass]
    public class DuplexMessageTests
    {
        [TestMethod]
        [TestCategory("Integration")]
        public void RegisteredCallback_ReceivesPublicMessage()
        {
            var callback = new TestCallback();
            var context = new InstanceContext(callback);
            var duplexFactory = new DuplexChannelFactory<IDuplexChatService>(context, TestServerFixture.CreateDuplexBinding(), new EndpointAddress(TestServerFixture.DuplexUrl));
            IDuplexChatService duplex = duplexFactory.CreateChannel();
            ChannelFactory<IChatService> pollingFactory = new ChannelFactory<IChatService>(new BasicHttpBinding(), new EndpointAddress(TestServerFixture.PollingUrl));
            IChatService polling = pollingFactory.CreateChannel();
            const string userId = "it-duplex-user";
            const string channel = "general";

            try
            {
                Assert.IsTrue(polling.SignIn(userId));
                Assert.IsTrue(polling.JoinChannel(userId, channel));
                duplex.RegisterCallback(userId);

                // Registration is one-way WCF, so make the test deterministic by waiting
                // for the callback registration to become observable through the callback
                // channel rather than assuming a fixed sleep is sufficient.
                polling.SendMessage(userId, channel, "callback-readiness");
                Assert.IsTrue(WaitFor(() => callback.LastMessage == "callback-readiness", 3000));

                callback.Reset();
                polling.SendMessage(userId, channel, "duplex-integration-message");
                Assert.IsTrue(WaitFor(() => callback.LastMessage == "duplex-integration-message", 3000));
            }
            finally
            {
                try { duplex.UnregisterCallback(userId); } catch { }
                try { polling.SignOut(userId); } catch { }
                Close(duplex, duplexFactory);
                Close(polling, pollingFactory);
            }
        }

        private static bool WaitFor(Func<bool> condition, int timeoutMs)
        {
            var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            while (DateTime.UtcNow < deadline)
            {
                if (condition()) return true;
                Thread.Sleep(25);
            }
            return condition();
        }

        private static void Close(IDuplexChatService proxy, ChannelFactory<IDuplexChatService> factory)
        {
            try { ((IClientChannel)proxy).Close(); } catch { ((IClientChannel)proxy).Abort(); }
            try { factory.Close(); } catch { factory.Abort(); }
        }

        private static void Close(IChatService proxy, ChannelFactory<IChatService> factory)
        {
            try { ((IClientChannel)proxy).Close(); } catch { ((IClientChannel)proxy).Abort(); }
            try { factory.Close(); } catch { factory.Abort(); }
        }

        private sealed class TestCallback : IChatCallback
        {
            private readonly object _sync = new object();
            private string _lastMessage;

            public string LastMessage { get { lock (_sync) return _lastMessage; } }
            public void Reset() { lock (_sync) _lastMessage = null; }
            public void OnMessageReceived(Message message) { lock (_sync) _lastMessage = message.Content; }
            public void OnPrivateMessageReceived(Message message) { }
            public void OnChannelListChanged(List<Channel> channels) { }
            public void OnChannelMembersChanged(string channelName, List<string> members) { }
            public void OnFileShared(SharedFile file) { }
            public void OnPrivateFileShared(SharedFile file) { }
            public void OnUserDisconnected(string userId) { }
        }
    }
}
