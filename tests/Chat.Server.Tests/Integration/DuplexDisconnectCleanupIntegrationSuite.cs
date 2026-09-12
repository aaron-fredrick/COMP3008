using System;
using System.ServiceModel;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Chat.Contracts.CallbackContracts;
using Chat.Contracts.DataContracts;
using Chat.Contracts.ServiceContracts;
using Chat.Server.Tests.TestInfrastructure;

namespace Chat.Server.Tests.Integration
{
    [TestClass]
    public class DuplexDisconnectCleanupIntegrationSuite
    {
        public static int Run(string pollingUrl, string duplexUrl)
        {
            int passed = 0;
            int failed = 0;
            RunTest("Abnormal duplex disconnect cleanup", () => AbnormalDisconnectCleanup(pollingUrl, duplexUrl), ref passed, ref failed);
            Console.WriteLine($"Duplex disconnect cleanup tests: {passed} passed, {failed} failed");
            return failed == 0 ? 0 : 1;
        }

        private static void RunTest(string name, Action test, ref int passed, ref int failed)
        {
            try { test(); passed++; Console.WriteLine($"[PASS] {name}"); }
            catch (Exception ex) { failed++; Console.WriteLine($"[FAIL] {name}: {ex.Message}"); }
        }

        [TestMethod]
        [TestCategory("Integration")]
        public void Test_AbnormalDisconnectCleanup() => AbnormalDisconnectCleanup(TestServerFixture.PollingUrl, TestServerFixture.DuplexUrl);

        private static void AbnormalDisconnectCleanup(string pollingUrl, string duplexUrl)
        {
            const string channel = "duplex_disconnect_cleanup";
            const string observerId = "duplex_cleanup_observer";
            const string disconnectedId = "duplex_cleanup_client";

            ChannelFactory<IChatService> observerFactory = CreatePollingFactory(pollingUrl);
            IChatService observer = observerFactory.CreateChannel();
            ChannelFactory<IChatService> setupFactory = CreatePollingFactory(pollingUrl);
            IChatService setup = setupFactory.CreateChannel();
            var callback = new TestCallback();
            var duplexFactory = new DuplexChannelFactory<IDuplexChatService>(new InstanceContext(callback), TestServerFixture.CreateDuplexBinding(), new EndpointAddress(duplexUrl));
            IDuplexChatService duplex = duplexFactory.CreateChannel();

            try
            {
                SafeSignOut(observer, observerId);
                SafeSignOut(setup, disconnectedId);
                TestAssert.True(observer.SignIn(observerId), "Observer sign-in failed");
                TestAssert.True(observer.CreateChannel(channel), "Channel creation failed");
                TestAssert.True(observer.JoinChannel(observerId, channel), "Observer join failed");
                TestAssert.True(setup.SignIn(disconnectedId), "Disconnected client sign-in failed");
                TestAssert.True(setup.JoinChannel(disconnectedId, channel), "Disconnected client join failed");
                duplex.RegisterCallback(disconnectedId);

                observer.CreateChannel("duplex_disconnect_probe");
                WaitUntil(() => callback.ChannelListChanged, 3000, "Duplex callback registration was not observed");

                ((IClientChannel)duplex).Abort();
                duplex = null;
                try { duplexFactory.Abort(); } catch { }

                observer.SendMessage(observerId, channel, "disconnect-cleanup-probe");
                WaitUntil(() => !observer.GetChannelMembers(channel).Contains(disconnectedId), 3000, "Abnormally disconnected duplex user remained in channel membership");

                TestAssert.True(setup.SignIn(disconnectedId), "Abnormally disconnected user session was not cleaned up");
                TestAssert.True(setup.JoinChannel(disconnectedId, channel), "Cleaned-up user could not rejoin the channel");
            }
            finally
            {
                SafeSignOut(observer, observerId);
                SafeSignOut(setup, disconnectedId);
                if (duplex != null) { try { ((IClientChannel)duplex).Close(); } catch { try { ((IClientChannel)duplex).Abort(); } catch { } } }
                try { duplexFactory.Close(); } catch { try { duplexFactory.Abort(); } catch { } }
                Close(observer, observerFactory);
                Close(setup, setupFactory);
            }
        }

        private static void WaitUntil(Func<bool> condition, int timeoutMs, string failureMessage)
        {
            var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            while (DateTime.UtcNow < deadline) { if (condition()) return; Thread.Sleep(25); }
            throw new InvalidOperationException(failureMessage);
        }

        private static ChannelFactory<IChatService> CreatePollingFactory(string url) => new ChannelFactory<IChatService>(new BasicHttpBinding(), new EndpointAddress(url));

        private static void SafeSignOut(IChatService proxy, string userId) { try { proxy.SignOut(userId); } catch { } }

        private static void Close(IChatService proxy, ChannelFactory<IChatService> factory)
        {
            try { ((IClientChannel)proxy).Close(); } catch { try { ((IClientChannel)proxy).Abort(); } catch { } }
            try { factory.Close(); } catch { try { factory.Abort(); } catch { } }
        }

        private sealed class TestCallback : IChatCallback
        {
            public bool ChannelListChanged { get; private set; }
            public void OnChannelListChanged() { ChannelListChanged = true; }
            public void OnChannelMembersChanged(string channelName) { }
            public void OnMessageReceived(Message message) { }
            public void OnPrivateMessageReceived(Message message) { }
            public void OnFileShared(SharedFile file) { }
            public void OnPrivateFileShared(SharedFile file) { }
            public void OnUserDisconnected(string userId) { }
        }
    }
}
