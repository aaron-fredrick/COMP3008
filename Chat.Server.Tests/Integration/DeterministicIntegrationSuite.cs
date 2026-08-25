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
    internal static class DeterministicIntegrationSuite
    {
        public static int Run(string pollingUrl, string duplexUrl)
        {
            int passed = 0, failed = 0;
            RunTest("Polling message delivery", () => PollingMessageDelivery(pollingUrl), ref passed, ref failed);
            RunTest("Polling join boundary", () => PollingJoinBoundary(pollingUrl), ref passed, ref failed);
            RunTest("Channel file visibility boundary", () => ChannelFileVisibilityBoundary(pollingUrl), ref passed, ref failed);
            RunTest("Duplicate sign-in", () => DuplicateSignIn(pollingUrl), ref passed, ref failed);
            RunTest("Channel membership invariant", () => ChannelMembershipInvariant(pollingUrl), ref passed, ref failed);
            RunTest("Duplex callback delivery", () => DuplexCallbackDelivery(pollingUrl, duplexUrl), ref passed, ref failed);
            Console.WriteLine();
            Console.WriteLine($"Structured integration tests: {passed} passed, {failed} failed");
            return failed == 0 ? 0 : 1;
        }
        private static void RunTest(string name, Action test, ref int passed, ref int failed) { try { test(); passed++; Console.WriteLine($"[PASS] {name}"); } catch (Exception ex) { failed++; Console.WriteLine($"[FAIL] {name}: {ex.Message}"); } }
        private static void PollingMessageDelivery(string url)
        {
            var sf = PollingFactory(url); var rf = PollingFactory(url); var s = sf.CreateChannel(); var r = rf.CreateChannel(); const string sid = "p0_det_sender", rid = "p0_det_receiver", channel = "general", text = "p0-det-public";
            try { SignInClean(s, sid); SignInClean(r, rid); s.CreateChannel(channel); TestAssert.True(s.JoinChannel(sid, channel), "Sender join failed"); TestAssert.True(r.JoinChannel(rid, channel), "Receiver join failed"); TestAssert.False(r.GetPendingMessages(rid).Any(), "Receiver unexpectedly had messages before send"); s.SendMessage(sid, channel, text); WaitUntil(() => r.GetPendingMessages(rid).Any(m => m.SenderId == sid && m.Content == text), 3000, "Receiver did not receive the public message"); }
            finally { SignOut(s, sid); SignOut(r, rid); Abort(s, sf); Abort(r, rf); }
        }
        private static void PollingJoinBoundary(string url)
        {
            var af = PollingFactory(url); var bf = PollingFactory(url); var a = af.CreateChannel(); var b = bf.CreateChannel(); const string aid = "p0_det_boundary_a", bid = "p0_det_boundary_b", channel = "general";
            try { SignInClean(a, aid); SignInClean(b, bid); TestAssert.True(a.JoinChannel(aid, channel), "First join failed"); a.SendMessage(aid, channel, "p0-before-join"); TestAssert.True(b.JoinChannel(bid, channel), "Second join failed"); var beforeJoin = b.GetPendingMessages(bid); TestAssert.False(beforeJoin.Any(m => m.Content == "p0-before-join"), "Pre-join message was replayed"); a.SendMessage(aid, channel, "p0-after-join"); Thread.Sleep(100); var afterJoin = b.GetPendingMessages(bid); TestAssert.True(afterJoin.Any(m => m.Content == "p0-after-join"), "Post-join message was not delivered"); }
            finally { SignOut(a, aid); SignOut(b, bid); Abort(a, af); Abort(b, bf); }
        }
        private static void ChannelFileVisibilityBoundary(string url)
        {
            var af = PollingFactory(url); var bf = PollingFactory(url); var a = af.CreateChannel(); var b = bf.CreateChannel(); const string aid = "p1_file_sender", bid = "p1_file_receiver", channel = "general";
            try
            {
                SignInClean(a, aid); SignInClean(b, bid);
                TestAssert.True(a.JoinChannel(aid, channel), "File sender join failed");
                bool beforeJoinStored = a.ShareFile(aid, channel, "p1-before-join.txt", FileType.Text, new byte[] { 1 });
                TestAssert.True(beforeJoinStored, "Pre-join file share failed");

                TestAssert.True(b.JoinChannel(bid, channel), "File receiver join failed");
                var beforeFiles = b.GetChannelFiles(bid, channel);
                TestAssert.False(beforeFiles.Any(f => f.FileName == "p1-before-join.txt"), "Pre-join file was visible");

                bool afterJoinStored = a.ShareFile(aid, channel, "p1-after-join.txt", FileType.Text, new byte[] { 2 });
                TestAssert.True(afterJoinStored, "Post-join file share failed");
                WaitUntil(() => b.GetChannelFiles(bid, channel).Any(f => f.FileName == "p1-after-join.txt"), 3000, "Post-join file was not visible");

                var nonMember = PollingFactory(url); var nc = nonMember.CreateChannel(); const string nid = "p1_file_nonmember";
                try { SignInClean(nc, nid); TestAssert.False(nc.GetChannelFiles(nid, channel).Any(), "Non-member received channel files"); }
                finally { SignOut(nc, nid); Abort(nc, nonMember); }
            }
            finally { SignOut(a, aid); SignOut(b, bid); Abort(a, af); Abort(b, bf); }
        }
        private static void DuplicateSignIn(string url)
        {
            var f = PollingFactory(url); var c = f.CreateChannel(); const string id = "p0_det_duplicate";
            try { SignOut(c, id); TestAssert.True(c.SignIn(id), "Initial sign-in failed"); TestAssert.False(c.SignIn(id), "Duplicate sign-in was accepted"); } finally { SignOut(c, id); Abort(c, f); }
        }
        private static void ChannelMembershipInvariant(string url)
        {
            var f = PollingFactory(url); var c = f.CreateChannel(); const string id = "p0_det_membership";
            try { const string a = "p0_det_channel_a", b = "p0_det_channel_b"; c.CreateChannel(a); c.CreateChannel(b); SignOut(c, id); TestAssert.True(c.SignIn(id), "Sign-in failed"); TestAssert.True(c.JoinChannel(id, a), "First join failed"); TestAssert.True(c.JoinChannel(id, b), "Second join failed"); TestAssert.False(c.GetChannelMembers(a).Contains(id), "User remained in previous channel"); TestAssert.True(c.GetChannelMembers(b).Contains(id), "User was not moved to second channel"); } finally { SignOut(c, id); Abort(c, f); }
        }
        private static void DuplexCallbackDelivery(string pollingUrl, string duplexUrl)
        {
            var pf = PollingFactory(pollingUrl); var polling = pf.CreateChannel(); var callback = new TestCallback(); var df = new DuplexChannelFactory<IDuplexChatService>(new InstanceContext(callback), new NetTcpBinding(), new EndpointAddress(duplexUrl)); var duplex = df.CreateChannel(); const string id = "p0_det_duplex", channel = "general", text = "p0-det-duplex";
            try { SignOut(polling, id); TestAssert.True(duplex.SignIn(id), "Duplex sign-in failed"); TestAssert.True(polling.JoinChannel(id, channel), "Duplex join failed"); duplex.RegisterCallback(id); duplex.Ping(id, new byte[] { 1, 2, 3 }); polling.SendMessage(id, channel, text); WaitUntil(() => callback.LastMessage != null, 3000, "Duplex message callback was not received"); TestAssert.Equal(text, callback.LastMessage.Content, "Duplex callback contained the wrong message"); duplex.UnregisterCallback(id); }
            finally { SignOut(polling, id); try { ((IClientChannel)duplex).Abort(); } catch { } try { df.Abort(); } catch { } Abort(polling, pf); }
        }
        private static ChannelFactory<IChatService> PollingFactory(string url) => new ChannelFactory<IChatService>(new BasicHttpBinding(), new EndpointAddress(url));
        private static void SignInClean(IChatService c, string id) { SignOut(c, id); TestAssert.True(c.SignIn(id), "Sign-in failed for " + id); }
        private static void SignOut(IChatService c, string id) { try { c.SignOut(id); } catch { } }
        private static void Abort(IChatService proxy, ICommunicationObject factory) { try { ((IClientChannel)proxy).Abort(); } catch { } try { factory.Abort(); } catch { } }
        private static void WaitUntil(Func<bool> condition, int timeoutMs, string message) { var end = DateTime.UtcNow.AddMilliseconds(timeoutMs); while (DateTime.UtcNow < end) { if (condition()) return; Thread.Sleep(50); } throw new InvalidOperationException(message); }
        private sealed class TestCallback : IChatCallback
        {
            public Message LastMessage { get; private set; }
            public void OnChannelListChanged() { } public void OnChannelMembersChanged(string channelName) { } public void OnMessageReceived(Message message) { LastMessage = message; } public void OnPrivateMessageReceived(Message message) { } public void OnFileShared(SharedFile file) { } public void OnPrivateFileShared(SharedFile file) { } public void OnUserDisconnected(string userId) { }
        }
    }
}