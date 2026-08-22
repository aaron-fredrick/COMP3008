using System;
using System.Collections.Generic;
using System.Windows.Threading;
using Chat.Client.Shared.Controls;
using Chat.Client.Shared.Services;
using Chat.Contracts.DataContracts;
using Chat.Contracts.SharedTypes;

namespace Chat.Client.Polling.Services
{
    /// <summary>
    /// Owns all session state, polling orchestration, and WCF service calls.
    /// Raises events that the MainWindow reacts to — keeping UI and logic separate.
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
        public event EventHandler<ConnectionState> ConnectionStateChanged;
        public event EventHandler<int> PingMsUpdated;

        private ChatServiceClient _serviceClient;
        private readonly ValidationService _validationService;
        private readonly FileHelperService _fileHelperService;
        private readonly DispatcherTimer _pollingTimer;
        private readonly DispatcherTimer _pingTimer;
        private readonly Random _random = new Random();
        private readonly int _pollingIntervalMs;

        private string _currentUserId;
        private string _currentChannel;
        private bool _isDisposed;

        public string CurrentUserId => _currentUserId;
        public string CurrentChannel => _currentChannel;
        public bool IsSignedIn => !string.IsNullOrEmpty(_currentUserId);
        public bool IsConnected => _serviceClient?.IsConnected ?? false;

        private PollingSessionCoordinator()
        {
            int pollingIntervalMs = int.Parse(System.Configuration.ConfigurationManager.AppSettings["PollingInterval"] ?? "2000");
            _pollingIntervalMs = pollingIntervalMs;
            _validationService = new ValidationService();
            _fileHelperService = new FileHelperService();

            _pollingTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(pollingIntervalMs) };
            _pollingTimer.Tick += OnPollingTick;

            _pingTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
            _pingTimer.Tick += OnPingTick;
        }

        // ── Session lifecycle ────────────────────────────────────────────────

        public void StartSession(ChatServiceClient serviceClient)
        {
            _serviceClient = serviceClient;
            _pingTimer.Start();
            PerformPing();
        }

        /// <summary>
        /// Records the already-authenticated user and starts the polling timer.
        /// The actual WCF SignIn call is made by SignInView before this is called.
        /// </summary>
        public void SetSignedInUser(string username)
        {
            _currentUserId = username;
            _pollingTimer.Start();
        }

        public void SignOut()
        {
            _pollingTimer.Stop();
            _pingTimer.Stop();

            if (!string.IsNullOrEmpty(_currentChannel))
                _serviceClient?.LeaveChannel(_currentUserId);

            _serviceClient?.SignOut(_currentUserId);
            _serviceClient?.Dispose();

            _currentUserId = null;
            _currentChannel = null;
        }

        // ── Channel operations ───────────────────────────────────────────────

        public bool JoinChannel(string channelName)
        {
            if (!string.IsNullOrEmpty(_currentChannel))
                _serviceClient.LeaveChannel(_currentUserId);

            bool success = _serviceClient.JoinChannel(_currentUserId, channelName);
            if (success)
                _currentChannel = channelName;

            return success;
        }

        public void LeaveChannel()
        {
            _serviceClient.LeaveChannel(_currentUserId);
            _currentChannel = null;
        }

        public bool CreateChannel(string channelName) =>
            _serviceClient.CreateChannel(channelName);

        public void RefreshChannels()
        {
            var channels = _serviceClient.GetChannels();
            ChannelsUpdated?.Invoke(this, channels);
        }

        public void RefreshChannelMembers()
        {
            if (string.IsNullOrEmpty(_currentChannel))
                return;

            var members = _serviceClient.GetChannelMembers(_currentChannel);
            ChannelMembersUpdated?.Invoke(this, members);
        }

        public void RefreshChannelFiles()
        {
            if (string.IsNullOrEmpty(_currentChannel))
                return;

            var files = _serviceClient.GetChannelFiles(_currentChannel);
            ChannelFilesUpdated?.Invoke(this, files);
        }

        // ── Messaging ────────────────────────────────────────────────────────

        public void SendPublicMessage(string content)
        {
            if (string.IsNullOrEmpty(_currentChannel))
                return;

            _serviceClient.SendMessage(_currentUserId, _currentChannel, content);
        }

        public void SendPrivateMessage(string recipientId, string content) =>
            _serviceClient.SendPrivateMessage(_currentUserId, recipientId, content);

        // ── File operations ──────────────────────────────────────────────────

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

        public bool DownloadAndOpenFile(SharedFile file)
        {
            var downloadedFile = _serviceClient.GetFile(file.FileId);
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

        // ── Polling ──────────────────────────────────────────────────────────

        private void OnPollingTick(object sender, EventArgs e)
        {
            if (!IsSignedIn)
                return;

            if (string.IsNullOrEmpty(_currentChannel))
            {
                RefreshChannels();
                return;
            }

            RefreshChannelMembers();
            RefreshChannelFiles();
            RefreshChannels();
            PollPublicMessages();
            PollPrivateMessages();
        }

        private void PollPublicMessages()
        {
            var messages = _serviceClient.GetPendingMessages(_currentUserId);
            foreach (var message in messages)
            {
                message.IsCurrentUser = string.Equals(message.SenderId, _currentUserId, StringComparison.OrdinalIgnoreCase);
                PublicMessageReceived?.Invoke(this, message);
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
                PrivateMessageReceived?.Invoke(this, (otherUserId, message));
            }
        }

        // ── Ping ─────────────────────────────────────────────────────────────

        private void OnPingTick(object sender, EventArgs e) => PerformPing();

        private async void PerformPing()
        {
            try
            {
                byte[] hash = new byte[6];
                _random.NextBytes(hash);
                string expectedPong = BitConverter.ToString(hash).Replace("-", "");

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                string pong = _serviceClient.Ping(_currentUserId ?? string.Empty, hash);
                stopwatch.Stop();

                if (pong == expectedPong)
                {
                    PingMsUpdated?.Invoke(this, (int)stopwatch.ElapsedMilliseconds);
                    ConnectionStateChanged?.Invoke(this, ConnectionState.Connected);
                }
                else
                {
                    PingMsUpdated?.Invoke(this, 0);
                    ConnectionStateChanged?.Invoke(this, ConnectionState.Disconnected);
                }
            }
            catch
            {
                PingMsUpdated?.Invoke(this, 0);
                ConnectionStateChanged?.Invoke(this, ConnectionState.Disconnected);
            }
        }

        // ── IDisposable ──────────────────────────────────────────────────────

        public void Dispose()
        {
            if (_isDisposed)
                return;

            if (IsSignedIn)
                SignOut();

            _pollingTimer.Stop();
            _pingTimer.Stop();
            _serviceClient?.Dispose();
            _isDisposed = true;
        }
    }
}
