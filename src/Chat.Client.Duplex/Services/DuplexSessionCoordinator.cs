using System;
using System.Collections.Generic;
using System.Windows.Threading;
using Chat.Client.Shared.Services;
using Chat.Contracts.DataContracts;
using Chat.Contracts.ServiceContracts;
using Chat.Contracts.SharedTypes;
using Chat.Client.Shared.Controls;
namespace Chat.Client.Duplex.Services
{
    public sealed class DuplexSessionCoordinator : IDisposable
    {
        private static readonly Lazy<DuplexSessionCoordinator> _instance = new Lazy<DuplexSessionCoordinator>(() => new DuplexSessionCoordinator());
        public static DuplexSessionCoordinator Instance => _instance.Value;
        public event EventHandler<List<Channel>> ChannelsUpdated; public event EventHandler<List<string>> ChannelMembersUpdated; public event EventHandler<List<SharedFile>> ChannelFilesUpdated; public event EventHandler<Message> PublicMessageReceived; public event EventHandler<(string OtherUserId, Message Message)> PrivateMessageReceived; public event EventHandler<(string OtherUserId, SharedFile File)> PrivateFileReceived; public event EventHandler<ConnectionState> ConnectionStateChanged; public event EventHandler<string> UserDisconnected;
        private DuplexServiceClient _serviceClient; private readonly ValidationService _validationService; private readonly FileHelperService _fileHelperService; private string _currentUserId; private string _currentChannel; private bool _isDisposed;

        // Local file list: populated on join (via RefreshChannelFilesAsync) and appended to by OnFileShared push.
        // Cleared on join/leave so the initial load always reflects the correct visibility boundary.
        private readonly List<SharedFile> _currentChannelFiles = new List<SharedFile>();

        public string CurrentUserId => _currentUserId; public string CurrentChannel => _currentChannel; public bool IsSignedIn => !string.IsNullOrEmpty(_currentUserId); public bool IsConnected => _serviceClient?.IsConnected ?? false;
        private DuplexSessionCoordinator() { _validationService = new ValidationService(); _fileHelperService = new FileHelperService(); }
        public void StartSession(Dispatcher dispatcher) { if (_serviceClient != null) { UnsubscribeEvents(); _serviceClient.Dispose(); } _serviceClient = new DuplexServiceClient(dispatcher); SubscribeEvents(); }
        private void SubscribeEvents() { _serviceClient.MessageReceived += OnMessageReceived; _serviceClient.PrivateMessageReceived += OnPrivateMessageReceived; _serviceClient.FileShared += OnFileShared; _serviceClient.PrivateFileShared += OnPrivateFileShared; _serviceClient.ChannelListChanged += OnChannelListChanged; _serviceClient.ChannelMembersChanged += OnChannelMembersChanged; _serviceClient.UserDisconnected += OnUserDisconnected; _serviceClient.ConnectionLost += OnConnectionLost; }
        private void UnsubscribeEvents() { if (_serviceClient != null) { _serviceClient.MessageReceived -= OnMessageReceived; _serviceClient.PrivateMessageReceived -= OnPrivateMessageReceived; _serviceClient.FileShared -= OnFileShared; _serviceClient.PrivateFileShared -= OnPrivateFileShared; _serviceClient.ChannelListChanged -= OnChannelListChanged; _serviceClient.ChannelMembersChanged -= OnChannelMembersChanged; _serviceClient.UserDisconnected -= OnUserDisconnected; _serviceClient.ConnectionLost -= OnConnectionLost; } }
        public async System.Threading.Tasks.Task<bool> SignInAsync(string username) { var result = _validationService.ValidateUsername(username); if (!result.IsValid) return false; ConnectionStateChanged?.Invoke(this, ConnectionState.Connecting); bool success = await System.Threading.Tasks.Task.Run(() => _serviceClient.SignIn(username)); if (success) { _currentUserId = username; ConnectionStateChanged?.Invoke(this, ConnectionState.Connected); } else { ConnectionStateChanged?.Invoke(this, ConnectionState.Disconnected); } return success; }
        public void SignOut() { if (!string.IsNullOrEmpty(_currentChannel)) _serviceClient?.LeaveChannel(_currentUserId); _serviceClient?.SignOut(_currentUserId); _currentUserId = null; _currentChannel = null; ConnectionStateChanged?.Invoke(this, ConnectionState.Disconnected); }
        public async System.Threading.Tasks.Task<bool> JoinChannelAsync(string channelName) { if (!string.IsNullOrEmpty(_currentChannel)) await System.Threading.Tasks.Task.Run(() => _serviceClient.LeaveChannel(_currentUserId)); _currentChannelFiles.Clear(); bool success = await System.Threading.Tasks.Task.Run(() => _serviceClient.JoinChannel(_currentUserId, channelName)); if (success) _currentChannel = channelName; return success; }
        public async System.Threading.Tasks.Task LeaveChannelAsync() { await System.Threading.Tasks.Task.Run(() => _serviceClient.LeaveChannel(_currentUserId)); _currentChannelFiles.Clear(); _currentChannel = null; }
        public async System.Threading.Tasks.Task<bool> CreateChannelAsync(string channelName) => await System.Threading.Tasks.Task.Run(() => _serviceClient.CreateChannel(channelName));

        // Initial-load operations — called by the view on first display, not from callbacks.
        public async System.Threading.Tasks.Task RefreshChannelsAsync() { var channels = await System.Threading.Tasks.Task.Run(() => _serviceClient.GetChannels()); ChannelsUpdated?.Invoke(this, channels); }
        public async System.Threading.Tasks.Task RefreshChannelMembersAsync() { if (string.IsNullOrEmpty(_currentChannel)) return; var members = await System.Threading.Tasks.Task.Run(() => _serviceClient.GetChannelMembers(_currentChannel)); ChannelMembersUpdated?.Invoke(this, members); }
        public async System.Threading.Tasks.Task RefreshChannelFilesAsync()
        {
            if (string.IsNullOrEmpty(_currentChannel)) return;
            var files = await System.Threading.Tasks.Task.Run(() => _serviceClient.GetChannelFiles(_currentUserId, _currentChannel));
            // Seed the local file list with the authoritative initial set so that
            // subsequent OnFileShared push notifications can append to it correctly.
            _currentChannelFiles.Clear();
            _currentChannelFiles.AddRange(files);
            ChannelFilesUpdated?.Invoke(this, new List<SharedFile>(files));
        }

        public void SendPublicMessage(string content) { if (string.IsNullOrEmpty(_currentChannel)) return; System.Threading.Tasks.Task.Run(() => _serviceClient.SendMessage(_currentUserId, _currentChannel, content)); }
        public bool SendPrivateMessage(string recipientId, string content) => _serviceClient.SendPrivateMessage(_currentUserId, recipientId, content);
        public ValidationResult ValidateFile(string filePath) => _validationService.ValidateFile(filePath); public FileType DetermineFileType(string fileName) => _validationService.DetermineFileType(fileName); public string GetFileName(string filePath) => _fileHelperService.GetFileName(filePath); public byte[] ReadFile(string filePath) => _fileHelperService.ReadFile(filePath);
        public bool ShareFile(string fileName, FileType fileType, byte[] fileData) => _serviceClient.ShareFile(_currentUserId, _currentChannel, fileName, fileType, fileData);
        public SharedFile SharePrivateFile(string recipientId, string fileName, FileType fileType, byte[] fileData) => _serviceClient.SharePrivateFile(_currentUserId, recipientId, fileName, fileType, fileData);
        public async System.Threading.Tasks.Task<bool> DownloadAndOpenPrivateFileAsync(SharedFile file)
        {
            var downloadService = new DownloadService();
            string downloadsPath = _fileHelperService.GetDownloadsPath();
            string fullPath = System.IO.Path.Combine(downloadsPath, file.FileName);
            
            System.ServiceModel.ChannelFactory<IChatService> factory = null;
            System.IO.Stream sourceStream = null;
            try
            {
                sourceStream = _serviceClient.DownloadPrivateFileStream(_currentUserId, file.FileId, out factory);
                if (sourceStream == null) return false;
                
                await downloadService.DownloadAsync(sourceStream, fullPath, file.FileSize, file.FileName);
                _fileHelperService.OpenFile(fullPath);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Streaming download failed: {ex.Message}");
                return false;
            }
            finally
            {
                if (sourceStream != null) { try { sourceStream.Dispose(); } catch { } }
                if (factory != null) { try { factory.Close(); } catch { } }
            }
        }
        
        public async System.Threading.Tasks.Task<bool> DownloadAndOpenFileAsync(SharedFile file)
        {
            var downloadService = new DownloadService();
            string downloadsPath = _fileHelperService.GetDownloadsPath();
            string fullPath = System.IO.Path.Combine(downloadsPath, file.FileName);
            
            System.ServiceModel.ChannelFactory<IChatService> factory = null;
            System.IO.Stream sourceStream = null;
            try
            {
                sourceStream = _serviceClient.DownloadFileStream(_currentUserId, file.FileId, out factory);
                if (sourceStream == null) return false;
                
                await downloadService.DownloadAsync(sourceStream, fullPath, file.FileSize, file.FileName);
                _fileHelperService.OpenFile(fullPath);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Streaming download failed: {ex.Message}");
                return false;
            }
            finally
            {
                if (sourceStream != null) { try { sourceStream.Dispose(); } catch { } }
                if (factory != null) { try { factory.Close(); } catch { } }
            }
        }
        public byte[] DownloadFileBytes(Guid fileId) { var downloadedFile = _serviceClient.GetFile(_currentUserId, fileId); return downloadedFile?.FileData; }

        // ── Callback handlers — payload consumed directly; no follow-up server requests ────────────

        private void OnMessageReceived(object sender, Message message) { if (message.ChannelName == _currentChannel) { message.IsCurrentUser = string.Equals(message.SenderId, _currentUserId, StringComparison.OrdinalIgnoreCase); PublicMessageReceived?.Invoke(this, message); } }
        private void OnPrivateMessageReceived(object sender, Message message) { string otherUserId = string.Equals(message.SenderId, _currentUserId, StringComparison.OrdinalIgnoreCase) ? message.RecipientId : message.SenderId; message.IsCurrentUser = string.Equals(message.SenderId, _currentUserId, StringComparison.OrdinalIgnoreCase); PrivateMessageReceived?.Invoke(this, (otherUserId, message)); }

        private void OnFileShared(object sender, SharedFile file)
        {
            // Server pushed the SharedFile metadata directly — no GetChannelFiles() needed.
            if (file.ChannelName != _currentChannel) return;
            // Avoid duplicates if the server sends the same file notification twice.
            if (!_currentChannelFiles.Exists(f => f.FileId == file.FileId))
                _currentChannelFiles.Add(file);
            ChannelFilesUpdated?.Invoke(this, new List<SharedFile>(_currentChannelFiles));
        }

        private void OnPrivateFileShared(object sender, SharedFile file) { string otherUserId = string.Equals(file.UploaderId, _currentUserId, StringComparison.OrdinalIgnoreCase) ? file.RecipientId : file.UploaderId; PrivateFileReceived?.Invoke(this, (otherUserId, file)); }

        private void OnChannelListChanged(object sender, List<Channel> channels)
        {
            // Server pushed the complete channel list — update UI directly.
            ChannelsUpdated?.Invoke(this, channels);
        }

        private void OnChannelMembersChanged(object sender, (string ChannelName, List<string> Members) args)
        {
            // Server pushed the updated member list — update UI directly.
            if (args.ChannelName == _currentChannel)
                ChannelMembersUpdated?.Invoke(this, args.Members);
        }

        private void OnUserDisconnected(object sender, string disconnectedUserId)
        {
            // The updated member list arrives separately via OnChannelMembersChanged — no pull needed.
            UserDisconnected?.Invoke(this, disconnectedUserId);
        }

        private void OnConnectionLost(object sender, EventArgs e) { ConnectionStateChanged?.Invoke(this, ConnectionState.Disconnected); _currentUserId = null; _currentChannel = null; }
        public void Dispose() { if (_isDisposed) return; if (IsSignedIn) SignOut(); UnsubscribeEvents(); _serviceClient?.Dispose(); _isDisposed = true; }
    }
}