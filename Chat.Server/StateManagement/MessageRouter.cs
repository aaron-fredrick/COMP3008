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
        private long _messageIdCounter;

        public MessageRouter(UserManager userManager, ChannelManager channelManager, CallbackManager callbackManager)
        {
            _userManager = userManager;
            _channelManager = channelManager;
            _callbackManager = callbackManager;
            _lock = new ReaderWriterLockSlim();
            _messageIdCounter = 0;
        }

        public bool RoutePublicMessage(string senderId, string channelName, string content, out string reason)
        {
            _lock.EnterWriteLock();
            try
            {
                reason = null;

                if (!_userManager.IsUserSignedIn(senderId))
                {
                    reason = "User is not signed in.";
                    return false;
                }

                if (!_channelManager.ChannelExists(channelName))
                {
                    reason = "Channel does not exist.";
                    return false;
                }

                var members = _channelManager.GetChannelMembers(channelName);
                if (!members.Contains(senderId))
                {
                    reason = "User is not a member of this channel.";
                    return false;
                }

                var message = new Message
                {
                    SenderId = senderId,
                    Content = content,
                    Timestamp = DateTime.UtcNow,
                    Type = MessageType.Public,
                    ChannelName = channelName,
                    RecipientId = null
                };

                foreach (var memberId in members)
                {
                    _userManager.AddPendingChannelMessage(memberId, message);
                }

                _callbackManager.NotifyMessageReceived(channelName, message);

                return true;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public bool RoutePrivateMessage(string senderId, string recipientId, string content, out string reason)
        {
            _lock.EnterWriteLock();
            try
            {
                reason = null;

                if (!_userManager.IsUserSignedIn(senderId))
                {
                    reason = "Sender is not signed in.";
                    return false;
                }

                if (!_userManager.IsUserSignedIn(recipientId))
                {
                    reason = "Recipient is not signed in.";
                    return false;
                }

                var senderSession = _userManager.GetUserSession(senderId);
                var recipientSession = _userManager.GetUserSession(recipientId);

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

                var message = new Message
                {
                    SenderId = senderId,
                    Content = content,
                    Timestamp = DateTime.UtcNow,
                    Type = MessageType.Private,
                    ChannelName = null,
                    RecipientId = recipientId
                };

                _userManager.AddPendingPrivateMessage(recipientId, message);
                _callbackManager.NotifyPrivateMessageReceived(recipientId, message);

                return true;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }
    }
}
