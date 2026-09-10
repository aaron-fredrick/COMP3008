using System;
using System.Collections.Generic;
using System.ServiceModel;
using Chat.Contracts.DataContracts;
using Chat.Contracts.CallbackContracts;
using Chat.Server.Logging;

namespace Chat.Server.StateManagement
{
    /// <summary>
    /// Dispatches WCF duplex callbacks to signed-in duplex clients.
    /// Faulted callbacks are removed and the associated session is cleaned up.
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

        public void RegisterCallback(string userId, IChatCallback callback) => _userManager.RegisterCallback(userId, callback);

        public void UnregisterCallback(string userId) => _userManager.UnregisterCallback(userId);

        public void NotifyChannelListChanged()
        {
            var allUsers = _userManager.GetAllSignedInUserIds();
            foreach (var userId in allUsers)
            {
                var callback = _userManager.GetCallback(userId);
                if (callback != null) SafeInvoke(userId, callback, () => callback.OnChannelListChanged());
            }
        }

        public void NotifyChannelMembersChanged(string channelName)
        {
            var members = _channelManager.GetChannelMembers(channelName);
            foreach (var memberId in members)
            {
                var callback = _userManager.GetCallback(memberId);
                if (callback != null) SafeInvoke(memberId, callback, () => callback.OnChannelMembersChanged(channelName));
            }
        }

        public void NotifyMessageReceived(string channelName, Message message)
        {
            var members = _channelManager.GetChannelMembers(channelName);
            foreach (var memberId in members)
            {
                var callback = _userManager.GetCallback(memberId);
                if (callback != null) SafeInvoke(memberId, callback, () => callback.OnMessageReceived(message));
            }
        }

        public void NotifyPrivateMessageReceived(string recipientId, Message message)
        {
            var callback = _userManager.GetCallback(recipientId);
            if (callback != null) SafeInvoke(recipientId, callback, () => callback.OnPrivateMessageReceived(message));
        }

        public void NotifyFileShared(string channelName, SharedFile file)
        {
            var members = _channelManager.GetChannelMembers(channelName);
            foreach (var memberId in members)
            {
                var callback = _userManager.GetCallback(memberId);
                if (callback != null) SafeInvoke(memberId, callback, () => callback.OnFileShared(file));
            }
        }

        public void NotifyPrivateFileShared(string recipientId, SharedFile file)
        {
            var callback = _userManager.GetCallback(recipientId);
            if (callback != null) SafeInvoke(recipientId, callback, () => callback.OnPrivateFileShared(file));
        }

        public void NotifyUserDisconnected(string channelName, string disconnectedUserId)
        {
            var members = _channelManager.GetChannelMembers(channelName);
            foreach (var memberId in members)
            {
                if (memberId == disconnectedUserId) continue;
                var callback = _userManager.GetCallback(memberId);
                if (callback != null) SafeInvoke(memberId, callback, () => callback.OnUserDisconnected(disconnectedUserId));
            }
        }

        private void SafeInvoke(string userId, IChatCallback callback, Action callbackAction)
        {
            try
            {
                callbackAction();
            }
            catch (CommunicationException ex)
            {
                ServerLogger.Warning("CALLBACK", "INVOKE", $"Callback to {userId} failed: {ex.Message}");
                CleanupDisconnectedUser(userId, callback);
            }
            catch (TimeoutException ex)
            {
                ServerLogger.Warning("CALLBACK", "INVOKE", $"Callback to {userId} timed out: {ex.Message}");
                CleanupDisconnectedUser(userId, callback);
            }
            catch (ObjectDisposedException ex)
            {
                ServerLogger.Warning("CALLBACK", "INVOKE", $"Callback to {userId} was disposed: {ex.Message}");
                CleanupDisconnectedUser(userId, callback);
            }
        }

        private void CleanupDisconnectedUser(string userId, IChatCallback failedCallback)
        {
            // A callback invocation may overlap a normal sign-out/reconnect. Only clean up
            // the session if the callback that failed is still the callback registered for it.
            if (!object.ReferenceEquals(_userManager.GetCallback(userId), failedCallback)) return;

            var session = _userManager.GetUserSession(userId);
            if (session == null) return;

            string channelName = session.CurrentChannel;
            if (channelName != null)
            {
                _channelManager.LeaveChannel(userId, channelName);
                _userManager.SetUserChannel(userId, null);
            }

            _userManager.UnregisterCallback(userId);
            _userManager.SignOut(userId);

            if (channelName != null)
            {
                NotifyUserDisconnected(channelName, userId);
                NotifyChannelMembersChanged(channelName);
                NotifyChannelListChanged();
            }
        }
    }
}
