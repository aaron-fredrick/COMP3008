using System;
using System.Collections.Generic;
using System.Linq;
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
    public class PollingDuplexParityIntegrationSuite
    {
        public static int Run(string pollingUrl, string duplexUrl)
        {
            int passed = 0, failed = 0;
            RunTest("Public message delivery parity", () => PublicMessageParity(pollingUrl, duplexUrl), ref passed, ref failed);
            RunTest("Private message delivery parity", () => PrivateMessageParity(pollingUrl, duplexUrl), ref passed, ref failed);
            RunTest("Channel file notification parity", () => ChannelFileParity(pollingUrl, duplexUrl), ref passed, ref failed);
            RunTest("Channel membership update parity", () => ChannelMembershipParity(pollingUrl, duplexUrl), ref passed, ref failed);
            Console.WriteLine($"Polling/Duplex parity tests: {passed} passed, {failed} failed");
            return failed == 0 ? 0 : 1;
        }

        private static void RunTest(string name, Action test, ref int passed, ref int failed)
        {
            try { test(); passed++; Console.WriteLine($"[PASS] {name}"); }
            catch (Exception ex) { failed++; Console.WriteLine($"[FAIL] {name}: {ex.Message}"); }
        }

        [TestMethod]
        [TestCategory("Integration")]
        public void Test_PublicMessageParity() => PublicMessageParity(TestServerFixture.PollingUrl, TestServerFixture.DuplexUrl);

        [TestMethod]
        [TestCategory("Integration")]
        public void Test_PrivateMessageParity() => PrivateMessageParity(TestServerFixture.PollingUrl, TestServerFixture.DuplexUrl);

        [TestMethod]
        [TestCategory("Integration")]
        public void Test_ChannelFileParity() => ChannelFileParity(TestServerFixture.PollingUrl, TestServerFixture.DuplexUrl);

        [TestMethod]
        [TestCategory("Integration")]
        public void Test_ChannelMembershipParity() => ChannelMembershipParity(TestServerFixture.PollingUrl, TestServerFixture.DuplexUrl);

        private static void PublicMessageParity(string pollingUrl, string duplexUrl)
        {
            const string pollingSender = "parity_poll_public_sender", pollingReceiver = "parity_poll_public_receiver";
            const string pollingChannel = "parity-poll-public";
            const string duplexSender = "parity_duplex_public_sender", duplexReceiver = "parity_duplex_public_receiver";
            const string duplexChannel = "parity-duplex-public", probe = "parity-duplex-public-probe";
            const string content = "parity-public-message";
            var pf1 = PollingFactory(pollingUrl); var pf2 = PollingFactory(pollingUrl);
            var df1 = DuplexFactory(duplexUrl, new TestCallback()); var callback = new TestCallback(); var df2 = DuplexFactory(duplexUrl, callback);
            var ps = pf1.CreateChannel(); var pr = pf2.CreateChannel(); var ds = df1.CreateChannel(); var dr = df2.CreateChannel();
            try
            {
                SignInClean(ps, pollingSender); SignInClean(pr, pollingReceiver); CreateChannel(ps, pollingChannel); Join(ps, pollingSender, pollingChannel); Join(pr, pollingReceiver, pollingChannel);
                ps.SendMessage(pollingSender, pollingChannel, content);
                var pollingMatch = pr.GetPendingMessages(pollingReceiver).FirstOrDefault(m => m.SenderId == pollingSender && m.Content == content);
                TestAssert.True(pollingMatch != null, "Polling public message was not delivered");
                SignInClean(ds, duplexSender); SignInClean(dr, duplexReceiver); CreateChannel(ds, duplexChannel); Join(ds, duplexSender, duplexChannel); Join(dr, duplexReceiver, duplexChannel);
                dr.RegisterCallback(duplexReceiver); TriggerCallbackRegistration(ds, probe, callback);
                ds.SendMessage(duplexSender, duplexChannel, content);
                WaitUntil(() => callback.LastMessage != null, 3000, "Duplex public message was not delivered");
                TestAssert.Equal(content, callback.LastMessage.Content, "Polling and duplex public message content differed");
                TestAssert.Equal(duplexSender, callback.LastMessage.SenderId, "Duplex public message sender differed");
                TestAssert.Equal(pollingMatch.Content, callback.LastMessage.Content, "Polling and duplex public delivery semantics differed");
            }
            finally { SignOut(ps, pollingSender); SignOut(pr, pollingReceiver); SignOut(ds, duplexSender); SignOut(dr, duplexReceiver); Close(ps, pf1); Close(pr, pf2); Close(ds, df1); Close(dr, df2); }
        }

        private static void PrivateMessageParity(string pollingUrl, string duplexUrl)
        {
            const string content = "parity-private-message";
            const string psid = "parity_poll_pm_sender", prid = "parity_poll_pm_receiver", dsid = "parity_duplex_pm_sender", drid = "parity_duplex_pm_receiver";
            const string pc = "parity-poll-pm", dc = "parity-duplex-pm", probe = "parity-duplex-pm-probe";
            var pf1 = PollingFactory(pollingUrl); var pf2 = PollingFactory(pollingUrl); var df1 = DuplexFactory(duplexUrl, new TestCallback()); var callback = new TestCallback(); var df2 = DuplexFactory(duplexUrl, callback);
            var ps = pf1.CreateChannel(); var pr = pf2.CreateChannel(); var ds = df1.CreateChannel(); var dr = df2.CreateChannel();
            try
            {
                SignInClean(ps, psid); SignInClean(pr, prid); CreateChannel(ps, pc); Join(ps, psid, pc); Join(pr, prid, pc);
                TestAssert.True(ps.SendPrivateMessage(psid, prid, content), "Polling private message was rejected");
                var pollingMatch = pr.GetPendingPrivateMessages(prid).FirstOrDefault(m => m.SenderId == psid && m.Content == content);
                TestAssert.True(pollingMatch != null, "Polling private message was not delivered");
                SignInClean(ds, dsid); SignInClean(dr, drid); CreateChannel(ds, dc); Join(ds, dsid, dc); Join(dr, drid, dc);
                dr.RegisterCallback(drid); TriggerCallbackRegistration(ds, probe, callback);
                TestAssert.True(ds.SendPrivateMessage(dsid, drid, content), "Duplex private message was rejected");
                WaitUntil(() => callback.LastPrivateMessage != null, 3000, "Duplex private message was not delivered");
                TestAssert.Equal(content, callback.LastPrivateMessage.Content, "Polling and duplex private message content differed");
                TestAssert.Equal(dsid, callback.LastPrivateMessage.SenderId, "Duplex private message sender differed");
                TestAssert.Equal(pollingMatch.Content, callback.LastPrivateMessage.Content, "Polling and duplex private delivery semantics differed");
            }
            finally { SignOut(ps, psid); SignOut(pr, prid); SignOut(ds, dsid); SignOut(dr, drid); Close(ps, pf1); Close(pr, pf2); Close(ds, df1); Close(dr, df2); }
        }

        private static void ChannelFileParity(string pollingUrl, string duplexUrl)
        {
            const string fileName = "parity.txt", pollUser = "parity_poll_file", duplexUser = "parity_duplex_file";
            const string pollChannel = "parity-poll-file", duplexChannel = "parity-duplex-file", probe = "parity-duplex-file-probe";
            var bytes = new byte[] { 31, 32, 33 }; var pf = PollingFactory(pollingUrl); var callback = new TestCallback(); var df = DuplexFactory(duplexUrl, callback); var p = pf.CreateChannel(); var d = df.CreateChannel();
            try
            {
                SignInClean(p, pollUser); CreateChannel(p, pollChannel); Join(p, pollUser, pollChannel); TestAssert.True(p.ShareFile(pollUser, pollChannel, fileName, FileType.Txt, bytes), "Polling file share was rejected");
                var metadata = p.GetChannelFiles(pollUser, pollChannel).FirstOrDefault(f => f.FileName == fileName);
                TestAssert.True(metadata != null, "Polling file metadata was not visible"); TestAssert.True(metadata.FileData == null, "Polling channel file listing transferred file bytes");
                var downloaded = p.GetFile(pollUser, metadata.FileId); TestAssert.True(downloaded != null && downloaded.FileData.SequenceEqual(bytes), "Polling file retrieval differed from upload");
                SignInClean(d, duplexUser); CreateChannel(d, duplexChannel); Join(d, duplexUser, duplexChannel); d.RegisterCallback(duplexUser); TriggerCallbackRegistration(d, probe, callback);
                TestAssert.True(d.ShareFile(duplexUser, duplexChannel, fileName, FileType.Txt, bytes), "Duplex file share was rejected");
                WaitUntil(() => callback.LastFile != null, 3000, "Duplex file notification was not delivered");
                TestAssert.Equal(fileName, callback.LastFile.FileName, "Polling and duplex file names differed"); TestAssert.True(callback.LastFile.FileData == null, "Duplex file notification transferred file bytes"); TestAssert.Equal(downloaded.FileName, callback.LastFile.FileName, "Polling and duplex file notification semantics differed");
            }
            finally { SignOut(p, pollUser); SignOut(d, duplexUser); Close(p, pf); Close(d, df); }
        }

        private static void ChannelMembershipParity(string pollingUrl, string duplexUrl)
        {
            const string pollUser = "parity_poll_membership", duplexUser = "parity_duplex_membership", duplexObserver = "parity_duplex_membership_observer";
            const string pollChannel = "parity-poll-membership", duplexChannel = "parity-duplex-membership", probe = "parity-duplex-membership-probe";
            var pf = PollingFactory(pollingUrl); var callback = new TestCallback(); var df = DuplexFactory(duplexUrl, callback); var p = pf.CreateChannel(); var d = df.CreateChannel();
            try
            {
                SignInClean(p, pollUser); CreateChannel(p, pollChannel); Join(p, pollUser, pollChannel);
                TestAssert.True(p.GetChannelMembers(pollChannel).Contains(pollUser), "Polling membership update was not visible");
                p.LeaveChannel(pollUser);
                TestAssert.False(p.GetChannelMembers(pollChannel).Contains(pollUser), "Polling leave did not update membership");

                SignInClean(d, duplexUser); SignInClean(p, duplexObserver); CreateChannel(d, duplexChannel);
                Join(d, duplexUser, duplexChannel); Join(p, duplexObserver, duplexChannel);
                d.RegisterCallback(duplexUser); TriggerCallbackRegistration(d, probe, callback);
                callback.LastMembersChanged = false;
                p.LeaveChannel(duplexObserver);
                WaitUntil(() => callback.LastMembersChanged, 3000, "Duplex membership leave update was not delivered to a remaining member");
                TestAssert.Equal(duplexChannel, callback.LastMembersChannel, "Duplex leave update referenced the wrong channel");
                TestAssert.True(d.GetChannelMembers(duplexChannel).Contains(duplexUser), "Remaining duplex member was incorrectly removed");
                TestAssert.False(d.GetChannelMembers(duplexChannel).Contains(duplexObserver), "Left member remained in authoritative membership state");
            }
            finally { SignOut(p, pollUser); SignOut(p, duplexObserver); SignOut(d, duplexUser); Close(p, pf); Close(d, df); }
        }

        private static ChannelFactory<IChatService> PollingFactory(string url) => new ChannelFactory<IChatService>(new BasicHttpBinding(), new EndpointAddress(url));
        private static DuplexChannelFactory<IDuplexChatService> DuplexFactory(string url, TestCallback callback) => new DuplexChannelFactory<IDuplexChatService>(new InstanceContext(callback), TestServerFixture.CreateDuplexBinding(), new EndpointAddress(url));
        private static void CreateChannel(IChatService client, string channel) { TestAssert.True(client.CreateChannel(channel), "Test channel creation failed: " + channel); }
        private static void Join(IChatService client, string user, string channel) { TestAssert.True(client.JoinChannel(user, channel), "Test join failed: " + channel); }
        private static void TriggerCallbackRegistration(IChatService client, string probeChannel, TestCallback callback) { CreateChannel(client, probeChannel); WaitUntil(() => callback.RegisteredSignal, 3000, "Duplex callback registration was not observed"); }
        private static void SignInClean(IChatService client, string user) { SignOut(client, user); TestAssert.True(client.SignIn(user), "Sign-in failed for " + user); }
        private static void SignOut(IChatService client, string user) { try { client.SignOut(user); } catch { } }
        private static void Close(object proxy, ICommunicationObject factory)
        {
            try { var channel = proxy as IClientChannel; if (channel != null) channel.Close(); } catch { try { var channel = proxy as IClientChannel; if (channel != null) channel.Abort(); } catch { } }
            try { factory.Close(); } catch { try { factory.Abort(); } catch { } }
        }
        private static void WaitUntil(Func<bool> condition, int timeoutMs, string message) { var end = DateTime.UtcNow.AddMilliseconds(timeoutMs); while (DateTime.UtcNow < end) { if (condition()) return; Thread.Sleep(25); } throw new InvalidOperationException(message); }

        private sealed class TestCallback : IChatCallback
        {
            public bool RegisteredSignal { get; private set; }
            public Message LastMessage { get; private set; }
            public Message LastPrivateMessage { get; private set; }
            public SharedFile LastFile { get; private set; }
            public bool LastMembersChanged { get; set; }
            public string LastMembersChannel { get; private set; }
            public void OnChannelListChanged(List<Channel> channels) { RegisteredSignal = true; }
            public void OnChannelMembersChanged(string channelName, List<string> members) { LastMembersChanged = true; LastMembersChannel = channelName; }
            public void OnMessageReceived(Message message) { LastMessage = message; }
            public void OnPrivateMessageReceived(Message message) { LastPrivateMessage = message; }
            public void OnFileShared(SharedFile file) { LastFile = file; }
            public void OnPrivateFileShared(SharedFile file) { }
            public void OnUserDisconnected(string userId) { }
        }
    }
}
