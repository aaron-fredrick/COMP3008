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
    /// <summary>
    /// Owns session state, background polling orchestration, and WCF service calls.
    /// Message polling is scoped to the currently active channel view; the WCF
    /// transport and ping lifecycle are deliberately independent from that view.
    /// UI-facing events are marshalled back to the WPF Dispatcher.
    /// </summary>
    public sealed class PollingSessionCoordinator : IDisposable
    {
        private static readonly Lazy<PollingSessionCoordinator> _instance =
            new Lazy<PollingSessionCoordinator>(() => new PollingSessionCoordinator());

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
        private readonly int _pollingIntervalMs;
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
            int pollingIntervalMs = int.Parse(System.Configuration.ConfigurationManager.AppSettings["PollingInterval"] ?? "2000");
            _pollingIntervalMs = pollingIntervalMs;
            _validationService = new ValidationService();
            _fileHelperService = new FileHelperService();

            _pollingTimer = new Timer(OnPollingTimerElapsed, null, Timeout.Infinite, Timeout.Infinite);
            _pingTimer = new Timer(OnPingTimerElapsed, null, Timeout.Infinite, Timeout.Infinite);
        }

        // Session lifecycle

        public void StartSession(ChatServiceClient serviceClient)
        {
            _serviceClient = serviceClient ?? throw new ArgumentNullException(nameof(serviceClient));
            // Do not perform the first Ping synchronously here. StartSession is
            // normally called from the WPF Dispatcher and Ping is a blocking WCF
            // operation. The timer callback runs on a ThreadPool thread instead.
            _pingTimer.Change(TimeSpan.Zero, TimeSpan.FromSeconds(5));
        }

        /// <summary>
        /// Records the already-authenticated user. Message polling starts only
        /// after a channel is joined and its conversation view becomes active.
        /// The ping timer is independent and remains available for session/transport health.
        /// </summary>
        public void SetSignedInUser(string username)
        {
            _currentUserId = username;
            StopPolling();
        }

        public void SignOut()
        {
            // Message polling is view-scoped, so it stops on sign-out.
            // Keep the WCF transport/ping lifecycle alive: signing out does not
            // mean the client must be disposed immediately.
            StopPolling();

            if (_serviceClient == null)
                return;

            try
            {
                if (!string.IsNullOrEmpty(_currentChannel))
                    _serviceClient.LeaveChannel(_currentUserId);

                if (!string.IsNullOrEmpty(_currentUserId))
                    _serviceClient.SignOut(_currentUserId);
            }
            finally
            {
                _currentUserId = null;
                _currentChannel = null;
                // Deliberately do not dispose/null _serviceClient here.
                // The ping lifecycle may continue while signed out.
            }
        }

        // Channel operations

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

        public bool CreateChannel(string channelName) =>
            _serviceClient.CreateChannel(channelName);

        public void RefreshChannels()
        {
            var channels = _serviceClient.GetChannels();
            RaiseOnUiThread(ChannelsUpdated, channels);
        }

        public void RefreshChannelMembers()
        {
            if (string.IsNullOrEmpty(_currentChannel))
                return;

            var members = _serviceClient.GetChannelMembers(_currentChannel);
            RaiseOnUiThread(ChannelMembersUpdated, members);
        }

        public void RefreshChannelFiles()
        {
            if (string.IsNullOrEmpty(_currentChannel))
                return;

            var files = _serviceClient.GetChannelFiles(_currentChannel);
            RaiseOnUiThread(ChannelFilesUpdated, files);
        }

        // Messaging

        public void SendPublicMessage(string content)
        {
            if (string.IsNullOrEmpty(_currentChannel))
                return;

            _serviceClient.SendMessage(_currentUserId, _currentChannel, content);
        }

        public bool SendPrivateMessage(string recipientId, string content) =>
            _serviceClient.SendPrivateMessage(_currentUserId, recipientId, content);

        // File operations

        public ValidationResult ValidateFile(string filePath) =>
            _validationService.ValidateFile(filePath);

        public FileType DetermineFileType(string fileName) =>
            _validationService.DetermineFileType(fileName);

        public string GetFileName(string filePath) =>
            _fileHelperService.GetFileName(filePath);

        public byte[] ReadFile(string filePath) =>
            _fileHelperService.ReadFile(filePath);

        public bool ShareFile(string fileName, FileType fileType, byte[] fileData) =>
            _serviceClient.ShareFile(_currentUserId, _currentChannel, fileName, fileType, fileData);

        public SharedFile SharePrivateFile(string recipientId, string fileName, FileType fileType, byte[] fileData) =>
            _serviceClient.SharePrivateFile(_currentUserId, recipientId, fileName, fileType, fileData);

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
            if (downloadedFile?.FileData == null)
                return false;

            string downloadsPath = _fileHelperService.GetDownloadsPath();
            bool saved = _fileHelperService.SaveFile(downloadedFile.FileData, file.FileName, downloadsPath);
            if (!saved)
                return false;

            string filePath = System.IO.Path.Combine(downloadsPath, file.FileName);
            _fileHelperService.OpenFile(filePath);
            return true;
        }

        // Polling

        private void StartPolling()
        {
            if (_isDisposed || !IsSignedIn || string.IsNullOrEmpty(_currentChannel))
                return;

            _pollingTimer.Change(TimeSpan.Zero, TimeSpan.FromMilliseconds(_pollingIntervalMs));
        }

        private void StopPolling()
        {
            _pollingTimer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        private void OnPollingTimerElapsed(object state)
        {
            lock (_pollingLock)
            {
                if (_isPolling || !IsSignedIn || string.IsNullOrEmpty(_currentChannel) || _isDisposed)
                    return;

                _isPolling = true;
            }

            try
            {
                PollOnce();
            }
            catch (System.ServiceModel.CommunicationException)
            {
                RaiseConnectionState(ConnectionState.Disconnected);
            }
            catch (TimeoutException)
            {
                RaiseConnectionState(ConnectionState.Disconnected);
            }
            finally
            {
                lock (_pollingLock)
                {
                    _isPolling = false;
                }
            }
        }

        private void PollOnce()
        {
            if (!IsSignedIn || _serviceClient == null || string.IsNullOrEmpty(_currentChannel))
                return;

            // P0 UI-responsiveness invariant: these WCF calls execute on the
            // ThreadPool timer callback, never on the WPF Dispatcher. UI events
            // are marshalled with BeginInvoke below.
            // TODO(P0): add a client-side behavioral test using a deliberately
            // delayed WCF response and assert the Dispatcher remains responsive.
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
                string otherUserId = string.Equals(message.SenderId, _currentUserId, StringComparison.OrdinalIgnoreCase)
                    ? message.RecipientId
                    : message.SenderId;

                message.IsCurrentUser = string.Equals(message.SenderId, _currentUserId, StringComparison.OrdinalIgnoreCase);
                RaiseOnUiThread(PrivateMessageReceived, (otherUserId, message));
            }
        }

        private void PollPrivateFiles()
        {
            var files = _serviceClient.GetPendingPrivateFiles(_currentUserId);
            foreach (var file in files)
            {
                string otherUserId = string.Equals(file.UploaderId, _currentUserId, StringComparison.OrdinalIgnoreCase)
                    ? file.RecipientId : file.UploaderId;
                RaiseOnUiThread(PrivateFileReceived, (otherUserId, file));
            }
        }

        // Ping

        private void OnPingTimerElapsed(object state) => PerformPing();

        private void PerformPing()
        {
            if (_serviceClient == null || _isDisposed)
                return;

            try
            {
                byte[] hash = new byte[6];
                lock (_random)
                {
                    _random.NextBytes(hash);
                }

                string expectedPong = BitConverter.ToString(hash).Replace("-", "");
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                string pong = _serviceClient.Ping(_currentUserId ?? string.Empty, hash);
                stopwatch.Stop();

                if (pong == expectedPong)
                {
                    RaisePing(stopwatch.ElapsedMilliseconds);
                    RaiseConnectionState(ConnectionState.Connected);
                }
                else
                {
                    RaisePing(0);
                    RaiseConnectionState(ConnectionState.Disconnected);
                }
            }
            catch (System.ServiceModel.CommunicationException)
            {
                RaisePing(0);
                RaiseConnectionState(ConnectionState.Disconnected);
            }
            catch (TimeoutException)
            {
                RaisePing(0);
                RaiseConnectionState(ConnectionState.Disconnected);
            }
        }

        private void RaiseOnUiThread<T>(EventHandler<T> handler, T value)
        {
            if (handler == null)
                return;

            _dispatcher.BeginInvoke(new Action(() => handler(this, value)));
        }

        private void RaisePing(long milliseconds)
        {
            int ping = milliseconds > int.MaxValue ? int.MaxValue : (int)milliseconds;
            RaiseOnUiThread(PingMsUpdated, ping);
        }

        private void RaiseConnectionState(ConnectionState state)
        {
            RaiseOnUiThread(ConnectionStateChanged, state);
        }

        // IDisposable

        public void Dispose()
        {
            if (_isDisposed)
                return;

            StopPolling();
            _pingTimer.Change(Timeout.Infinite, Timeout.Infinite);

            if (IsSignedIn)
                SignOut();

            _serviceClient?.Dispose();
            _serviceClient = null;

            _pollingTimer.Dispose();
            _pingTimer.Dispose();
            _isDisposed = true;
        }
    }
}