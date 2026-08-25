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
    internal static class P1HardeningIntegrationSuite
    {
        public static int Run(string pollingUrl, string duplexUrl)
        {
            int passed = 0, failed = 0;
            RunTest("Channel file download authorization", () => ChannelFileDownloadAuthorization(pollingUrl), ref passed, ref failed);
            RunTest("Private message same-channel authorization", () => PrivateMessageSameChannelAuthorization(pollingUrl), ref passed, ref failed);
            RunTest("Private file same-channel authorization", () => PrivateFileSameChannelAuthorization(pollingUrl), ref passed, ref failed);
            RunTest("Private file pending delivery is user-scoped", () => PrivateFilePendingDelivery(pollingUrl), ref passed, ref failed);
            RunTest("Duplex file notification", () => DuplexFileNotification(pollingUrl, duplexUrl), ref passed, ref failed);
            Console.WriteLine($"P1 hardening tests: {passed} passed, {failed} failed");
            return failed == 0 ? 0 : 1;
        }

        private static void RunTest(string name, Action test, ref int passed, ref int failed)
        {
            try { test(); passed++; Console.WriteLine($"[PASS] {name}"); }
            catch (Exception ex) { failed++; Console.WriteLine($"[FAIL] {name}: {ex.Message}"); }
        }

        private static void ChannelFileDownloadAuthorization(string url)
        {
            var af = PollingFactory(url); var bf = PollingFactory(url); var nf = PollingFactory(url);
            var a = af.CreateChannel(); var b = bf.CreateChannel(); var n = nf.CreateChannel();
            const string aid = "p1_auth_file_sender", bid = "p1_auth_file_member", nid = "p1_auth_file_nonmember", channel = "general";
            try
            {
                SignInClean(a, aid); SignInClean(b, bid); SignInClean(n, nid);
                TestAssert.True(a.JoinChannel(aid, channel), "Sender join failed");
                TestAssert.True(b.JoinChannel(bid, channel), "Member join failed");
                byte[] payload = { 7, 8, 9 };
                TestAssert.True(a.ShareFile(aid, channel, "p1-authorized.txt", FileType.Txt, payload), "File share failed");
                var metadata = b.GetChannelFiles(bid, channel).FirstOrDefault(f => f.FileName == "p1-authorized.txt");
                TestAssert.True(metadata != null, "Current channel member could not see the uploaded file metadata");
                TestAssert.True(metadata.FileData == null, "File listing must not transfer file bytes");
                var downloaded = b.GetFile(bid, metadata.FileId);
                TestAssert.True(downloaded != null, "Current channel member could not download the file");
                TestAssert.True(downloaded.FileData.SequenceEqual(payload), "Downloaded file content did not match uploaded content");
                TestAssert.True(n.GetFile(nid, metadata.FileId) == null, "Non-member was allowed to download a channel file");
                TestAssert.True(b.GetFile(bid, Guid.NewGuid()) == null, "Unknown file ID should not be downloadable");
            }
            finally { SignOut(a, aid); SignOut(b, bid); SignOut(n, nid); Abort(a, af); Abort(b, bf); Abort(n, nf); }
        }

        private static void PrivateMessageSameChannelAuthorization(string url)
        {
            var sf = PollingFactory(url); var rf = PollingFactory(url); var of = PollingFactory(url);
            var s = sf.CreateChannel(); var r = rf.CreateChannel(); var o = of.CreateChannel();
            const string sid = "p1_pm_sender", rid = "p1_pm_recipient", oid = "p1_pm_other";
            try
            {
                SignInClean(s, sid); SignInClean(r, rid); SignInClean(o, oid);
                TestAssert.True(s.JoinChannel(sid, "p1-pm-a"), "Sender join failed");
                TestAssert.True(r.JoinChannel(rid, "p1-pm-a"), "Recipient join failed");
                TestAssert.True(o.JoinChannel(oid, "p1-pm-b"), "Other user join failed");
                TestAssert.True(s.SendPrivateMessage(sid, rid, "same-channel"), "Same-channel private message was rejected");
                TestAssert.True(r.GetPendingPrivateMessages(rid).Any(m => m.SenderId == sid && m.Content == "same-channel"), "Recipient did not receive the same-channel private message");
                TestAssert.False(s.SendPrivateMessage(sid, oid, "cross-channel"), "Cross-channel private message was accepted");
                TestAssert.False(o.GetPendingPrivateMessages(oid).Any(m => m.Content == "cross-channel"), "Rejected cross-channel private message was delivered");
                r.LeaveChannel(rid);
                TestAssert.False(s.SendPrivateMessage(sid, rid, "after-leave"), "Private message was accepted after recipient left the channel");
            }
            finally { SignOut(s, sid); SignOut(r, rid); SignOut(o, oid); Abort(s, sf); Abort(r, rf); Abort(o, of); }
        }

        private static void PrivateFileSameChannelAuthorization(string url)
        {
            var sf = PollingFactory(url); var rf = PollingFactory(url); var of = PollingFactory(url);
            var s = sf.CreateChannel(); var r = rf.CreateChannel(); var o = of.CreateChannel();
            const string sid = "p1_private_file_sender", rid = "p1_private_file_recipient", oid = "p1_private_file_other";
            try
            {
                SignInClean(s, sid); SignInClean(r, rid); SignInClean(o, oid);
                TestAssert.True(s.JoinChannel(sid, "p1-private-a"), "Private-file sender join failed");
                TestAssert.True(r.JoinChannel(rid, "p1-private-a"), "Private-file recipient join failed");
                TestAssert.True(o.JoinChannel(oid, "p1-private-b"), "Private-file other user join failed");
                var file = s.SharePrivateFile(sid, rid, "private.txt", FileType.Txt, new byte[] { 4, 5, 6 });
                TestAssert.True(file != null, "Same-channel private file share was rejected");
                var recipientFile = r.GetPrivateFile(rid, file.FileId);
                TestAssert.True(recipientFile != null && recipientFile.FileData != null && recipientFile.FileData.SequenceEqual(new byte[] { 4, 5, 6 }), "Recipient could not retrieve private file content");
                TestAssert.True(s.GetPrivateFile(sid, file.FileId) != null, "Sender could not retrieve private file");
                TestAssert.True(o.GetPrivateFile(oid, file.FileId) == null, "Unrelated user could retrieve private file");
                r.LeaveChannel(rid);
                TestAssert.True(r.GetPrivateFile(rid, file.FileId) == null, "Recipient could retrieve private file after leaving shared channel");
                TestAssert.True(s.SharePrivateFile(sid, rid, "after-leave.txt", FileType.Txt, new byte[] { 1 }) == null, "Private file share was accepted after recipient left");
            }
            finally { SignOut(s, sid); SignOut(r, rid); SignOut(o, oid); Abort(s, sf); Abort(r, rf); Abort(o, of); }
        }

        private static void PrivateFilePendingDelivery(string url)
        {
            var sf = PollingFactory(url); var rf = PollingFactory(url); var of = PollingFactory(url);
            var s = sf.CreateChannel(); var r = rf.CreateChannel(); var o = of.CreateChannel();
            const string sid = "p1_pending_file_sender", rid = "p1_pending_file_recipient", oid = "p1_pending_file_other";
            try
            {
                SignInClean(s, sid); SignInClean(r, rid); SignInClean(o, oid);
                TestAssert.True(s.JoinChannel(sid, "p1-pending"), "Pending-file sender join failed");
                TestAssert.True(r.JoinChannel(rid, "p1-pending"), "Pending-file recipient join failed");
                TestAssert.True(o.JoinChannel(oid, "p1-pending"), "Pending-file other user join failed");
                var file = s.SharePrivateFile(sid, rid, "pending.txt", FileType.Txt, new byte[] { 10, 11 });
                TestAssert.True(file != null, "Private file setup failed");
                var pending = r.GetPendingPrivateFiles(rid);
                TestAssert.True(pending.Any(f => f.FileId == file.FileId && f.FileData == null), "Recipient did not receive metadata-only pending private file notification");
                TestAssert.False(o.GetPendingPrivateFiles(oid).Any(f => f.FileId == file.FileId), "Unrelated user received the private file notification");
            }
            finally { SignOut(s, sid); SignOut(r, rid); SignOut(o, oid); Abort(s, sf); Abort(r, rf); Abort(o, of); }
        }

        private static void DuplexFileNotification(string pollingUrl, string duplexUrl)
        {
            var pf = PollingFactory(pollingUrl); var polling = pf.CreateChannel();
            var callback = new TestCallback();
            var df = new DuplexChannelFactory<IDuplexChatService>(new InstanceContext(callback), new NetTcpBinding(), new EndpointAddress(duplexUrl));
            var duplex = df.CreateChannel(); const string id = "p1_duplex_file", channel = "p1-duplex-file";
            try
            {
                SignOut(polling, id);
                TestAssert.True(duplex.SignIn(id), "Duplex file test sign-in failed");
                TestAssert.True(polling.JoinChannel(id, channel), "Duplex file test join failed");
                duplex.RegisterCallback(id);
                TestAssert.True(polling.ShareFile(id, channel, "duplex.txt", FileType.Txt, new byte[] { 20, 21 }), "Duplex file share failed");
                WaitUntil(() => callback.LastFile != null, 3000, "Duplex client did not receive file notification");
                TestAssert.Equal("duplex.txt", callback.LastFile.FileName, "Duplex file notification contained the wrong filename");
                TestAssert.True(callback.LastFile.FileData == null, "Duplex file notification must contain metadata, not file bytes");
                duplex.UnregisterCallback(id);
            }
            finally
            {
                SignOut(polling, id);
                try { ((IClientChannel)duplex).Abort(); } catch { }
                try { df.Abort(); } catch { }
                Abort(polling, pf);
            }
        }

        private static ChannelFactory<IChatService> PollingFactory(string url) => new ChannelFactory<IChatService>(new BasicHttpBinding(), new EndpointAddress(url));
        private static void SignInClean(IChatService client, string id) { SignOut(client, id); TestAssert.True(client.SignIn(id), "Sign-in failed for " + id); }
        private static void SignOut(IChatService client, string id) { try { client.SignOut(id); } catch { } }
        private static void Abort(IChatService proxy, ICommunicationObject factory) { try { ((IClientChannel)proxy).Abort(); } catch { } try { factory.Abort(); } catch { } }
        private static void WaitUntil(Func<bool> condition, int timeoutMs, string message) { var end = DateTime.UtcNow.AddMilliseconds(timeoutMs); while (DateTime.UtcNow < end) { if (condition()) return; Thread.Sleep(50); } throw new InvalidOperationException(message); }

        private sealed class TestCallback : IChatCallback
        {
            public SharedFile LastFile { get; private set; }
            public void OnChannelListChanged() { }
            public void OnChannelMembersChanged(string channelName) { }
            public void OnMessageReceived(Message message) { }
            public void OnPrivateMessageReceived(Message message) { }
            public void OnFileShared(SharedFile file) { LastFile = file; }
            public void OnPrivateFileShared(SharedFile file) { }
            public void OnUserDisconnected(string userId) { }
        }
    }
}
