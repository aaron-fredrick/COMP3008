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
            return _userManager.TrySignIn(userId, out string reason);
        }

        public void SignOut(string userId)
        {
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
            return _channelManager.GetChannels();
        }

        public bool CreateChannel(string channelName)
        {
            return _channelManager.TryCreateChannel(channelName, out string reason);
        }

        public bool JoinChannel(string userId, string channelName)
        {
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
            var session = _userManager.GetUserSession(userId);
            if (session != null && session.CurrentChannel != null)
            {
                _channelManager.LeaveChannel(userId, session.CurrentChannel);
                _userManager.SetUserChannel(userId, null);
            }
        }

        public List<string> GetChannelMembers(string channelName)
        {
            return _channelManager.GetChannelMembers(channelName);
        }

        public void SendMessage(string senderId, string channelName, string content)
        {
            _messageRouter.RoutePublicMessage(senderId, channelName, content, out string reason);
        }

        public void SendPrivateMessage(string senderId, string recipientId, string content)
        {
            _messageRouter.RoutePrivateMessage(senderId, recipientId, content, out string reason);
        }

        public bool ShareFile(string uploaderId, string channelName, string fileName, FileType fileType, byte[] fileData)
        {
            return _fileHandler.StoreFile(uploaderId, channelName, fileName, fileType, fileData, out string reason);
        }

        public SharedFile GetFile(string channelName, string fileName)
        {
            return _fileHandler.GetFile(channelName, fileName);
        }

        public List<Message> GetPendingMessages(string userId)
        {
            var pendingQueue = _userManager.GetPendingChannelMessages(userId);
            return new List<Message>(pendingQueue);
        }

        public List<Message> GetPendingPrivateMessages(string userId)
        {
            var pendingQueue = _userManager.GetPendingPrivateMessages(userId);
            return new List<Message>(pendingQueue);
        }

        public void RegisterCallback(string userId)
        {
            var callback = OperationContext.Current.GetCallbackChannel<IChatCallback>();
            _callbackManager.RegisterCallback(userId, callback);
        }

        public void UnregisterCallback(string userId)
        {
            _callbackManager.UnregisterCallback(userId);
        }
    }
}
