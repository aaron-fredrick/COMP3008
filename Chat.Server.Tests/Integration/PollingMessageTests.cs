using System;
using System.Linq;
using System.ServiceModel;
using Chat.Contracts.ServiceContracts;
using NUnit.Framework;

namespace Chat.Server.Tests.Integration
{
    [TestFixture]
    public class PollingMessageTests
    {
        private const string Url = "http://localhost:9000/ChatService/Polling";

        [Test]
        public void Message_IsDeliveredToAnotherCurrentChannelMember()
        {
            ChannelFactory<IChatService> factoryA = CreateFactory();
            ChannelFactory<IChatService> factoryB = CreateFactory();
            IChatService sender = factoryA.CreateChannel();
            IChatService receiver = factoryB.CreateChannel();
            const string senderId = "it-poll-sender";
            const string receiverId = "it-poll-receiver";
            const string channel = "general";
            const string content = "integration-public-message";

            try
            {
                Assert.That(sender.SignIn(senderId), Is.True);
                Assert.That(receiver.SignIn(receiverId), Is.True);
                sender.CreateChannel(channel);
                Assert.That(sender.JoinChannel(senderId, channel), Is.True);
                Assert.That(receiver.JoinChannel(receiverId, channel), Is.True);

                sender.SendMessage(senderId, channel, content);

                var pending = receiver.GetPendingMessages(receiverId);
                Assert.That(pending.Any(m => m.SenderId == senderId && m.Content == content), Is.True);
            }
            finally
            {
                try { sender.SignOut(senderId); } catch { }
                try { receiver.SignOut(receiverId); } catch { }
                Close(sender, factoryA);
                Close(receiver, factoryB);
            }
        }

        [Test]
        public void Sender_DoesNotReceiveItsOwnMessageAsPendingPollingMessage()
        {
            ChannelFactory<IChatService> factory = CreateFactory();
            IChatService client = factory.CreateChannel();
            const string userId = "it-poll-self";
            const string channel = "general";
            const string content = "self-message-boundary";

            try
            {
                Assert.That(client.SignIn(userId), Is.True);
                Assert.That(client.JoinChannel(userId, channel), Is.True);
                client.SendMessage(userId, channel, content);

                var pending = client.GetPendingMessages(userId);
                Assert.That(pending.Any(m => m.SenderId == userId && m.Content == content), Is.False);
            }
            finally
            {
                try { client.SignOut(userId); } catch { }
                Close(client, factory);
            }
        }

        private static ChannelFactory<IChatService> CreateFactory()
        {
            return new ChannelFactory<IChatService>(new BasicHttpBinding(), new EndpointAddress(Url));
        }

        private static void Close(IChatService proxy, ChannelFactory<IChatService> factory)
        {
            try { ((IClientChannel)proxy).Close(); } catch { ((IClientChannel)proxy).Abort(); }
            try { factory.Close(); } catch { factory.Abort(); }
        }
    }
}
