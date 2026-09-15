using System;
using System.Collections.Generic;
using Chat.Client.Shared.ViewModels;
using Chat.Contracts.DataContracts;
using Chat.Contracts.SharedTypes;

namespace Chat.Client.Tests.Helpers
{
    /// <summary>
    /// Builds <see cref="Message"/> instances for use in tests, with sensible defaults.
    /// </summary>
    internal static class MessageBuilder
    {
        public static Message Text(string senderId, string content, DateTime? timestamp = null)
        {
            return new Message
            {
                SenderId = senderId,
                Content = content,
                Type = MessageType.Public,
                Timestamp = timestamp ?? new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc),
                IsCurrentUser = false
            };
        }

        public static Message OwnText(string senderId, string content, DateTime? timestamp = null)
        {
            var msg = Text(senderId, content, timestamp);
            msg.IsCurrentUser = true;
            return msg;
        }

        public static Message File(string senderId, string fileName, Guid? fileId = null, DateTime? timestamp = null)
        {
            return new Message
            {
                SenderId = senderId,
                Content = $"Shared file: {fileName}",
                Type = MessageType.File,
                FileId = fileId ?? Guid.NewGuid(),
                Timestamp = timestamp ?? new DateTime(2024, 1, 15, 10, 31, 0, DateTimeKind.Utc),
                IsCurrentUser = false
            };
        }

        /// <summary>
        /// Wraps the given <see cref="Message"/> objects as <see cref="MessageViewModel"/> instances
        /// so they can be passed to <see cref="Chat.Client.Shared.Services.ChatExportService.ExportChannelChat"/>.
        /// </summary>
        public static List<ConversationItemViewModel> Sequence(params Message[] messages)
        {
            var result = new List<ConversationItemViewModel>(messages.Length);
            foreach (var m in messages)
                result.Add(new MessageViewModel(m, showMetadata: true));
            return result;
        }

        /// <summary>
        /// Wraps a single <see cref="Message"/> as a <see cref="MessageViewModel"/>.
        /// Convenience overload for tests that construct messages inline.
        /// </summary>
        public static List<ConversationItemViewModel> Wrap(params Message[] messages)
            => Sequence(messages);
    }
}
