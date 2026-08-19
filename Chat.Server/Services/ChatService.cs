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
                if (session.CurrentChannel != null)
                {
                    _channelManager.LeaveChannel(userId, session.CurrentChannel);
                }
                _callbackManager.UnregisterCallback(userId);
            }
            _userManager.SignOut(userId);
        }

        public List<Channel> GetChannels()
        {
            var channels = _channelManager.GetChannels();
            // Don't log polling requests to reduce noise
            return channels;
        }

        public bool CreateChannel(string channelName)
        {
            bool result = _channelManager.TryCreateChannel(channelName, out string reason);
            string clientType = DetectClientType();
            if (result)
            {
                ServerLogger.Success(clientType, "CHANNEL", $"Created \"{channelName}\"");
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
            }

            bool success = _channelManager.JoinChannel(userId, channelName, out previousChannel);
            if (success)
            {
                _userManager.SetUserChannel(userId, channelName);
                ServerLogger.Success(clientType, "JOIN", $"{userId} → {channelName}");
            }
            else
            {
                ServerLogger.Warning(clientType, "JOIN", $"{userId} → {channelName} failed");
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
                _channelManager.LeaveChannel(userId, session.CurrentChannel);
                _userManager.SetUserChannel(userId, null);
            }
        }

        public List<string> GetChannelMembers(string channelName)
        {
            var members = _channelManager.GetChannelMembers(channelName);
            // Don't log polling requests to reduce noise
            return members;
        }

        public void SendMessage(string senderId, string channelName, string content)
        {
            string clientType = DetectClientType();
            ServerLogger.Request(clientType, "MESSAGE", $"{senderId} → {channelName}: {content}");
            _messageRouter.RoutePublicMessage(senderId, channelName, content, out string reason);
        }

        public void SendPrivateMessage(string senderId, string recipientId, string content)
        {
            string clientType = DetectClientType();
            ServerLogger.Request(clientType, "PRIVATE", $"{senderId} → {recipientId}: {content}");
            _messageRouter.RoutePrivateMessage(senderId, recipientId, content, out string reason);
        }

        public bool ShareFile(string uploaderId, string channelName, string fileName, FileType fileType, byte[] fileData)
        {
            bool result = _fileHandler.StoreFile(uploaderId, channelName, fileName, fileType, fileData, out string reason);
            string clientType = DetectClientType();
            if (result)
            {
                ServerLogger.Success(clientType, "FILE", $"{uploaderId} shared {fileName} in {channelName}");
            }
            else
            {
                ServerLogger.Warning(clientType, "FILE", $"Share failed: {reason}");
            }
            return result;
        }

        public SharedFile GetFile(string channelName, string fileName)
        {
            string clientType = DetectClientType();
            ServerLogger.Request(clientType, "FILE", $"Download {fileName} from {channelName}");
            return _fileHandler.GetFile(channelName, fileName);
        }

        public List<Message> GetPendingMessages(string userId)
        {
            var pendingQueue = _userManager.GetPendingChannelMessages(userId);
            string clientType = DetectClientType();
            if (pendingQueue.Count > 0)
            {
                ServerLogger.Request(clientType, "POLL", $"{userId} ← {pendingQueue.Count} message(s)");
            }
            return new List<Message>(pendingQueue);
        }

        public List<Message> GetPendingPrivateMessages(string userId)
        {
            var pendingQueue = _userManager.GetPendingPrivateMessages(userId);
            string clientType = DetectClientType();
            if (pendingQueue.Count > 0)
            {
                ServerLogger.Request(clientType, "POLL", $"{userId} ← {pendingQueue.Count} private message(s)");
            }
            return new List<Message>(pendingQueue);
        }

        public void RegisterCallback(string userId)
        {
            string clientType = DetectClientType();
            ServerLogger.Success(clientType, "CALLBACK", $"{userId} registered");
            var callback = OperationContext.Current.GetCallbackChannel<IChatCallback>();
            _callbackManager.RegisterCallback(userId, callback);
        }

        public void UnregisterCallback(string userId)
        {
            string clientType = DetectClientType();
            ServerLogger.Request(clientType, "CALLBACK", $"{userId} unregistered");
            _callbackManager.UnregisterCallback(userId);
        }

        private string DetectClientType()
        {
            try
            {
                if (OperationContext.Current != null)
                {
                    var channel = OperationContext.Current.Channel;
                    if (channel != null)
                    {
                        var binding = channel.GetProperty<System.ServiceModel.Channels.Binding>();
                        if (binding != null)
                        {
                            if (binding is System.ServiceModel.NetTcpBinding)
                            {
                                return "DUPLEX";
                            }
                            else if (binding is System.ServiceModel.BasicHttpBinding || binding is System.ServiceModel.WSHttpBinding)
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
