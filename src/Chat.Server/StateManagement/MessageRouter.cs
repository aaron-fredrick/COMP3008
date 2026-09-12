using System;
using System.Collections.Generic;
using System.Threading;
using Chat.Contracts.DataContracts;
using Chat.Contracts.SharedTypes;

namespace Chat.Server.StateManagement
{
    public class MessageRouter
    {
        private readonly UserManager _userManager;
        private readonly ChannelManager _channelManager;
        private readonly CallbackManager _callbackManager;
        private readonly ReaderWriterLockSlim _lock;

        public MessageRouter(UserManager userManager, ChannelManager channelManager, CallbackManager callbackManager)
        {
            _userManager = userManager;
            _channelManager = channelManager;
            _callbackManager = callbackManager;
            _lock = new ReaderWriterLockSlim();
        }

        public bool RoutePublicMessage(string senderId, string channelName, string content, out string reason)
        {
            var message = new Message
            {
                SenderId = senderId,
                Content = content,
                Timestamp = DateTime.UtcNow,
                Type = MessageType.Public,
                ChannelName = channelName
            };
            return RoutePublicMessage(message, out reason);
        }

        public bool RoutePublicMessage(Message message, out string reason)
        {
            _lock.EnterWriteLock();
            try
            {
                reason = null;

                if (!_userManager.IsUserSignedIn(message.SenderId))
                {
                    reason = "User is not signed in.";
                    return false;
                }

                if (!_channelManager.ChannelExists(message.ChannelName))
                {
                    reason = "Channel does not exist.";
                    return false;
                }

                var members = _channelManager.GetChannelMembers(message.ChannelName);
                if (!members.Contains(message.SenderId))
                {
                    reason = "User is not a member of this channel.";
                    return false;
                }

                // Add to channel message history
                _channelManager.AddChannelMessage(message.ChannelName, message);

                // Notify via callbacks for duplex clients
                _callbackManager.NotifyMessageReceived(message.ChannelName, message);

                return true;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public bool RoutePrivateMessage(string senderId, string recipientId, string content, out string reason)
        {
            var message = new Message
            {
                SenderId = senderId,
                Content = content,
                Timestamp = DateTime.UtcNow,
                Type = MessageType.Private,
                ChannelName = null,
                RecipientId = recipientId
            };

            return RoutePrivateMessage(message, false, out reason);
        }

        public bool RoutePrivateMessage(Message message, bool includeSender, out string reason)
        {
            _lock.EnterWriteLock();
            try
            {
                reason = null;

                if (!_userManager.IsUserSignedIn(message.SenderId))
                {
                    reason = "Sender is not signed in.";
                    return false;
                }

                if (!_userManager.IsUserSignedIn(message.RecipientId))
                {
                    reason = "Recipient is not signed in.";
                    return false;
                }

                var senderSession = _userManager.GetUserSession(message.SenderId);
                var recipientSession = _userManager.GetUserSession(message.RecipientId);

                if (senderSession.CurrentChannel == null || recipientSession.CurrentChannel == null)
                {
                    reason = "Private messages can only be sent between members of the same channel.";
                    return false;
                }

                if (senderSession.CurrentChannel != recipientSession.CurrentChannel)
                {
                    reason = "Private messages can only be sent between members of the same channel.";
                    return false;
                }

                _userManager.AddPendingPrivateMessage(message.RecipientId, message);
                _callbackManager.NotifyPrivateMessageReceived(message.RecipientId, message);

                if (includeSender)
                {
                    _userManager.AddPendingPrivateMessage(message.SenderId, message);
                    _callbackManager.NotifyPrivateMessageReceived(message.SenderId, message);
                }

                return true;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }
    }
}
