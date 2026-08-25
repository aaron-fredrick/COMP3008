using Chat.Server.StateManagement;
using Chat.Server.Tests.TestInfrastructure;

namespace Chat.Server.Tests.Unit
{
    internal static class UserManagerTests
    {
        public static void Run()
        {
            SignInAndDuplicateId();
            SignOutReleasesId();
            ChannelStateIsTracked();
            PrivateMessageQueueIsConsumedAtomically();
        }

        private static void SignInAndDuplicateId()
        {
            var manager = new UserManager();
            string reason;
            TestAssert.True(manager.TrySignIn("unit-user", out reason), "First sign-in should succeed");
            TestAssert.False(manager.TrySignIn("unit-user", out reason), "Duplicate sign-in should fail");
            TestAssert.True(manager.IsUserSignedIn("unit-user"), "Signed-in user should exist");
        }

        private static void SignOutReleasesId()
        {
            var manager = new UserManager();
            string reason;
            manager.TrySignIn("unit-reuse", out reason);
            manager.SignOut("unit-reuse");
            TestAssert.False(manager.IsUserSignedIn("unit-reuse"), "Sign-out should remove the session");
            TestAssert.True(manager.TrySignIn("unit-reuse", out reason), "Signed-out ID should be reusable");
        }

        private static void ChannelStateIsTracked()
        {
            var manager = new UserManager();
            string reason;
            manager.TrySignIn("unit-channel", out reason);
            manager.SetUserChannel("unit-channel", "general");
            TestAssert.Equal("general", manager.GetUserSession("unit-channel").CurrentChannel, "Current channel was not stored");
            TestAssert.True(manager.GetChannelMembers("general").Contains("unit-channel"), "Channel member lookup is incorrect");
        }

        private static void PrivateMessageQueueIsConsumedAtomically()
        {
            var manager = new UserManager();
            string reason;
            manager.TrySignIn("unit-pm", out reason);
            var message = new Chat.Contracts.DataContracts.Message { SenderId = "sender", Content = "hello" };
            manager.AddPendingPrivateMessage("unit-pm", message);
            var first = manager.ConsumePendingPrivateMessages("unit-pm");
            var second = manager.ConsumePendingPrivateMessages("unit-pm");
            TestAssert.Equal(1, first.Count, "Pending private message was not queued");
            TestAssert.Equal(0, second.Count, "Consumed private messages were not cleared");
        }
    }
}
