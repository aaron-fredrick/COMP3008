using System;
using System.Collections.Generic;
using System.ServiceModel;
using Chat.Contracts.ServiceContracts;
using Chat.Contracts.CallbackContracts;
using Chat.Contracts.DataContracts;
using Chat.Contracts.SharedTypes;
using Chat.Server.StateManagement;
using Chat.Server.FileStorage;
using Chat.Server.Logging;

namespace Chat.Server.Services
{
    [ServiceBehavior(
        InstanceContextMode = InstanceContextMode.Single,
        ConcurrencyMode = ConcurrencyMode.Multiple,
        UseSynchronizationContext = false)]
    public class ChatService : IChatService, IDuplexChatService
    {
        private readonly UserManager _userManager;
        private readonly ChannelManager _channelManager;
        private readonly MessageRouter _messageRouter;
        private readonly CallbackManager _callbackManager;
        private readonly FileHandler _fileHandler;
        private readonly object _membershipTransitionLock;

        public ChatService() : this(50) { }

        public ChatService(int maxMessages)
        {
            _userManager = new UserManager();
            _channelManager = new ChannelManager(maxMessages);
            _callbackManager = new CallbackManager(_userManager, _channelManager);
            _messageRouter = new MessageRouter(_userManager, _channelManager, _callbackManager);
            _fileHandler = new FileHandler();
            _membershipTransitionLock = new object();
        }

        public bool SignIn(string userId)
        {
            bool result = _userManager.TrySignIn(userId, out string reason);
            string clientType = DetectClientType();
            if (result) ServerLogger.Success(clientType, "SIGN-IN", userId);
            else ServerLogger.Warning(clientType, "SIGN-IN", $"{userId} failed: {reason}");
            return result;
        }

        public void SignOut(string userId)
        {
            string clientType = DetectClientType();
            ServerLogger.Request(clientType, "SIGN-OUT", userId);
            string channelBeforeSignOut = null;
            bool hadSession = false;

            lock (_membershipTransitionLock)
            {
                var session = _userManager.GetUserSession(userId);
                if (session != null)
                {
                    hadSession = true;
                    channelBeforeSignOut = session.CurrentChannel;
                    if (channelBeforeSignOut != null)
                    {
                        _channelManager.LeaveChannel(userId, channelBeforeSignOut);
                        _userManager.SetUserChannel(userId, null);
                    }
                    _callbackManager.UnregisterCallback(userId);
                    _userManager.SignOut(userId);
                }
            }

            if (hadSession && channelBeforeSignOut != null)
            {
                _callbackManager.NotifyUserDisconnected(channelBeforeSignOut, userId);
                _callbackManager.NotifyChannelMembersChanged(channelBeforeSignOut);
                _callbackManager.NotifyChannelListChanged();
            }
        }

        public List<Channel> GetChannels()
        {
            string clientType = DetectClientType();
            var channels = _channelManager.GetChannels();
            ServerLogger.Request(clientType, "CHANNELS", $"Returned {channels.Count} channel(s)");
            return channels;
        }

        public bool CreateChannel(string channelName)
        {
            bool result = _channelManager.TryCreateChannel(channelName, out string reason);
            string clientType = DetectClientType();
            if (result)
            {
                ServerLogger.Success(clientType, "CHANNEL", $"Created \"{channelName}\"");
                _callbackManager.NotifyChannelListChanged();
            }
            else ServerLogger.Warning(clientType, "CHANNEL", $"Create failed: {reason}");
            return result;
        }

        public bool JoinChannel(string userId, string channelName)
        {
            string clientType = DetectClientType();
            string previousChannel = null;
            bool success;

            lock (_membershipTransitionLock)
            {
                var session = _userManager.GetUserSession(userId);
                if (session == null) return false;

                previousChannel = session.CurrentChannel;
                if (previousChannel != null)
                {
                    _channelManager.LeaveChannel(userId, previousChannel);
                    _userManager.SetUserChannel(userId, null);
                }

                success = _channelManager.JoinChannel(userId, channelName, out _);
                if (success)
                {
                    _userManager.SetUserChannel(userId, channelName);
                    // The boundary must be captured AFTER membership is established.
                    // Otherwise messages sent between the boundary timestamp and the
                    // actual join can be replayed to the newly joined polling client.
                    _userManager.UpdateLastPollTime(userId);
                }
            }

            if (previousChannel != null)
            {
                _callbackManager.NotifyUserDisconnected(previousChannel, userId);
                _callbackManager.NotifyChannelMembersChanged(previousChannel);
            }

            if (success)
            {
                ServerLogger.Success(clientType, "JOIN", $"{userId} -> {channelName}");
                _callbackManager.NotifyChannelMembersChanged(channelName);
                _callbackManager.NotifyChannelListChanged();
            }
            else ServerLogger.Warning(clientType, "JOIN", $"{userId} -> {channelName} failed");
            return success;
        }

        public void LeaveChannel(string userId)
        {
            string clientType = DetectClientType();
            ServerLogger.Request(clientType, "LEAVE", userId);
            string channel = null;
            lock (_membershipTransitionLock)
            {
                var session = _userManager.GetUserSession(userId);
                if (session != null && session.CurrentChannel != null)
                {
                    channel = session.CurrentChannel;
                    _channelManager.LeaveChannel(userId, channel);
                    _userManager.SetUserChannel(userId, null);
                }
            }
            if (channel != null)
            {
                _callbackManager.NotifyUserDisconnected(channel, userId);
                _callbackManager.NotifyChannelMembersChanged(channel);
                _callbackManager.NotifyChannelListChanged();
            }
        }

        public List<string> GetChannelMembers(string channelName)
        {
            string clientType = DetectClientType();
            var members = _channelManager.GetChannelMembers(channelName);
            ServerLogger.Request(clientType, "MEMBERS", $"Channel \"{channelName}\": {members.Count} member(s)");
            return members;
        }

        public void SendMessage(string senderId, string channelName, string content)
        {
            string clientType = DetectClientType();
            ServerLogger.Request(clientType, "MESSAGE", $"{senderId} -> {channelName}: {content}");
            _messageRouter.RoutePublicMessage(senderId, channelName, content, out string reason);
        }

        public bool SendPrivateMessage(string senderId, string recipientId, string content)
        {
            string clientType = DetectClientType();
            ServerLogger.Request(clientType, "PRIVATE", $"{senderId} -> {recipientId}: {content}");
            bool result = _messageRouter.RoutePrivateMessage(senderId, recipientId, content, out string reason);
            if (!result) ServerLogger.Warning(clientType, "PRIVATE", $"{senderId} -> {recipientId} failed: {reason}");
            return result;
        }

        public bool ShareFile(string uploaderId, string channelName, string fileName, FileType fileType, byte[] fileData)
        {
            string clientType = DetectClientType();
            if (!IsUserInChannel(uploaderId, channelName))
            {
                ServerLogger.Warning(clientType, "FILE", $"{uploaderId} attempted to share a file outside their current channel.");
                return false;
            }
            bool result = _fileHandler.StoreFile(uploaderId, channelName, fileName, fileType, fileData, out string reason, out SharedFile storedFile);
            if (result)
            {
                ServerLogger.Success(clientType, "FILE", $"{uploaderId} shared {fileName} in {channelName}");
                _callbackManager.NotifyFileShared(channelName, storedFile);
                var fileMessage = new Message
                {
                    SenderId = uploaderId, Content = $"Shared file: {fileName}", Timestamp = DateTime.UtcNow,
                    Type = MessageType.File, ChannelName = channelName, FileId = storedFile.FileId
                };
                _messageRouter.RoutePublicMessage(fileMessage, out string _);
            }
            else ServerLogger.Warning(clientType, "FILE", $"Share failed: {reason}");
            return result;
        }

        public SharedFile GetFile(string userId, Guid fileId)
        {
            string clientType = DetectClientType();
            var file = _fileHandler.GetFile(fileId);
            if (file == null) return null;
            if (!IsUserInChannel(userId, file.ChannelName)) return null;
            ServerLogger.Request(clientType, "FILE", $"{userId} downloaded file {fileId}");
            return file;
        }

        public List<SharedFile> GetChannelFiles(string channelName)
        {
            string clientType = DetectClientType();
            var files = _fileHandler.GetChannelFiles(channelName);
            ServerLogger.Request(clientType, "FILES", $"Channel \"{channelName}\": {files.Count} file(s)");
            return files;
        }

        public List<Message> GetPendingMessages(string userId)
        {
            string clientType = DetectClientType();
            var session = _userManager.GetUserSession(userId);
            if (session == null) return new List<Message>();
            var lastPollTime = _userManager.GetLastPollTime(userId);
            var messages = _channelManager.GetMessagesSince(session.CurrentChannel, lastPollTime, userId);
            _userManager.UpdateLastPollTime(userId);
            if (messages.Count > 0) ServerLogger.Request(clientType, "POLL", $"{userId} <- {messages.Count} message(s)");
            return messages;
        }

        public List<Message> GetPendingPrivateMessages(string userId)
        {
            var pendingQueue = _userManager.ConsumePendingPrivateMessages(userId);
            return new List<Message>(pendingQueue);
        }

        public List<SharedFile> GetPendingPrivateFiles(string userId)
        {
            var pendingQueue = _userManager.ConsumePendingPrivateFiles(userId);
            return new List<SharedFile>(pendingQueue);
        }

        public string Ping(string userId, byte[] hash)
        {
            string clientType = DetectClientType();
            string clientIp = GetClientIpAddress();
            string hashStr = BitConverter.ToString(hash).Replace("-", "").Substring(0, Math.Min(12, BitConverter.ToString(hash).Replace("-", "").Length));
            ServerLogger.Request(clientType, "PING", $"Ping from {userId} (IP: {clientIp}, Hash: {hashStr})");
            return BitConverter.ToString(hash).Replace("-", "");
        }

        public void RegisterCallback(string userId)
        {
            string clientType = DetectClientType();
            ServerLogger.Success(clientType, "CALLBACK", $"{userId} -> registered");
            var callback = OperationContext.Current.GetCallbackChannel<IChatCallback>();
            _userManager.RegisterCallback(userId, callback);
        }

        public void UnregisterCallback(string userId)
        {
            string clientType = DetectClientType();
            _userManager.UnregisterCallback(userId);
            ServerLogger.Success(clientType, "CALLBACK", $"{userId} -> unregistered");
        }

        public List<Message> GetMessageHistory(string channelName)
        {
            return _channelManager.GetMessagesSince(channelName, DateTime.MinValue, string.Empty);
        }

        private bool IsUserInChannel(string userId, string channelName)
        {
            var session = _userManager.GetUserSession(userId);
            return session != null && string.Equals(session.CurrentChannel, channelName, StringComparison.Ordinal);
        }

        private string DetectClientType()
        {
            try
            {
                var uri = OperationContext.Current?.IncomingMessageHeaders?.To?.AbsoluteUri ?? string.Empty;
                return uri.IndexOf("Duplex", StringComparison.OrdinalIgnoreCase) >= 0 ? "DUPLEX" : "POLLING";
            }
            catch { return "UNKNOWN"; }
        }

        private string GetClientIpAddress()
        {
            try
            {
                var prop = OperationContext.Current.IncomingMessageProperties[RemoteEndpointMessageProperty.Name] as RemoteEndpointMessageProperty;
                return prop?.Address ?? "unknown";
            }
            catch { return "unknown"; }
        }
    }
}
