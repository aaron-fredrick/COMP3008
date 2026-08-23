using System;
using System.Collections.Generic;
using Chat.Contracts.DataContracts;
using Chat.Contracts.CallbackContracts;
using Chat.Server.Logging;

namespace Chat.Server.StateManagement
{
    /// <summary>
    /// Dispatches WCF duplex callbacks to signed-in duplex clients.
    /// All callback invocations are protected against faulted channels —
    /// a dead callback must never crash or block the server.
    /// Callback state is stored in UserManager; this class is a pure dispatcher.
    /// </summary>
    public class CallbackManager
    {
        private readonly UserManager _userManager;
        private readonly ChannelManager _channelManager;

        public CallbackManager(UserManager userManager, ChannelManager channelManager)
        {
            _userManager = userManager;
            _channelManager = channelManager;
        }

        public void RegisterCallback(string userId, IChatCallback callback)
        {
            _userManager.RegisterCallback(userId, callback);
        }

        public void UnregisterCallback(string userId)
        {
            _userManager.UnregisterCallback(userId);
        }

        /// <summary>
        /// Notifies every signed-in duplex client that the channel list has changed
        /// (a channel was created or a user count changed).
        /// </summary>
        public void NotifyChannelListChanged()
        {
            var allUsers = _userManager.GetAllSignedInUserIds();
            foreach (var userId in allUsers)
            {
                var callback = _userManager.GetCallback(userId);
                if (callback == null)
                {
                    continue;
                }

                SafeInvoke(userId, () => callback.OnChannelListChanged());
            }
        }

        /// <summary>
        /// Notifies every member of <paramref name="channelName"/> that the member list changed.
        /// </summary>
        public void NotifyChannelMembersChanged(string channelName)
        {
            var members = _channelManager.GetChannelMembers(channelName);
            foreach (var memberId in members)
            {
                var callback = _userManager.GetCallback(memberId);
                if (callback == null)
                {
                    continue;
                }

                SafeInvoke(memberId, () => callback.OnChannelMembersChanged(channelName));
            }
        }

        /// <summary>
        /// Notifies every member of the message's channel that a message was received.
        /// </summary>
        public void NotifyMessageReceived(string channelName, Message message)
        {
            var members = _channelManager.GetChannelMembers(channelName);
            foreach (var memberId in members)
            {
                var callback = _userManager.GetCallback(memberId);
                if (callback == null)
                {
                    continue;
                }

                SafeInvoke(memberId, () => callback.OnMessageReceived(message));
            }
        }

        /// <summary>
        /// Notifies the named recipient that a private message was received.
        /// </summary>
        public void NotifyPrivateMessageReceived(string recipientId, Message message)
        {
            var callback = _userManager.GetCallback(recipientId);
            if (callback == null)
            {
                return;
            }

            SafeInvoke(recipientId, () => callback.OnPrivateMessageReceived(message));
        }

        /// <summary>
        /// Notifies every member of <paramref name="channelName"/> that a file was shared.
        /// All channel members receive the notification — not only the uploader.
        /// </summary>
        public void NotifyFileShared(string channelName, SharedFile file)
        {
            var members = _channelManager.GetChannelMembers(channelName);
            foreach (var memberId in members)
            {
                var callback = _userManager.GetCallback(memberId);
                if (callback == null)
                {
                    continue;
                }

                SafeInvoke(memberId, () => callback.OnFileShared(file));
            }
        }

        public void NotifyPrivateFileShared(string recipientId, SharedFile file)
        {
            var callback = _userManager.GetCallback(recipientId);
            if (callback == null) return;
            SafeInvoke(recipientId, () => callback.OnPrivateFileShared(file));
        }

        /// <summary>
        /// Notifies remaining members of <paramref name="channelName"/> that
        /// <paramref name="disconnectedUserId"/> has left or disconnected.
        /// The disconnected user's own callback is not invoked.
        /// </summary>
        public void NotifyUserDisconnected(string channelName, string disconnectedUserId)
        {
            var members = _channelManager.GetChannelMembers(channelName);
            foreach (var memberId in members)
            {
                if (memberId == disconnectedUserId)
                {
                    continue;
                }

                var callback = _userManager.GetCallback(memberId);
                if (callback == null)
                {
                    continue;
                }

                SafeInvoke(memberId, () => callback.OnUserDisconnected(disconnectedUserId));
            }
        }

        /// <summary>
        /// Invokes <paramref name="callbackAction"/> and swallows any communication
        /// exception so a dead callback cannot crash or stall the server.
        /// </summary>
        private static void SafeInvoke(string userId, Action callbackAction)
        {
            try
            {
                callbackAction();
            }
            catch (Exception ex)
            {
                ServerLogger.Warning("CALLBACK", "INVOKE", $"Callback to {userId} failed: {ex.Message}");
            }
        }
    }
}
