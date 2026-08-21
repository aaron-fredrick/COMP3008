using System;
using System.Configuration;
using System.ServiceModel;
using System.Windows.Threading;
using Chat.Contracts.ServiceContracts;
using Chat.Contracts.CallbackContracts;
using Chat.Contracts.DataContracts;
using Chat.Contracts.SharedTypes;

namespace Chat.Client.Duplex.Services
{
    public class DuplexServiceClient : IDisposable
    {
        private DuplexChannelFactory<IDuplexChatService> _channelFactory;
        private IDuplexChatService _proxy;
        private IChatCallback _callback;
        private InstanceContext _instanceContext;
        private string _serverUrl;
        private Dispatcher _dispatcher;
        private bool _isConnected;

        public bool IsConnected => _isConnected;

        public event EventHandler<Message> MessageReceived;
        public event EventHandler<Message> PrivateMessageReceived;
        public event EventHandler<SharedFile> FileShared;
        public event EventHandler ChannelListChanged;
        public event EventHandler<string> ChannelMembersChanged;
        public event EventHandler<string> UserDisconnected;
        public event EventHandler ConnectionLost;

        public DuplexServiceClient(Dispatcher dispatcher)
        {
            _dispatcher = dispatcher;
            _serverUrl = ConfigurationManager.AppSettings["DuplexServerUrl"] ?? "net.tcp://localhost:9001/ChatService/Duplex";
            Initialize();
        }

        public DuplexServiceClient(string serverUrl, Dispatcher dispatcher)
        {
            _dispatcher = dispatcher;
            _serverUrl = serverUrl;
            Initialize();
        }

        private void Initialize()
        {
            _callback = new ChatCallbackHandler(_dispatcher, this);
            _instanceContext = new InstanceContext(_callback);

            var binding = new NetTcpBinding();
            binding.MaxBufferSize = 2147483647;
            binding.MaxReceivedMessageSize = 2147483647;
            binding.MaxBufferPoolSize = 2147483647;
            binding.ReaderQuotas.MaxDepth = 2147483647;
            binding.ReaderQuotas.MaxStringContentLength = 2147483647;
            binding.ReaderQuotas.MaxArrayLength = 2147483647;
            binding.ReaderQuotas.MaxBytesPerRead = 2147483647;
            binding.ReaderQuotas.MaxNameTableCharCount = 2147483647;
            binding.Security.Mode = SecurityMode.None;

            var endpoint = new EndpointAddress(_serverUrl);
            _channelFactory = new DuplexChannelFactory<IDuplexChatService>(_instanceContext, binding, endpoint);
            _proxy = _channelFactory.CreateChannel();
            _isConnected = true;
        }

        public bool SignIn(string userId)
        {
            try
            {
                bool result = _proxy.SignIn(userId);
                if (result)
                {
                    _proxy.RegisterCallback(userId);
                }
                return result;
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
                _proxy.UnregisterCallback(userId);
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

        public SharedFile GetFile(Guid fileId)
        {
            try
            {
                return _proxy.GetFile(fileId);
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

        public string Ping(string userId, byte[] hash)
        {
            try
            {
                return _proxy.Ping(userId, hash);
            }
            catch (Exception ex)
            {
                HandleError(ex);
                return null;
            }
        }

        private void HandleError(Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}");
            if (_isConnected)
            {
                _isConnected = false;
                _dispatcher.BeginInvoke(new Action(() =>
                {
                    ConnectionLost?.Invoke(this, EventArgs.Empty);
                }));
            }
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

            if (_instanceContext != null)
            {
                _instanceContext.Close();
            }
        }

        internal void OnMessageReceivedInternal(Message message)
        {
            MessageReceived?.Invoke(this, message);
        }

        internal void OnPrivateMessageReceivedInternal(Message message)
        {
            PrivateMessageReceived?.Invoke(this, message);
        }

        internal void OnFileSharedInternal(SharedFile file)
        {
            FileShared?.Invoke(this, file);
        }

        internal void OnChannelListChangedInternal()
        {
            ChannelListChanged?.Invoke(this, EventArgs.Empty);
        }

        internal void OnChannelMembersChangedInternal(string channelName)
        {
            ChannelMembersChanged?.Invoke(this, channelName);
        }

        internal void OnUserDisconnectedInternal(string userId)
        {
            UserDisconnected?.Invoke(this, userId);
        }
    }
}
