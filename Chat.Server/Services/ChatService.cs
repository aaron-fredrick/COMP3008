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
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple, UseSynchronizationContext = false)]
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
            if (result) { ServerLogger.Success(clientType, "CHANNEL", $"Created \"{channelName}\""); _callbackManager.NotifyChannelListChanged(); }
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
                    // Establish deterministic visibility boundaries from server state.
                    // Existing messages/files are not replayed to a newly joined client.
                    _userManager.SetLastPollSequence(userId, _channelManager.GetCurrentSequence(channelName));
                    _userManager.SetChannelFileVisibilityBoundary(userId, DateTime.UtcNow);
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
                    _userManager.SetChannelFileVisibilityBoundary(userId, DateTime.MaxValue);
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
                var fileMessage = new Message { SenderId = uploaderId, Content = $"Shared file: {fileName}", Timestamp = DateTime.UtcNow, Type = MessageType.File, ChannelName = channelName, FileId = storedFile.FileId };
                _messageRouter.RoutePublicMessage(fileMessage, out string _);
            }
            else ServerLogger.Warning(clientType, "FILE", $"Share failed: {reason}");
            return result;
        }

        public SharedFile GetFile(string userId, Guid fileId)
        {
            string clientType = DetectClientType();
            var file = _fileHandler.GetFile(fileId);
            if (file == null) { ServerLogger.Warning(clientType, "FILE", $"{userId} requested an unknown file {fileId}"); return null; }
            if (!IsUserInChannel(userId, file.ChannelName)) { ServerLogger.Warning(clientType, "FILE", $"{userId} attempted to download a file outside their current channel."); return null; }
            ServerLogger.Request(clientType, "FILE", $"{userId} downloaded file {fileId}");
            return file;
        }

        public SharedFile SharePrivateFile(string senderId, string recipientId, string fileName, FileType fileType, byte[] fileData)
        {
            string clientType = DetectClientType();
            var sender = _userManager.GetUserSession(senderId);
            var recipient = _userManager.GetUserSession(recipientId);
            if (sender == null || recipient == null || string.Equals(senderId, recipientId, StringComparison.Ordinal) || string.IsNullOrWhiteSpace(sender.CurrentChannel) || !string.Equals(sender.CurrentChannel, recipient.CurrentChannel, StringComparison.Ordinal))
            { ServerLogger.Warning(clientType, "PRIVATE FILE", $"{senderId} -> {recipientId} rejected: users are not sharing a channel."); return null; }
            if (!_fileHandler.StorePrivateFile(senderId, recipientId, fileName, fileType, fileData, out string reason, out SharedFile storedFile))
            { ServerLogger.Warning(clientType, "PRIVATE FILE", $"Share failed: {reason}"); return null; }
            var metadata = ToFileMetadata(storedFile);
            _userManager.AddPendingPrivateFile(recipientId, metadata);
            _callbackManager.NotifyPrivateFileShared(recipientId, metadata);
            ServerLogger.Success(clientType, "PRIVATE FILE", $"{senderId} shared {fileName} with {recipientId}");
            return metadata;
        }

        public SharedFile GetPrivateFile(string userId, Guid fileId)
        {
            var file = _fileHandler.GetFile(fileId);
            if (file == null || string.IsNullOrWhiteSpace(file.RecipientId) || (!string.Equals(userId, file.UploaderId, StringComparison.Ordinal) && !string.Equals(userId, file.RecipientId, StringComparison.Ordinal))) return null;
            var sender = _userManager.GetUserSession(file.UploaderId);
            var recipient = _userManager.GetUserSession(file.RecipientId);
            var requester = _userManager.GetUserSession(userId);
            if (sender == null || recipient == null || requester == null || string.IsNullOrWhiteSpace(sender.CurrentChannel) || !string.Equals(sender.CurrentChannel, recipient.CurrentChannel, StringComparison.Ordinal) || !string.Equals(requester.CurrentChannel, sender.CurrentChannel, StringComparison.Ordinal)) return null;
            return file;
        }

        public List<SharedFile> GetChannelFiles(string userId, string channelName)
        {
            string clientType = DetectClientType();
            var session = _userManager.GetUserSession(userId);
            if (session == null || !string.Equals(session.CurrentChannel, channelName, StringComparison.Ordinal))
            {
                ServerLogger.Warning(clientType, "FILES", $"{userId} attempted to list files outside their current channel.");
                return new List<SharedFile>();
            }

            var files = _fileHandler.GetChannelFiles(channelName, session.ChannelFileVisibilityBoundary);
            ServerLogger.Request(clientType, "FILES", $"Channel \"{channelName}\": {files.Count} visible file(s)");
            return files;
        }

        public List<Message> GetPendingMessages(string userId)
        {
            string clientType = DetectClientType();
            var session = _userManager.GetUserSession(userId);
            if (session == null) return new List<Message>();
            long lastSequence = _userManager.GetLastPollSequence(userId);
            var messages = _channelManager.GetMessagesSinceSequence(session.CurrentChannel, lastSequence, userId);
            if (messages.Count > 0)
                _userManager.SetLastPollSequence(userId, messages[messages.Count - 1].Sequence);
            else
                _userManager.SetLastPollSequence(userId, _channelManager.GetCurrentSequence(session.CurrentChannel));
            _userManager.UpdateLastPollTime(userId);
            if (messages.Count > 0) ServerLogger.Request(clientType, "POLL", $"{userId} <- {messages.Count} message(s)");
            return messages;
        }

        public List<Message> GetPendingPrivateMessages(string userId)
        {
            var pendingQueue = _userManager.ConsumePendingPrivateMessages(userId);
            string clientType = DetectClientType();
            if (pendingQueue.Count > 0) ServerLogger.Request(clientType, "POLL", $"{userId} <- {pendingQueue.Count} private message(s)");
            return new List<Message>(pendingQueue);
        }

        public List<SharedFile> GetPendingPrivateFiles(string userId)
        {
            var pendingQueue = _userManager.ConsumePendingPrivateFiles(userId);
            return new List<SharedFile>(pendingQueue);
        }

        private static SharedFile ToFileMetadata(SharedFile file)
        {
            return new SharedFile { FileId = file.FileId, FileName = file.FileName, FileType = file.FileType, FileSize = file.FileSize, UploaderId = file.UploaderId, RecipientId = file.RecipientId, UploadedAt = file.UploadedAt, ChannelName = file.ChannelName, FileData = null };
        }

        public string Ping(string userId, byte[] hash)
        {
            string clientType = DetectClientType();
            string clientIp = GetClientIpAddress();
            string hashStr = BitConverter.ToString(hash).Replace("-", "");
            hashStr = hashStr.Substring(0, Math.Min(12, hashStr.Length));
            ServerLogger.Request(clientType, "PING", $"Ping from {userId} (IP: {clientIp}, Hash: {hashStr})");
            return BitConverter.ToString(hash).Replace("-", "");
        }

        public void RegisterCallback(string userId)
        {
            string clientType = DetectClientType();
            ServerLogger.Success(clientType, "CALLBACK", $"{userId} -> registered");
            var callback = OperationContext.Current.GetCallbackChannel<IChatCallback>();
            _callbackManager.RegisterCallback(userId, callback);
        }

        public void UnregisterCallback(string userId)
        {
            string clientType = DetectClientType();
            ServerLogger.Request(clientType, "CALLBACK", $"{userId} -> unregistered");
            _callbackManager.UnregisterCallback(userId);
        }

        private bool IsUserInChannel(string userId, string channelName)
        {
            var session = _userManager.GetUserSession(userId);
            return session != null && string.Equals(session.CurrentChannel, channelName, StringComparison.Ordinal);
        }

        private string GetClientIpAddress()
        {
            try
            {
                if (OperationContext.Current != null && OperationContext.Current.IncomingMessageProperties != null && OperationContext.Current.IncomingMessageProperties.ContainsKey(System.ServiceModel.Channels.RemoteEndpointMessageProperty.Name))
                {
                    var remoteEndpoint = OperationContext.Current.IncomingMessageProperties[System.ServiceModel.Channels.RemoteEndpointMessageProperty.Name] as System.ServiceModel.Channels.RemoteEndpointMessageProperty;
                    if (remoteEndpoint != null) return remoteEndpoint.Address;
                }
            }
            catch (System.ServiceModel.CommunicationException) { return "unknown"; }
            catch (System.ObjectDisposedException) { return "unknown"; }
            catch (System.InvalidOperationException) { return "unknown"; }
            return "unknown";
        }

        private string DetectClientType()
        {
            try
            {
                if (OperationContext.Current != null && OperationContext.Current.IncomingMessageProperties != null)
                {
                    var properties = OperationContext.Current.IncomingMessageProperties;
                    if (properties.ContainsKey("Via"))
                    {
                        var via = properties["Via"] as string;
                        if (via != null)
                        {
                            if (via.Contains("net.tcp://") || via.Contains("net.tcp:")) return "DUPLEX";
                            if (via.Contains("http://") || via.Contains("https://")) return "POLLING";
                        }
                    }
                    if (properties.ContainsKey("RemoteAddressMessageProperty"))
                    {
                        var remoteAddress = properties["RemoteAddressMessageProperty"];
                        if (remoteAddress != null)
                        {
                            var addressStr = remoteAddress.ToString();
                            if (addressStr.Contains("net.tcp")) return "DUPLEX";
                            if (addressStr.Contains("http")) return "POLLING";
                        }
                    }
                    foreach (var key in properties.Keys)
                    {
                        var value = properties[key];
                        if (value != null)
                        {
                            var valueStr = value.ToString();
                            if (valueStr.Contains("net.tcp")) return "DUPLEX";
                            if (valueStr.Contains("http")) return "POLLING";
                        }
                    }
                }
            }
            catch (System.ServiceModel.CommunicationException) { return "UNKNOWN"; }
            catch (System.ObjectDisposedException) { return "UNKNOWN"; }
            catch (System.InvalidOperationException) { return "UNKNOWN"; }
            return "UNKNOWN";
        }
    }
}