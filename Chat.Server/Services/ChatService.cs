using System;
using System.Collections.Generic;
using System.ServiceModel;
using Chat.Contracts.ServiceContracts;
using Chat.Contracts.CallbackContracts;
using Chat.Contracts.DataContracts;
using Chat.Contracts.SharedTypes;
using Chat.Server.StateManagement;
using Chat.Server.FileStorage;

namespace Chat.Server.Services
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
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
            _callbackManager = new CallbackManager(_userManager);
            _messageRouter = new MessageRouter(_userManager, _channelManager, _callbackManager);
            _fileHandler = new FileHandler();
        }

        public bool SignIn(string userId)
        {
            bool result = _userManager.TrySignIn(userId, out string reason);
            LogRequest($"SignIn - User: {userId}, Success: {result}, Reason: {reason}");
            return result;
        }

        public void SignOut(string userId)
        {
            LogRequest($"SignOut - User: {userId}");
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
            LogRequest($"GetChannels - Count: {channels.Count}");
            return channels;
        }

        public bool CreateChannel(string channelName)
        {
            bool result = _channelManager.TryCreateChannel(channelName, out string reason);
            LogRequest($"CreateChannel - Name: {channelName}, Success: {result}, Reason: {reason}");
            return result;
        }

        public bool JoinChannel(string userId, string channelName)
        {
            LogRequest($"JoinChannel - User: {userId}, Channel: {channelName}");
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
            }

            return success;
        }

        public void LeaveChannel(string userId)
        {
            LogRequest($"LeaveChannel - User: {userId}");
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
            LogRequest($"GetChannelMembers - Channel: {channelName}, Count: {members.Count}");
            return members;
        }

        public void SendMessage(string senderId, string channelName, string content)
        {
            LogRequest($"SendMessage - Sender: {senderId}, Channel: {channelName}, Content: {content}");
            _messageRouter.RoutePublicMessage(senderId, channelName, content, out string reason);
        }

        public void SendPrivateMessage(string senderId, string recipientId, string content)
        {
            LogRequest($"SendPrivateMessage - Sender: {senderId}, Recipient: {recipientId}, Content: {content}");
            _messageRouter.RoutePrivateMessage(senderId, recipientId, content, out string reason);
        }

        public bool ShareFile(string uploaderId, string channelName, string fileName, FileType fileType, byte[] fileData)
        {
            bool result = _fileHandler.StoreFile(uploaderId, channelName, fileName, fileType, fileData, out string reason);
            LogRequest($"ShareFile - Uploader: {uploaderId}, Channel: {channelName}, File: {fileName}, Type: {fileType}, Success: {result}, Reason: {reason}");
            return result;
        }

        public SharedFile GetFile(string channelName, string fileName)
        {
            LogRequest($"GetFile - Channel: {channelName}, File: {fileName}");
            return _fileHandler.GetFile(channelName, fileName);
        }

        public List<Message> GetPendingMessages(string userId)
        {
            var pendingQueue = _userManager.GetPendingChannelMessages(userId);
            LogRequest($"GetPendingMessages - User: {userId}, Count: {pendingQueue.Count}");
            return new List<Message>(pendingQueue);
        }

        public List<Message> GetPendingPrivateMessages(string userId)
        {
            var pendingQueue = _userManager.GetPendingPrivateMessages(userId);
            LogRequest($"GetPendingPrivateMessages - User: {userId}, Count: {pendingQueue.Count}");
            return new List<Message>(pendingQueue);
        }

        public void RegisterCallback(string userId)
        {
            LogRequest($"RegisterCallback - User: {userId}");
            var callback = OperationContext.Current.GetCallbackChannel<IChatCallback>();
            _callbackManager.RegisterCallback(userId, callback);
        }

        public void UnregisterCallback(string userId)
        {
            LogRequest($"UnregisterCallback - User: {userId}");
            _callbackManager.UnregisterCallback(userId);
        }

        private void LogRequest(string message)
        {
            string logMessage = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC] [REQUEST] {message}";
            string logPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ChatServer.log");
            try
            {
                System.IO.File.AppendAllText(logPath, logMessage + Environment.NewLine);
            }
            catch { }
            Console.WriteLine(logMessage);
        }
    }
}
