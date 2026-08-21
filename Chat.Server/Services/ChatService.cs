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

        public ChatService()
        {
            _userManager = new UserManager();
            _channelManager = new ChannelManager();
            _callbackManager = new CallbackManager(_userManager, _channelManager);
            _messageRouter = new MessageRouter(_userManager, _channelManager, _callbackManager);
            _fileHandler = new FileHandler();
        }

        public bool SignIn(string userId)
        {
            bool result = _userManager.TrySignIn(userId, out string reason);
            string clientType = DetectClientType();
            if (result)
            {
                ServerLogger.Success(clientType, "SIGN-IN", userId);
            }
            else
            {
                ServerLogger.Warning(clientType, "SIGN-IN", $"{userId} failed: {reason}");
            }
            return result;
        }

        public void SignOut(string userId)
        {
            string clientType = DetectClientType();
            ServerLogger.Request(clientType, "SIGN-OUT", userId);

            var session = _userManager.GetUserSession(userId);
            if (session != null)
            {
                string channelBeforeSignOut = session.CurrentChannel;

                if (channelBeforeSignOut != null)
                {
                    _channelManager.LeaveChannel(userId, channelBeforeSignOut);
                    _userManager.SetUserChannel(userId, null);
                    _callbackManager.NotifyUserDisconnected(channelBeforeSignOut, userId);
                    _callbackManager.NotifyChannelMembersChanged(channelBeforeSignOut);
                    _callbackManager.NotifyChannelListChanged();
                }

                _callbackManager.UnregisterCallback(userId);
            }

            _userManager.SignOut(userId);
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
            else
            {
                ServerLogger.Warning(clientType, "CHANNEL", $"Create failed: {reason}");
            }
            return result;
        }

        public bool JoinChannel(string userId, string channelName)
        {
            string clientType = DetectClientType();
            var session = _userManager.GetUserSession(userId);
            if (session == null)
            {
                return false;
            }

            string previousChannel = session.CurrentChannel;
            if (previousChannel != null)
            {
                _channelManager.LeaveChannel(userId, previousChannel);
                _userManager.SetUserChannel(userId, null);

                _callbackManager.NotifyUserDisconnected(previousChannel, userId);
                _callbackManager.NotifyChannelMembersChanged(previousChannel);
            }

            bool success = _channelManager.JoinChannel(userId, channelName, out _);
            if (success)
            {
                _userManager.SetUserChannel(userId, channelName);
                ServerLogger.Success(clientType, "JOIN", $"{userId} -> {channelName}");
                _callbackManager.NotifyChannelMembersChanged(channelName);
                _callbackManager.NotifyChannelListChanged();
            }
            else
            {
                ServerLogger.Warning(clientType, "JOIN", $"{userId} -> {channelName} failed");
            }

            return success;
        }

        public void LeaveChannel(string userId)
        {
            string clientType = DetectClientType();
            ServerLogger.Request(clientType, "LEAVE", userId);

            var session = _userManager.GetUserSession(userId);
            if (session != null && session.CurrentChannel != null)
            {
                string channel = session.CurrentChannel;
                _channelManager.LeaveChannel(userId, channel);
                _userManager.SetUserChannel(userId, null);
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

        public void SendPrivateMessage(string senderId, string recipientId, string content)
        {
            string clientType = DetectClientType();
            ServerLogger.Request(clientType, "PRIVATE", $"{senderId} -> {recipientId}: {content}");
            _messageRouter.RoutePrivateMessage(senderId, recipientId, content, out string reason);
        }

        public bool ShareFile(string uploaderId, string channelName, string fileName, FileType fileType, byte[] fileData)
        {
            bool result = _fileHandler.StoreFile(uploaderId, channelName, fileName, fileType, fileData, out string reason, out SharedFile storedFile);
            string clientType = DetectClientType();
            if (result)
            {
                ServerLogger.Success(clientType, "FILE", $"{uploaderId} shared {fileName} in {channelName}");
                _callbackManager.NotifyFileShared(channelName, storedFile);
                var fileMessage = new Message
                {
                    SenderId = uploaderId,
                    Content = $"Shared file: {fileName}",
                    Timestamp = DateTime.UtcNow,
                    Type = MessageType.File,
                    ChannelName = channelName
                };
                _messageRouter.RoutePublicMessage(fileMessage, out string _);
            }
            else
            {
                ServerLogger.Warning(clientType, "FILE", $"Share failed: {reason}");
            }
            return result;
        }

        public SharedFile GetFile(Guid fileId)
        {
            string clientType = DetectClientType();
            ServerLogger.Request(clientType, "FILE", $"Download file {fileId}");
            return _fileHandler.GetFile(fileId);
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
            if (session == null)
            {
                return new List<Message>();
            }

            var lastPollTime = _userManager.GetLastPollTime(userId);
            var messages = _channelManager.GetMessagesSince(session.CurrentChannel, lastPollTime, userId);

            _userManager.UpdateLastPollTime(userId);

            if (messages.Count > 0)
            {
                ServerLogger.Request(clientType, "POLL", $"{userId} <- {messages.Count} message(s)");
            }
            return messages;
        }

        public List<Message> GetPendingPrivateMessages(string userId)
        {
            var pendingQueue = _userManager.GetPendingPrivateMessages(userId);
            string clientType = DetectClientType();
            if (pendingQueue.Count > 0)
            {
                ServerLogger.Request(clientType, "POLL", $"{userId} <- {pendingQueue.Count} private message(s)");
            }
            return new List<Message>(pendingQueue);
        }

        public string Ping(string userId, byte[] hash)
        {
            string clientType = DetectClientType();
            string clientIp = GetClientIpAddress();
            string hashStr = BitConverter.ToString(hash).Replace("-", "").Substring(0, Math.Min(12, BitConverter.ToString(hash).Replace("-", "").Length));

            string logMessage = $"Ping from {userId} (IP: {clientIp}, Hash: {hashStr})";
            ServerLogger.Request(clientType, "PING", logMessage);

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

        private string GetClientIpAddress()
        {
            try
            {
                if (OperationContext.Current != null && OperationContext.Current.IncomingMessageProperties != null)
                {
                    var properties = OperationContext.Current.IncomingMessageProperties;

                    if (properties.ContainsKey(System.ServiceModel.Channels.RemoteEndpointMessageProperty.Name))
                    {
                        var remoteEndpoint = properties[System.ServiceModel.Channels.RemoteEndpointMessageProperty.Name] as System.ServiceModel.Channels.RemoteEndpointMessageProperty;
                        if (remoteEndpoint != null)
                        {
                            return remoteEndpoint.Address;
                        }
                    }
                }
            }
            catch { }
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
                            if (via.Contains("net.tcp://") || via.Contains("net.tcp:"))
                            {
                                return "DUPLEX";
                            }
                            else if (via.Contains("http://") || via.Contains("https://"))
                            {
                                return "POLLING";
                            }
                        }
                    }

                    if (properties.ContainsKey("RemoteAddressMessageProperty"))
                    {
                        var remoteAddress = properties["RemoteAddressMessageProperty"];
                        if (remoteAddress != null)
                        {
                            var addressStr = remoteAddress.ToString();
                            if (addressStr.Contains("net.tcp"))
                            {
                                return "DUPLEX";
                            }
                            else if (addressStr.Contains("http"))
                            {
                                return "POLLING";
                            }
                        }
                    }

                    foreach (var key in properties.Keys)
                    {
                        var value = properties[key];
                        if (value != null)
                        {
                            var valueStr = value.ToString();
                            if (valueStr.Contains("net.tcp"))
                            {
                                return "DUPLEX";
                            }
                            else if (valueStr.Contains("http"))
                            {
                                return "POLLING";
                            }
                        }
                    }
                }
            }
            catch { }
            return "UNKNOWN";
        }
    }
}
