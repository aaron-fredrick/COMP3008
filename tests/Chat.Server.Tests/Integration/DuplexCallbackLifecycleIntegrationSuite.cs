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
    public class DuplexCallbackLifecycleIntegrationSuite
    {
        public static int Run(string pollingUrl, string duplexUrl)
        {
            int passed = 0;
            int failed = 0;
            RunTest("Stale duplex callback cannot clean a replacement session", () => StaleCallbackCannotCleanReplacementSession(pollingUrl, duplexUrl), ref passed, ref failed);
            Console.WriteLine($"Duplex callback lifecycle tests: {passed} passed, {failed} failed");
            return failed == 0 ? 0 : 1;
        }

        private static void RunTest(string name, Action test, ref int passed, ref int failed)
        {
            try { test(); passed++; Console.WriteLine($"[PASS] {name}"); }
            catch (Exception ex) { failed++; Console.WriteLine($"[FAIL] {name}: {ex.Message}"); }
        }

        [TestMethod]
        [TestCategory("Integration")]
        public void Test_StaleCallbackCannotCleanReplacementSession() => StaleCallbackCannotCleanReplacementSession(TestServerFixture.PollingUrl, TestServerFixture.DuplexUrl);

        private static void StaleCallbackCannotCleanReplacementSession(string pollingUrl, string duplexUrl)
        {
            const string userId = "duplex_callback_race_user";
            const string adminId = "duplex_callback_race_admin";
            const string channel = "duplex_callback_race_channel";
            string probeChannel = "duplex_callback_race_probe_" + Guid.NewGuid().ToString("N");
            string replacementProbeChannel = "duplex_callback_race_replacement_probe_" + Guid.NewGuid().ToString("N");

            ChannelFactory<IChatService> adminFactory = CreatePollingFactory(pollingUrl);
            IChatService admin = adminFactory.CreateChannel();
            ChannelFactory<IChatService> cleanupFactory = CreatePollingFactory(pollingUrl);
            IChatService cleanup = cleanupFactory.CreateChannel();

            var oldCallback = new BlockingThrowingCallback();
            var newCallback = new RecordingCallback();
            var oldDuplexFactory = CreateDuplexFactory(oldCallback, duplexUrl);
            var newDuplexFactory = CreateDuplexFactory(newCallback, duplexUrl);
            IDuplexChatService oldDuplex = oldDuplexFactory.CreateChannel();
            IDuplexChatService newDuplex = null;

            Thread notificationThread = null;
            try
            {
                SafeSignOut(admin, adminId);
                SafeSignOut(cleanup, userId);
                TestAssert.True(admin.SignIn(adminId), "Admin sign-in failed");
                TestAssert.True(admin.CreateChannel(channel), "Channel creation failed");
                TestAssert.True(admin.JoinChannel(adminId, channel), "Admin join failed");

                TestAssert.True(cleanup.SignIn(userId), "Initial user sign-in failed");
                TestAssert.True(cleanup.JoinChannel(userId, channel), "Initial user join failed");
                oldDuplex.RegisterCallback(userId);

                notificationThread = new Thread(() => admin.CreateChannel(probeChannel));
                notificationThread.Start();
                TestAssert.True(oldCallback.Entered.WaitOne(3000), "Old callback was not invoked");

                // Replace the session while the old callback invocation is still in flight.
                cleanup.SignOut(userId);
                TestAssert.True(cleanup.SignIn(userId), "Replacement user sign-in failed");
                TestAssert.True(cleanup.JoinChannel(userId, channel), "Replacement user join failed");
                newDuplex = newDuplexFactory.CreateChannel();
                newDuplex.RegisterCallback(userId);

                oldCallback.Release.Set();
                notificationThread.Join(3000);
                TestAssert.False(notificationThread.IsAlive, "Notification thread did not complete");

                TestAssert.True(cleanup.GetChannelMembers(channel).Contains(userId), "Replacement session was removed by stale callback cleanup");

                admin.CreateChannel(replacementProbeChannel);
                TestAssert.True(newCallback.ChannelListChanged.WaitOne(3000), "Replacement callback did not remain active");
            }
            finally
            {
                oldCallback.Release.Set();
                if (notificationThread != null && notificationThread.IsAlive) notificationThread.Join(1000);
                SafeSignOut(admin, adminId);
                SafeSignOut(cleanup, userId);
                Close(oldDuplex, oldDuplexFactory);
                Close(newDuplex, newDuplexFactory);
                Close(admin, adminFactory);
                Close(cleanup, cleanupFactory);
            }
        }

        private static DuplexChannelFactory<IDuplexChatService> CreateDuplexFactory(IChatCallback callback, string url)
        {
            return new DuplexChannelFactory<IDuplexChatService>(new InstanceContext(callback), TestServerFixture.CreateDuplexBinding(), new EndpointAddress(url));
        }

        private static ChannelFactory<IChatService> CreatePollingFactory(string url) => new ChannelFactory<IChatService>(new BasicHttpBinding(), new EndpointAddress(url));

        private static void SafeSignOut(IChatService proxy, string userId) { try { proxy.SignOut(userId); } catch { } }

        private static void Close(IDuplexChatService proxy, DuplexChannelFactory<IDuplexChatService> factory)
        {
            if (proxy != null)
            {
                try { ((IClientChannel)proxy).Close(); }
                catch { try { ((IClientChannel)proxy).Abort(); } catch { } }
            }
            try { factory.Close(); } catch { try { factory.Abort(); } catch { } }
        }

        private static void Close(IChatService proxy, ChannelFactory<IChatService> factory)
        {
            try { ((IClientChannel)proxy).Close(); } catch { try { ((IClientChannel)proxy).Abort(); } catch { } }
            try { factory.Close(); } catch { try { factory.Abort(); } catch { } }
        }

        private sealed class BlockingThrowingCallback : IChatCallback
        {
            public readonly ManualResetEvent Entered = new ManualResetEvent(false);
            public readonly ManualResetEvent Release = new ManualResetEvent(false);

            public void OnChannelListChanged()
            {
                Entered.Set();
                Release.WaitOne(5000);
                throw new CommunicationException("Simulated stale callback failure");
            }

            public void OnChannelMembersChanged(string channelName) { }
            public void OnMessageReceived(Message message) { }
            public void OnPrivateMessageReceived(Message message) { }
            public void OnFileShared(SharedFile file) { }
            public void OnPrivateFileShared(SharedFile file) { }
            public void OnUserDisconnected(string userId) { }
        }

        private sealed class RecordingCallback : IChatCallback
        {
            public readonly ManualResetEvent ChannelListChanged = new ManualResetEvent(false);

            public void OnChannelListChanged() { ChannelListChanged.Set(); }
            public void OnChannelMembersChanged(string channelName) { }
            public void OnMessageReceived(Message message) { }
            public void OnPrivateMessageReceived(Message message) { }
            public void OnFileShared(SharedFile file) { }
            public void OnPrivateFileShared(SharedFile file) { }
            public void OnUserDisconnected(string userId) { }
        }
    }
}
