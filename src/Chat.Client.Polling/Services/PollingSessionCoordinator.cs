using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Threading;
using Chat.Client.Shared.Controls;
using Chat.Client.Shared.Services;
using Chat.Contracts.DataContracts;
using Chat.Contracts.SharedTypes;

namespace Chat.Client.Polling.Services
{
    public sealed class PollingSessionCoordinator : IDisposable
    {
        private static readonly Lazy<PollingSessionCoordinator> _instance = new Lazy<PollingSessionCoordinator>(() => new PollingSessionCoordinator());
        public static PollingSessionCoordinator Instance => _instance.Value;
        public event EventHandler<List<Chat.Contracts.DataContracts.Channel>> ChannelsUpdated;
        public event EventHandler<List<string>> ChannelMembersUpdated;
        public event EventHandler<List<SharedFile>> ChannelFilesUpdated;
        public event EventHandler<Message> PublicMessageReceived;
        public event EventHandler<(string OtherUserId, Message Message)> PrivateMessageReceived;
        public event EventHandler<(string OtherUserId, SharedFile File)> PrivateFileReceived;
        public event EventHandler<ConnectionState> ConnectionStateChanged;
        public event EventHandler<int> PingMsUpdated;
        private ChatServiceClient _serviceClient;
        private readonly ValidationService _validationService;
        private readonly FileHelperService _fileHelperService;
        private readonly Dispatcher _dispatcher;
        private readonly Timer _pollingTimer;
        private readonly Timer _pingTimer;
        private readonly Random _random = new Random();
        private int _pollingIntervalMs;
        private readonly object _pollingLock = new object();
        private string _currentUserId;
        private string _currentChannel;
        private bool _isPolling;
        private bool _isDisposed;
        public string CurrentUserId => _currentUserId;
        public string CurrentChannel => _currentChannel;
        public bool IsSignedIn => !string.IsNullOrEmpty(_currentUserId);
        public bool IsConnected => _serviceClient?.IsConnected ?? false;

        private PollingSessionCoordinator()
        {
            _dispatcher = Dispatcher.CurrentDispatcher;
            _pollingIntervalMs = new ConfigurationService().GetSettings().PollingIntervalMs;
            _validationService = new ValidationService();
            _fileHelperService = new FileHelperService();
            _pollingTimer = new Timer(OnPollingTimerElapsed, null, Timeout.Infinite, Timeout.Infinite);
            _pingTimer = new Timer(OnPingTimerElapsed, null, Timeout.Infinite, Timeout.Infinite);
        }

        public void StartSession(ChatServiceClient serviceClient)
        {
            _serviceClient = serviceClient ?? throw new ArgumentNullException(nameof(serviceClient));
            _pollingIntervalMs = new ConfigurationService().GetSettings().PollingIntervalMs;
            // Start the first ping through the ThreadPool timer, not the WPF Dispatcher.
            _pingTimer.Change(TimeSpan.Zero, TimeSpan.FromSeconds(5));
        }

        public void SetSignedInUser(string username)
        {
            _currentUserId = username;
            StopPolling();
        }

        public void SignOut()
        {
            StopPolling();
            if (_serviceClient == null) return;
            try
            {
                if (!string.IsNullOrEmpty(_currentChannel)) _serviceClient.LeaveChannel(_currentUserId);
                if (!string.IsNullOrEmpty(_currentUserId)) _serviceClient.SignOut(_currentUserId);
            }
            finally
            {
                _currentUserId = null;
                _currentChannel = null;
            }
        }

        public bool JoinChannel(string channelName)
        {
            if (!string.IsNullOrEmpty(_currentChannel))
            {
                StopPolling();
                _serviceClient.LeaveChannel(_currentUserId);
            }
            bool success = _serviceClient.JoinChannel(_currentUserId, channelName);
            if (success)
            {
                _currentChannel = channelName;
                StartPolling();
            }
            return success;
        }

        public void LeaveChannel()
        {
            StopPolling();
            _serviceClient.LeaveChannel(_currentUserId);
            _currentChannel = null;
        }

        public bool CreateChannel(string channelName) => _serviceClient.CreateChannel(channelName);

        public void RefreshChannels()
        {
            var channels = _serviceClient.GetChannels();
            RaiseOnUiThread(ChannelsUpdated, channels);
        }

        public void RefreshChannelMembers()
        {
            if (string.IsNullOrEmpty(_currentChannel)) return;
            var members = _serviceClient.GetChannelMembers(_currentChannel);
            RaiseOnUiThread(ChannelMembersUpdated, members);
        }

        public void RefreshChannelFiles()
        {
            if (string.IsNullOrEmpty(_currentChannel)) return;
            var files = _serviceClient.GetChannelFiles(_currentUserId, _currentChannel);
            RaiseOnUiThread(ChannelFilesUpdated, files);
        }

        public void SendPublicMessage(string content)
        {
            if (string.IsNullOrEmpty(_currentChannel)) return;
            _serviceClient.SendMessage(_currentUserId, _currentChannel, content);
        }

        public bool SendPrivateMessage(string recipientId, string content) => _serviceClient.SendPrivateMessage(_currentUserId, recipientId, content);
        public ValidationResult ValidateFile(string filePath) => _validationService.ValidateFile(filePath);
        public FileType DetermineFileType(string fileName) => _validationService.DetermineFileType(fileName);
        public string GetFileName(string filePath) => _fileHelperService.GetFileName(filePath);
        public byte[] ReadFile(string filePath) => _fileHelperService.ReadFile(filePath);
        public bool ShareFile(string fileName, FileType fileType, byte[] fileData) => _serviceClient.ShareFile(_currentUserId, _currentChannel, fileName, fileType, fileData);
        public SharedFile SharePrivateFile(string recipientId, string fileName, FileType fileType, byte[] fileData) => _serviceClient.SharePrivateFile(_currentUserId, recipientId, fileName, fileType, fileData);

        public bool DownloadAndOpenPrivateFile(SharedFile file)
        {
            var downloadedFile = _serviceClient.GetPrivateFile(_currentUserId, file.FileId);
            if (downloadedFile?.FileData == null) return false;
            string downloadsPath = _fileHelperService.GetDownloadsPath();
            if (!_fileHelperService.SaveFile(downloadedFile.FileData, file.FileName, downloadsPath)) return false;
            _fileHelperService.OpenFile(System.IO.Path.Combine(downloadsPath, file.FileName));
            return true;
        }

        public bool DownloadAndOpenFile(SharedFile file)
        {
            var downloadedFile = _serviceClient.GetFile(_currentUserId, file.FileId);
            if (downloadedFile?.FileData == null) return false;
            string downloadsPath = _fileHelperService.GetDownloadsPath();
            bool saved = _fileHelperService.SaveFile(downloadedFile.FileData, file.FileName, downloadsPath);
            if (!saved) return false;
            _fileHelperService.OpenFile(System.IO.Path.Combine(downloadsPath, file.FileName));
            return true;
        }

        private void StartPolling()
        {
            if (_isDisposed || !IsSignedIn || string.IsNullOrEmpty(_currentChannel)) return;
            _pollingTimer.Change(TimeSpan.Zero, TimeSpan.FromMilliseconds(_pollingIntervalMs));
        }

        private void StopPolling() => _pollingTimer.Change(Timeout.Infinite, Timeout.Infinite);

        private void OnPollingTimerElapsed(object state)
        {
            lock (_pollingLock)
            {
                if (_isPolling || !IsSignedIn || string.IsNullOrEmpty(_currentChannel) || _isDisposed) return;
                _isPolling = true;
            }
            try { PollOnce(); }
            catch (System.ServiceModel.CommunicationException) { RaiseConnectionState(ConnectionState.Disconnected); }
            catch (TimeoutException) { RaiseConnectionState(ConnectionState.Disconnected); }
            finally { lock (_pollingLock) { _isPolling = false; } }
        }

        private void PollOnce()
        {
            if (!IsSignedIn || _serviceClient == null || string.IsNullOrEmpty(_currentChannel)) return;
            // P0 UI-responsiveness invariant: WCF calls run on the ThreadPool timer.
            // UI-facing events are marshalled through Dispatcher.BeginInvoke.
            // TODO(P0): add a delayed-WCF behavioral test for Dispatcher responsiveness.
            RefreshChannelMembers();
            RefreshChannelFiles();
            PollPublicMessages();
            PollPrivateMessages();
            PollPrivateFiles();
        }

        private void PollPublicMessages()
        {
            var messages = _serviceClient.GetPendingMessages(_currentUserId);
            foreach (var message in messages)
            {
                message.IsCurrentUser = string.Equals(message.SenderId, _currentUserId, StringComparison.OrdinalIgnoreCase);
                RaiseOnUiThread(PublicMessageReceived, message);
            }
        }

        private void PollPrivateMessages()
        {
            var messages = _serviceClient.GetPendingPrivateMessages(_currentUserId);
            foreach (var message in messages)
            {
                string otherUserId = string.Equals(message.SenderId, _currentUserId, StringComparison.OrdinalIgnoreCase) ? message.RecipientId : message.SenderId;
                message.IsCurrentUser = string.Equals(message.SenderId, _currentUserId, StringComparison.OrdinalIgnoreCase);
                RaiseOnUiThread(PrivateMessageReceived, (otherUserId, message));
            }
        }

        private void PollPrivateFiles()
        {
            var files = _serviceClient.GetPendingPrivateFiles(_currentUserId);
            foreach (var file in files)
            {
                string otherUserId = string.Equals(file.UploaderId, _currentUserId, StringComparison.OrdinalIgnoreCase) ? file.RecipientId : file.UploaderId;
                RaiseOnUiThread(PrivateFileReceived, (otherUserId, file));
            }
        }

        private void OnPingTimerElapsed(object state) => PerformPing();

        private void PerformPing()
        {
            if (_serviceClient == null || _isDisposed) return;
            try
            {
                byte[] hash = new byte[6];
                lock (_random) { _random.NextBytes(hash); }
                string expectedPong = BitConverter.ToString(hash).Replace("-", "");
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                string pong = _serviceClient.Ping(_currentUserId ?? string.Empty, hash);
                stopwatch.Stop();
                if (pong == expectedPong)
                {
                    RaisePing(stopwatch.ElapsedMilliseconds);
                    RaiseConnectionState(ConnectionState.Connected);
                }
                else { RaisePing(0); RaiseConnectionState(ConnectionState.Disconnected); }
            }
            catch (System.ServiceModel.CommunicationException) { RaisePing(0); RaiseConnectionState(ConnectionState.Disconnected); }
            catch (TimeoutException) { RaisePing(0); RaiseConnectionState(ConnectionState.Disconnected); }
        }

        private void RaiseOnUiThread<T>(EventHandler<T> handler, T value)
        {
            if (handler == null) return;
            _dispatcher.BeginInvoke(new Action(() => handler(this, value)));
        }

        private void RaisePing(long milliseconds)
        {
            int ping = milliseconds > int.MaxValue ? int.MaxValue : (int)milliseconds;
            RaiseOnUiThread(PingMsUpdated, ping);
        }

        private void RaiseConnectionState(ConnectionState state) => RaiseOnUiThread(ConnectionStateChanged, state);

        public void Dispose()
        {
            if (_isDisposed) return;
            StopPolling();
            _pingTimer.Change(Timeout.Infinite, Timeout.Infinite);
            if (IsSignedIn) SignOut();
            _serviceClient?.Dispose();
            _serviceClient = null;
            _pollingTimer.Dispose();
            _pingTimer.Dispose();
            _isDisposed = true;
        }
    }
}