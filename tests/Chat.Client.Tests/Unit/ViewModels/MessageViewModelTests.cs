using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Chat.Contracts.DataContracts;
using Chat.Contracts.SharedTypes;
using Chat.Client.Shared.ViewModels;
using Chat.Client.Tests.Helpers;

namespace Chat.Client.Tests.Unit.ViewModels
{
    [TestClass]
    public class MessageViewModelTests
    {
        // ── Property delegation ────────────────────────────────────────────────

        [TestMethod]
        [TestCategory("Unit")]
        public void SenderId_DelegatesToMessage()
        {
            var vm = MakeVm(MessageBuilder.Text("alice", "hi"));
            Assert.AreEqual("alice", vm.SenderId);
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void Content_DelegatesToMessage()
        {
            var vm = MakeVm(MessageBuilder.Text("alice", "hello world"));
            Assert.AreEqual("hello world", vm.Content);
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void Type_DelegatesToMessage()
        {
            var vm = MakeVm(MessageBuilder.Text("alice", "text"));
            Assert.AreEqual(MessageType.Public, vm.Type);
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void IsCurrentUser_ReflectsMessage()
        {
            var own = MakeVm(MessageBuilder.OwnText("me", "mine"));
            var other = MakeVm(MessageBuilder.Text("them", "theirs"));

            Assert.IsTrue(own.IsCurrentUser);
            Assert.IsFalse(other.IsCurrentUser);
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void FileId_DelegatesToMessage()
        {
            var id = Guid.NewGuid();
            var vm = MakeVm(MessageBuilder.File("alice", "doc.txt", id));
            Assert.AreEqual(id, vm.FileId);
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void FileId_IsNull_ForTextMessages()
        {
            var vm = MakeVm(MessageBuilder.Text("alice", "text"));
            Assert.IsNull(vm.FileId);
        }

        // ── Timestamp ─────────────────────────────────────────────────────────

        [TestMethod]
        [TestCategory("Unit")]
        public void Timestamp_IsConvertedToLocalTime()
        {
            var utc = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc);
            var vm = MakeVm(MessageBuilder.Text("alice", "ts", utc));

            // The ViewModel converts to local time; we just verify it's not UTC kind
            Assert.AreNotEqual(DateTimeKind.Utc, vm.Timestamp.Kind);
        }

        // ── HeaderText ────────────────────────────────────────────────────────

        [TestMethod]
        [TestCategory("Unit")]
        public void HeaderText_ForOtherUser_ContainsSenderIdAndTime()
        {
            var utc = new DateTime(2024, 1, 15, 14, 30, 0, DateTimeKind.Utc);
            var vm = MakeVm(MessageBuilder.Text("alice", "msg", utc));

            StringAssert.Contains(vm.HeaderText, "alice");
            // Time portion should follow HH:mm format (local conversion)
            Assert.IsTrue(System.Text.RegularExpressions.Regex.IsMatch(vm.HeaderText, @"\d{2}:\d{2}"),
                "HeaderText must include a HH:mm time");
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void HeaderText_ForCurrentUser_ContainsTimeOnly()
        {
            var utc = new DateTime(2024, 1, 15, 14, 30, 0, DateTimeKind.Utc);
            var vm = MakeVm(MessageBuilder.OwnText("me", "my msg", utc));

            // Current-user header shows time only, no sender name
            Assert.IsFalse(vm.HeaderText.Contains("me"),
                "HeaderText for own messages must not repeat the sender ID");
            Assert.IsTrue(System.Text.RegularExpressions.Regex.IsMatch(vm.HeaderText, @"\d{2}:\d{2}"),
                "HeaderText must include a HH:mm time");
        }

        // ── ShowMetadata ──────────────────────────────────────────────────────

        [TestMethod]
        [TestCategory("Unit")]
        public void ShowMetadata_TruePassedThrough()
        {
            var vm = new MessageViewModel(MessageBuilder.Text("alice", "hi"), showMetadata: true);
            Assert.IsTrue(vm.ShowMetadata);
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void ShowMetadata_FalsePassedThrough()
        {
            var vm = new MessageViewModel(MessageBuilder.Text("alice", "hi"), showMetadata: false);
            Assert.IsFalse(vm.ShowMetadata);
        }

        // ── Helper ────────────────────────────────────────────────────────────

        private static MessageViewModel MakeVm(Message message, bool showMetadata = true)
            => new MessageViewModel(message, showMetadata);
    }
}
