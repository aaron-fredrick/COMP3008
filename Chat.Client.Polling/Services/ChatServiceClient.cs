using System;
using System.Configuration;
using System.ServiceModel;
using Chat.Contracts.ServiceContracts;
using Chat.Contracts.DataContracts;
using Chat.Contracts.SharedTypes;

namespace Chat.Client.Polling.Services
{
    public class ChatServiceClient : IDisposable
    {
        private ChannelFactory<IChatService> _channelFactory;
        private IChatService _proxy;
        private string _serverUrl;

        public ChatServiceClient()
        {
            _serverUrl = ConfigurationManager.AppSettings["ServerUrl"] ?? "http://localhost:9000/ChatService/Polling";
            Initialize();
        }

        public ChatServiceClient(string serverUrl)
        {
            _serverUrl = serverUrl;
            Initialize();
        }

        private void Initialize()
        {
            var binding = new BasicHttpBinding();
            var endpoint = new EndpointAddress(_serverUrl);
            _channelFactory = new ChannelFactory<IChatService>(binding, endpoint);
            _proxy = _channelFactory.CreateChannel();
        }

        public bool SignIn(string userId)
        {
            try
           {
                return _proxy.SignIn(userId);
            }
            catch (Exception ex)
            {
                HandleError(ex);
                return false;
            }
        }

        public void SignOut(string userId)
        {
            try
            {
                _proxy.SignOut(userId);
            }
            catch (Exception ex)
            {
                HandleError(ex);
            }
        }

        public System.Collections.Generic.List<Channel> GetChannels()
        {
            try
            {
                return _proxy.GetChannels();
            }
            catch (Exception ex)
            {
                HandleError(ex);
                return new System.Collections.Generic.List<Channel>();
            }
        }

        public bool CreateChannel(string channelName)
        {
            try
            {
                return _proxy.CreateChannel(channelName);
            }
            catch (Exception ex)
            {
                HandleError(ex);
                return false;
            }
        }

        public bool JoinChannel(string userId, string channelName)
        {
            try
            {
                return _proxy.JoinChannel(userId, channelName);
            }
            catch (Exception ex)
            {
                HandleError(ex);
                return false;
            }
        }

        public void LeaveChannel(string userId)
        {
            try
            {
                _proxy.LeaveChannel(userId);
            }
            catch (Exception ex)
            {
                HandleError(ex);
            }
        }

        public System.Collections.Generic.List<string> GetChannelMembers(string channelName)
        {
            try
            {
                return _proxy.GetChannelMembers(channelName);
            }
            catch (Exception ex)
            {
                HandleError(ex);
                return new System.Collections.Generic.List<string>();
            }
        }

        public void SendMessage(string senderId, string channelName, string content)
        {
            try
            {
                _proxy.SendMessage(senderId, channelName, content);
            }
            catch (Exception ex)
            {
                HandleError(ex);
            }
        }

        public void SendPrivateMessage(string senderId, string recipientId, string content)
        {
            try
            {
                _proxy.SendPrivateMessage(senderId, recipientId, content);
            }
            catch (Exception ex)
            {
                HandleError(ex);
            }
        }

        public System.Collections.Generic.List<Message> GetPendingMessages(string userId)
        {
            try
            {
                return _proxy.GetPendingMessages(userId);
            }
            catch (Exception ex)
            {
                HandleError(ex);
                return new System.Collections.Generic.List<Message>();
            }
        }

        public System.Collections.Generic.List<Message> GetPendingPrivateMessages(string userId)
        {
            try
            {
                return _proxy.GetPendingPrivateMessages(userId);
            }
            catch (Exception ex)
            {
                HandleError(ex);
                return new System.Collections.Generic.List<Message>();
            }
        }

        public bool ShareFile(string uploaderId, string channelName, string fileName, FileType fileType, byte[] fileData)
        {
            try
            {
                return _proxy.ShareFile(uploaderId, channelName, fileName, fileType, fileData);
            }
            catch (Exception ex)
            {
                HandleError(ex);
                return false;
            }
        }

        public SharedFile GetFile(string channelName, string fileName)
        {
            try
            {
                return _proxy.GetFile(channelName, fileName);
            }
            catch (Exception ex)
            {
                HandleError(ex);
                return null;
            }
        }

        public System.Collections.Generic.List<SharedFile> GetChannelFiles(string channelName)
        {
            try
            {
                return _proxy.GetChannelFiles(channelName);
            }
            catch (Exception ex)
            {
                HandleError(ex);
                return new System.Collections.Generic.List<SharedFile>();
            }
        }

        private void HandleError(Exception ex)
        {
            // Log error or raise event for UI to handle
            System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}");
        }

        public void Dispose()
        {
            if (_proxy != null)
            {
                var channel = _proxy as ICommunicationObject;
                if (channel != null && channel.State == CommunicationState.Opened)
                {
                    channel.Close();
                }
            }

            if (_channelFactory != null)
            {
                _channelFactory.Close();
            }
        }
    }
}
