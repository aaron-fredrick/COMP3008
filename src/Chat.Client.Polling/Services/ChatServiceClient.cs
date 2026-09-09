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
        private bool _isConnected;
        private readonly object _proxyLock = new object();
        public bool IsConnected => _isConnected;
        public ChatServiceClient() { _serverUrl = ConfigurationManager.AppSettings["ServerUrl"] ?? "http://localhost:9000/ChatService/Polling"; Initialize(); }
        public ChatServiceClient(string serverUrl) { _serverUrl = serverUrl; Initialize(); }
        private void Initialize()
        {
            var binding = new BasicHttpBinding();
            binding.MaxBufferSize = 2147483647; binding.MaxReceivedMessageSize = 2147483647; binding.MaxBufferPoolSize = 2147483647;
            binding.ReaderQuotas.MaxDepth = 2147483647; binding.ReaderQuotas.MaxStringContentLength = 2147483647; binding.ReaderQuotas.MaxArrayLength = 2147483647; binding.ReaderQuotas.MaxBytesPerRead = 2147483647; binding.ReaderQuotas.MaxNameTableCharCount = 2147483647;
            _channelFactory = new ChannelFactory<IChatService>(binding, new EndpointAddress(_serverUrl));
            _proxy = _channelFactory.CreateChannel(); _isConnected = true;
        }
        public bool SignIn(string userId) { try { lock (_proxyLock) return _proxy.SignIn(userId); } catch (Exception ex) { HandleError(ex); return false; } }
        public void SignOut(string userId) { try { lock (_proxyLock) _proxy.SignOut(userId); } catch (Exception ex) { HandleError(ex); } }
        public System.Collections.Generic.List<Channel> GetChannels() { try { lock (_proxyLock) return _proxy.GetChannels(); } catch (Exception ex) { HandleError(ex); return new System.Collections.Generic.List<Channel>(); } }
        public bool CreateChannel(string channelName) { try { lock (_proxyLock) return _proxy.CreateChannel(channelName); } catch (Exception ex) { HandleError(ex); return false; } }
        public bool JoinChannel(string userId, string channelName) { try { lock (_proxyLock) return _proxy.JoinChannel(userId, channelName); } catch (Exception ex) { HandleError(ex); return false; } }
        public void LeaveChannel(string userId) { try { lock (_proxyLock) _proxy.LeaveChannel(userId); } catch (Exception ex) { HandleError(ex); } }
        public System.Collections.Generic.List<string> GetChannelMembers(string channelName) { try { lock (_proxyLock) return _proxy.GetChannelMembers(channelName); } catch (Exception ex) { HandleError(ex); return new System.Collections.Generic.List<string>(); } }
        public void SendMessage(string senderId, string channelName, string content) { try { lock (_proxyLock) _proxy.SendMessage(senderId, channelName, content); } catch (Exception ex) { HandleError(ex); } }
        public bool SendPrivateMessage(string senderId, string recipientId, string content) { try { lock (_proxyLock) return _proxy.SendPrivateMessage(senderId, recipientId, content); } catch (Exception ex) { HandleError(ex); return false; } }
        public System.Collections.Generic.List<Message> GetPendingMessages(string userId) { try { lock (_proxyLock) return _proxy.GetPendingMessages(userId); } catch (Exception ex) { HandleError(ex); return new System.Collections.Generic.List<Message>(); } }
        public System.Collections.Generic.List<Message> GetPendingPrivateMessages(string userId) { try { lock (_proxyLock) return _proxy.GetPendingPrivateMessages(userId); } catch (Exception ex) { HandleError(ex); return new System.Collections.Generic.List<Message>(); } }
        public System.Collections.Generic.List<SharedFile> GetPendingPrivateFiles(string userId) { try { lock (_proxyLock) return _proxy.GetPendingPrivateFiles(userId); } catch (Exception ex) { HandleError(ex); return new System.Collections.Generic.List<SharedFile>(); } }
        public bool ShareFile(string uploaderId, string channelName, string fileName, FileType fileType, byte[] fileData) { try { lock (_proxyLock) return _proxy.ShareFile(uploaderId, channelName, fileName, fileType, fileData); } catch (Exception ex) { HandleError(ex); return false; } }
        public SharedFile SharePrivateFile(string senderId, string recipientId, string fileName, FileType fileType, byte[] fileData) { try { lock (_proxyLock) return _proxy.SharePrivateFile(senderId, recipientId, fileName, fileType, fileData); } catch (Exception ex) { HandleError(ex); return null; } }
        public SharedFile GetFile(string userId, Guid fileId) { try { lock (_proxyLock) return _proxy.GetFile(userId, fileId); } catch (Exception ex) { HandleError(ex); return null; } }
        public SharedFile GetPrivateFile(string userId, Guid fileId) { try { lock (_proxyLock) return _proxy.GetPrivateFile(userId, fileId); } catch (Exception ex) { HandleError(ex); return null; } }
        public System.Collections.Generic.List<SharedFile> GetChannelFiles(string userId, string channelName) { try { lock (_proxyLock) return _proxy.GetChannelFiles(userId, channelName); } catch (Exception ex) { HandleError(ex); return new System.Collections.Generic.List<SharedFile>(); } }
        public string Ping(string userId, byte[] hash) { try { lock (_proxyLock) return _proxy.Ping(userId, hash); } catch (Exception ex) { HandleError(ex); return null; } }
        private void HandleError(Exception ex) { System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}"); _isConnected = false; }
        public void Dispose()
        {
            lock (_proxyLock)
            {
                if (_proxy != null) { var channel = _proxy as ICommunicationObject; if (channel != null && channel.State == CommunicationState.Opened) channel.Close(); }
                if (_channelFactory != null) _channelFactory.Close();
            }
        }
    }
}