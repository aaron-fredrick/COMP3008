using System;
using System.ServiceModel;
using System.Threading;
using Chat.Contracts.CallbackContracts;
using Chat.Contracts.DataContracts;
using Chat.Contracts.ServiceContracts;
using Chat.Contracts.SharedTypes;
using Chat.Server.Tests.TestInfrastructure;

namespace Chat.Server.Tests.Integration
{
    internal static class P1ParityIntegrationSuite
    {
        public static int Run(string pollingUrl, string duplexUrl)
        {
            int passed = 0, failed = 0;
            RunTest("Polling/duplex semantic parity", () => PollingDuplexSemanticParity(pollingUrl, duplexUrl), ref passed, ref failed);
            Console.WriteLine($"P1 parity tests: {passed} passed, {failed} failed");
            return failed == 0 ? 0 : 1;
        }

        private static void RunTest(string name, Action test, ref int passed, ref int failed)
        {
            try { test(); passed++; Console.WriteLine($"[PASS] {name}"); }
            catch (Exception ex) { failed++; Console.WriteLine($"[FAIL] {name}: {ex.Message}"); }
        }

        private static void PollingDuplexSemanticParity(string pollingUrl, string duplexUrl)
        {
            var pollingFactory = new ChannelFactory<IChatService>(new BasicHttpBinding(), new EndpointAddress(pollingUrl));
            var polling = pollingFactory.CreateChannel();
            var callback = new TestCallback();
            var duplexFactory = new DuplexChannelFactory<IDuplexChatService>(
                new InstanceContext(callback),
                new NetTcpBinding(),
                new EndpointAddress(duplexUrl));
            var duplex = duplexFactory.CreateChannel();
            const string duplexId = "p1_parity_duplex";
            const string pollingId = "p1_parity_polling";
            const string observerId = "p1_parity_observer";
            string channel = "p1-parity-" + Guid.NewGuid().ToString("N");

            try
            {
                SignOut(polling, pollingId);
                SignOut(polling, observerId);
                TestAssert.True(duplex.SignIn(duplexId), "Duplex parity sign-in failed");
                TestAssert.True(polling.SignIn(pollingId), "Polling parity sign-in failed");
                TestAssert.True(polling.SignIn(observerId), "Observer parity sign-in failed");

                polling.CreateChannel(channel);
                TestAssert.True(polling.JoinChannel(pollingId, channel), "Polling user join failed");
                TestAssert.True(polling.JoinChannel(observerId, channel), "Observer join failed");
                TestAssert.True(polling.JoinChannel(duplexId, channel), "Duplex user join failed");

                duplex.RegisterCallback(duplexId);

                // Public message: polling observes the message through pending delivery;
                // duplex observes the same server-side event through its callback.
                callback.Reset();
                polling.SendMessage(pollingId, channel, "parity-public");
                var publicMessages = duplex.GetPendingMessages(duplexId);
                TestAssert.True(publicMessages.Exists(m => m.Content == "parity-public"), "Duplex client could not retrieve the public message through the shared service contract");
                WaitUntil(() => callback.LastMessage == "parity-public", 3000, "Duplex callback did not receive the public message");

                // Private message: polling retrieves the pending PM while duplex receives
                // the same logical delivery through the callback contract.
                callback.Reset();
                TestAssert.True(polling.SendPrivateMessage(pollingId, duplexId, "parity-private"), "Private parity message was rejected");
                var privateMessages = duplex.GetPendingPrivateMessages(duplexId);
                TestAssert.True(privateMessages.Exists(m => m.Content == "parity-private"), "Private message was not available through polling retrieval");
                WaitUntil(() => callback.LastPrivateMessage == "parity-private", 3000, "Duplex callback did not receive the private message");

                // File notification: both paths expose metadata, while bytes remain an
                // explicit download operation.
                callback.Reset();
                TestAssert.True(polling.ShareFile(pollingId, channel, "parity.txt", FileType.Txt, new byte[] { 1, 2, 3 }), "Parity file share was rejected");
                var files = polling.GetChannelFiles(duplexId, channel);
                TestAssert.True(files.Exists(f => f.FileName == "parity.txt" && f.FileData == null), "Polling file listing did not expose metadata-only file state");
                WaitUntil(() => callback.LastFile != null && callback.LastFile.FileName == "parity.txt", 3000, "Duplex callback did not receive the file notification");
                TestAssert.True(callback.LastFile.FileData == null, "Duplex file notification must remain metadata-only");

                // Membership update: joining another user changes the authoritative server
                // membership and must be observable through the duplex callback path.
                callback.Reset();
                polling.LeaveChannel(observerId);
                WaitUntil(() => callback.LastMembersChannel == channel, 3000, "Duplex callback did not receive the membership update");
            }
            finally
            {
                try { duplex.UnregisterCallback(duplexId); } catch { }
                SignOut(polling, pollingId);
                SignOut(polling, observerId);
                SignOut(polling, duplexId);
                try { ((IClientChannel)duplex).Abort(); } catch { }
                try { duplexFactory.Abort(); } catch { }
                try { ((IClientChannel)polling).Abort(); } catch { }
                try { pollingFactory.Abort(); } catch { }
            }
        }

        private static void SignOut(IChatService client, string userId)
        {
            try { client.SignOut(userId); } catch { }
        }

        private static void WaitUntil(Func<bool> condition, int timeoutMs, string message)
        {
            var end = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            while (DateTime.UtcNow < end)
            {
                if (condition()) return;
                Thread.Sleep(50);
            }
            if (!condition()) throw new InvalidOperationException(message);
        }

        private sealed class TestCallback : IChatCallback
        {
            private readonly object _sync = new object();
            private string _lastMessage;
            private string _lastPrivateMessage;
            private SharedFile _lastFile;
            private string _lastMembersChannel;

            public string LastMessage { get { lock (_sync) return _lastMessage; } }
            public string LastPrivateMessage { get { lock (_sync) return _lastPrivateMessage; } }
            public SharedFile LastFile { get { lock (_sync) return _lastFile; } }
            public string LastMembersChannel { get { lock (_sync) return _lastMembersChannel; } }

            public void Reset()
            {
                lock (_sync)
                {
                    _lastMessage = null;
                    _lastPrivateMessage = null;
                    _lastFile = null;
                    _lastMembersChannel = null;
                }
            }

            public void OnChannelListChanged() { }
            public void OnChannelMembersChanged(string channelName) { lock (_sync) _lastMembersChannel = channelName; }
            public void OnMessageReceived(Message message) { lock (_sync) _lastMessage = message.Content; }
            public void OnPrivateMessageReceived(Message message) { lock (_sync) _lastPrivateMessage = message.Content; }
            public void OnFileShared(SharedFile file) { lock (_sync) _lastFile = file; }
            public void OnPrivateFileShared(SharedFile file) { }
            public void OnUserDisconnected(string userId) { }
        }
    }
}
