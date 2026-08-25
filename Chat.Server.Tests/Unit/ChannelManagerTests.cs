using System;
using Chat.Contracts.DataContracts;
using Chat.Server.StateManagement;
using Chat.Server.Tests.TestInfrastructure;

namespace Chat.Server.Tests.Unit
{
    internal static class ChannelManagerTests
    {
        public static void Run()
        {
            CreateAndRejectDuplicateChannel();
            MembershipIsIdempotent();
            MessageHistoryExcludesSenderAndHonorsMembership();
            MessageHistoryIsBounded();
        }

        private static void CreateAndRejectDuplicateChannel()
        {
            var manager = new ChannelManager();
            string reason;
            TestAssert.True(manager.TryCreateChannel("unit-channel", out reason), "Channel creation should succeed");
            TestAssert.False(manager.TryCreateChannel("unit-channel", out reason), "Duplicate channel creation should fail");
        }

        private static void MembershipIsIdempotent()
        {
            var manager = new ChannelManager();
            string reason;
            manager.TryCreateChannel("unit-members", out reason);
            string previous;
            TestAssert.True(manager.JoinChannel("user", "unit-members", out previous), "Join should succeed");
            TestAssert.True(manager.JoinChannel("user", "unit-members", out previous), "Repeated join should be idempotent");
            TestAssert.Equal(1, manager.GetChannelMembers("unit-members").Count, "Duplicate membership was stored");
            manager.LeaveChannel("user", "unit-members");
            TestAssert.Equal(0, manager.GetChannelMembers("unit-members").Count, "Leave did not remove membership");
        }

        private static void MessageHistoryExcludesSenderAndHonorsMembership()
        {
            var manager = new ChannelManager();
            string reason;
            manager.TryCreateChannel("unit-history", out reason);
            string previous;
            manager.JoinChannel("sender", "unit-history", out previous);
            manager.JoinChannel("receiver", "unit-history", out previous);
            var message = new Message
            {
                SenderId = "sender",
                Content = "hello",
                Timestamp = DateTime.UtcNow
            };
            manager.AddChannelMessage("unit-history", message);
            TestAssert.Equal(0, manager.GetMessagesSince("unit-history", DateTime.MinValue, "sender").Count, "Sender should not receive its own pending message");
            TestAssert.Equal(1, manager.GetMessagesSince("unit-history", DateTime.MinValue, "receiver").Count, "Current member should receive message history");
            TestAssert.Equal(0, manager.GetMessagesSince("unit-history", DateTime.MinValue, "outsider").Count, "Non-member should not receive channel history");
        }

        private static void MessageHistoryIsBounded()
        {
            var manager = new ChannelManager(2);
            string reason;
            manager.TryCreateChannel("unit-bound", out reason);
            string previous;
            manager.JoinChannel("receiver", "unit-bound", out previous);
            for (int i = 0; i < 3; i++)
            {
                manager.AddChannelMessage("unit-bound", new Message
                {
                    SenderId = "sender" + i,
                    Content = "message" + i,
                    Timestamp = DateTime.UtcNow.AddSeconds(i)
                });
            }
            var messages = manager.GetMessagesSince("unit-bound", DateTime.MinValue, "receiver");
            TestAssert.Equal(2, messages.Count, "Channel history exceeded its configured bound");
            TestAssert.False(messages.Exists(m => m.Content == "message0"), "Oldest message was not evicted");
        }
    }
}
