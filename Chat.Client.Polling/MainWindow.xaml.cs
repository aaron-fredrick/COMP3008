using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Chat.Client.Polling.Services;
using Chat.Client.Polling.Views;
using Chat.Client.Shared.Services;
using Chat.Client.Shared.Controls;
using Chat.Contracts.DataContracts;
using Chat.Contracts.SharedTypes;

namespace Chat.Client.Polling
{
    public partial class MainWindow : Window
    {
        private ChatServiceClient _serviceClient;
        private DispatcherTimer _pollingTimer;
        private DispatcherTimer _pingTimer;
        private string _currentUserId;
        private string _currentChannel;
        private int _pollingInterval;
        private ChannelListView _channelListView;
        private ConversationView _conversationView;
        private System.Collections.Generic.Dictionary<string, PrivateMessageView> _privateMessageViews;
        private bool _isSigningOut = false;
        private ValidationService _validationService;
        private FileHelperService _fileHelperService;
        private Random _random = new Random();
        private WindowResizer _windowResizer;

        public MainWindow()
        {
            InitializeComponent();
            _pollingInterval = int.Parse(ConfigurationManager.AppSettings["PollingInterval"] ?? "2000");
            _privateMessageViews = new System.Collections.Generic.Dictionary<string, PrivateMessageView>();
            _validationService = new ValidationService();
            _fileHelperService = new FileHelperService();
            InitializePollingTimer();
            InitializePingTimer();
            InitializeFooter();
            _windowResizer = new WindowResizer(this);
            _serviceClient = new ChatServiceClient();
            StartPingTimer();
            var signInView = new SignInView();
            signInView.SetServiceClient(_serviceClient);
            signInView.SignInSuccess += SignInView_SignInSuccess;
            signInView.SignInFailed += SignInView_SignInFailed;
            MainContent.Content = signInView;
        }

        private void InitializeFooter()
        {
            AppFooter.SettingsClicked += AppFooter_SettingsClicked;
            UpdateFooterState();
        }

        private void AppFooter_SettingsClicked(object sender, EventArgs e)
        {
            MessageBox.Show("Settings view will be implemented in a future task.", "Settings", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void UpdateFooterState()
        {
            if (!string.IsNullOrEmpty(_currentUserId))
            {
                AppFooter.CurrentUser = _currentUserId;
                AppFooter.IsLoggedIn = true;
            }
            else
            {
                AppFooter.CurrentUser = string.Empty;
                AppFooter.IsLoggedIn = false;
            }

            if (_serviceClient != null && _serviceClient.IsConnected)
            {
                AppFooter.ConnectionStatus = Chat.Client.Shared.Controls.ConnectionState.Connected;
            }
            else
            {
                AppFooter.ConnectionStatus = Chat.Client.Shared.Controls.ConnectionState.Disconnected;
            }
        }

        private void InitializePollingTimer()
        {
            _pollingTimer = new DispatcherTimer();
            _pollingTimer.Interval = TimeSpan.FromMilliseconds(_pollingInterval);
            _pollingTimer.Tick += PollingTimer_Tick;
        }

        private void PollingTimer_Tick(object sender, EventArgs e)
        {
            if (_serviceClient != null && _currentUserId != null)
            {
                if (_currentChannel == null && _channelListView != null)
                {
                    var channels = _serviceClient.GetChannels();
                    _channelListView.UpdateChannels(channels);
                }
                else if (_currentChannel != null && _conversationView != null)
                {
                    LoadChannelMembers();
                    LoadChannelFiles();
                    LoadChannels();
                    PollForMessages();
                }

                var privateMessages = _serviceClient.GetPendingPrivateMessages(_currentUserId);
                foreach (var message in privateMessages)
                {
                    string otherUserId = (message.SenderId == _currentUserId) ? message.RecipientId : message.SenderId;

                    if (!_privateMessageViews.ContainsKey(otherUserId))
                    {
                        var privateMessageView = new PrivateMessageView(otherUserId);
                        privateMessageView.CurrentUserId = _currentUserId;
                        privateMessageView.SendMessageRequested += PrivateMessageView_SendMessageRequested;
                        privateMessageView.Closing += PrivateMessageView_Closing;
                        privateMessageView.Owner = this;
                        _privateMessageViews[otherUserId] = privateMessageView;
                        privateMessageView.Show();
                    }

                    _privateMessageViews[otherUserId].AddMessage(message);
                }
            }
        }

        private void InitializePingTimer()
        {
            _pingTimer = new DispatcherTimer();
            _pingTimer.Interval = TimeSpan.FromSeconds(5);
            _pingTimer.Tick += PingTimer_Tick;
        }

        private void StartPingTimer()
        {
            _pingTimer.Start();
            PerformPing();
        }

        private void PingTimer_Tick(object sender, EventArgs e)
        {
            if (_serviceClient != null)
            {
                PerformPing();
            }
        }

        private async void PerformPing()
        {
            try
            {
                byte[] hash = new byte[6];
                _random.NextBytes(hash);
                string hashStr = BitConverter.ToString(hash).Replace("-", "");

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                string pong = _serviceClient.Ping(_currentUserId ?? "", hash);
                stopwatch.Stop();
                if (pong == hashStr)
                {
                    int pingMs = (int)stopwatch.ElapsedMilliseconds;
                    AppFooter.ConnectionStatus = Chat.Client.Shared.Controls.ConnectionState.Connected;
                    AppFooter.PingMs = pingMs;
                    Console.WriteLine($"[PING] Success - Hash: {hashStr}, Round-trip: {pingMs}ms");
                }
                else
                {
                    AppFooter.PingMs = 0;
                    AppFooter.ConnectionStatus = Chat.Client.Shared.Controls.ConnectionState.Disconnected;
                    Console.WriteLine($"[PING] Checksum mismatch - Sent: {hashStr}, Received: {pong}");
                }
            }
            catch (Exception ex)
            {
                AppFooter.PingMs = 0;
                AppFooter.ConnectionStatus = Chat.Client.Shared.Controls.ConnectionState.Disconnected;
                Console.WriteLine($"[PING] Failed - {ex.Message}");
            }
        }

        private void SignInView_SignInSuccess(object sender, string username)
        {
            _currentUserId = username;
            UpdateFooterState();
            ShowChannelListView();
            _pollingTimer.Start();
        }

        private void SignInView_SignInFailed(object sender, string username)
        {
            AppFooter.ConnectionStatus = Chat.Client.Shared.Controls.ConnectionState.Disconnected;
        }

        private void ShowChannelListView()
        {
            _channelListView = new ChannelListView();
            _channelListView.SetServiceClient(_serviceClient);
            _channelListView.JoinChannelRequested += ChannelListView_JoinChannelRequested;
            _channelListView.CreateChannelRequested += ChannelListView_CreateChannelRequested;
            _channelListView.SignOutRequested += ChannelListView_SignOutRequested;
            MainContent.Content = _channelListView;
        }

        private void ShowConversationView(string channelName)
        {
            _currentChannel = channelName;
            _conversationView = new ConversationView();
            _conversationView.SetChannelName(channelName);
            _conversationView.SetCurrentUserId(_currentUserId);
            _conversationView.SendMessageRequested += ConversationView_SendMessageRequested;
            _conversationView.LeaveChannelRequested += ConversationView_LeaveChannelRequested;
            _conversationView.FileDownloadRequested += ConversationView_FileDownloadRequested;
            _conversationView.PrivateMessageRequested += ConversationView_PrivateMessageRequested;
            _conversationView.FileShareRequested += ConversationView_FileShareRequested;

            LoadChannelMembers();
            MainContent.Content = _conversationView;
        }

        private void ConversationView_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ConversationView_LeaveChannelRequested(sender, EventArgs.Empty);
        }

        private void ChannelListView_JoinChannelRequested(object sender, string channelName)
        {
            if (_currentChannel != null)
            {
                _serviceClient.LeaveChannel(_currentUserId);
            }

            bool success = _serviceClient.JoinChannel(_currentUserId, channelName);
            if (success)
            {
                _currentChannel = channelName;
                ShowConversationView(channelName);
            }
        }

        private void ChannelListView_CreateChannelRequested(object sender, string channelName)
        {
            bool success = _serviceClient.CreateChannel(channelName);
            if (success)
            {
                LoadChannels();
            }
            else
            {
                MessageBox.Show("Channel already exists or creation failed.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ChannelListView_SignOutRequested(object sender, EventArgs e)
        {
            SignOut();
        }

        private void ConversationView_SendMessageRequested(object sender, string message)
        {
            if (!string.IsNullOrEmpty(_currentChannel))
            {
                _serviceClient.SendMessage(_currentUserId, _currentChannel, message);
                var ownMessage = new Message
                {
                    SenderId = _currentUserId,
                    Content = message,
                    Timestamp = DateTime.UtcNow,
                    Type = MessageType.Public,
                    ChannelName = _currentChannel
                };
                _conversationView?.AddMessage(ownMessage);
            }
        }

        private void ConversationView_LeaveChannelRequested(object sender, EventArgs e)
        {
            _serviceClient.LeaveChannel(_currentUserId);
            _currentChannel = null;
            ShowChannelListView();
        }

        private void ConversationView_FileDownloadRequested(object sender, SharedFile file)
        {
            LogDebug($"[FILE DOWNLOAD] Requested: {file.FileName}");
            var downloadedFile = _serviceClient.GetFile(file.FileId);
            if (downloadedFile != null && downloadedFile.FileData != null)
            {
                string downloadsPath = _fileHelperService.GetDownloadsPath();
                bool saved = _fileHelperService.SaveFile(downloadedFile.FileData, file.FileName, downloadsPath);
                
                if (saved)
                {
                    string filePath = System.IO.Path.Combine(downloadsPath, file.FileName);
                    LogDebug($"[FILE DOWNLOAD] Saved to: {filePath}");
                    LogDebug($"[FILE DOWNLOAD] Opening file");
                    _fileHelperService.OpenFile(filePath);
                }
                else
                {
                    LogDebug($"[FILE DOWNLOAD] Failed to save file");
                }
            }
            else
            {
                LogDebug($"[FILE DOWNLOAD] Failed: file is null or has no data");
            }
        }

        private void ConversationView_FileShareRequested(object sender, EventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select a file to share",
                Filter = "All files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                string fileName = _fileHelperService.GetFileName(filePath);
                
                // Validate file using shared service
                var validationResult = _validationService.ValidateFile(filePath);
                if (!validationResult.IsValid)
                {
                    MessageBox.Show(validationResult.ErrorMessage, "File Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                byte[] fileData = _fileHelperService.ReadFile(filePath);
                FileType fileType = _validationService.DetermineFileType(fileName);

                LogDebug($"[FILE SHARE] File type: {fileType}, Uploading to channel: {_currentChannel}");

                bool success = _serviceClient.ShareFile(_currentUserId, _currentChannel, fileName, fileType, fileData);
                LogDebug($"[FILE SHARE] Upload result: {success}");

                if (success)
                {
                    LoadChannelFiles();

                    // Add file message to conversation view immediately
                    var fileMessage = new Message
                    {
                        SenderId = _currentUserId,
                        Content = $"Shared file: {fileName}",
                        Timestamp = DateTime.UtcNow,
                        Type = MessageType.File,
                        ChannelName = _currentChannel
                    };
                    _conversationView?.AddMessage(fileMessage);
                }
            }
        }

        private void LoadChannelFiles()
        {
            if (!string.IsNullOrEmpty(_currentChannel))
            {
                var files = _serviceClient.GetChannelFiles(_currentChannel);
                _conversationView?.UpdateFiles(files);
            }
        }

        private void ConversationView_PrivateMessageRequested(object sender, string recipientId)
        {
            if (recipientId == _currentUserId)
            {
                return; // Can't send private message to self
            }

            if (!_privateMessageViews.ContainsKey(recipientId))
            {
                var privateMessageView = new PrivateMessageView(recipientId);
                privateMessageView.CurrentUserId = _currentUserId;
                privateMessageView.SendMessageRequested += PrivateMessageView_SendMessageRequested;
                privateMessageView.Closing += PrivateMessageView_Closing;
                privateMessageView.Owner = this;
                _privateMessageViews[recipientId] = privateMessageView;
                privateMessageView.Show();
            }
            else
            {
                _privateMessageViews[recipientId].Focus();
            }
        }

        private void PrivateMessageView_SendMessageRequested(object sender, string message)
        {
            if (sender is PrivateMessageView privateMessageView)
            {
                string recipientId = privateMessageView.Title.Replace("Private Conversation with: ", "");
                _serviceClient.SendPrivateMessage(_currentUserId, recipientId, message);

                // Display own message immediately
                var ownMessage = new Message
                {
                    SenderId = _currentUserId,
                    Content = message,
                    Timestamp = DateTime.UtcNow,
                    Type = MessageType.Private,
                    RecipientId = recipientId
                };
                privateMessageView.AddMessage(ownMessage);
            }
        }

        private void PrivateMessageView_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (sender is PrivateMessageView privateMessageView)
            {
                string recipientId = privateMessageView.Title.Replace("Private Conversation with: ", "");
                _privateMessageViews.Remove(recipientId);
            }
        }

        private void LoadChannels()
        {
            if (!_isSigningOut)
            {
                var channels = _serviceClient.GetChannels();
                _channelListView?.UpdateChannels(channels);
            }
        }

        private void LoadChannelMembers()
        {
            if (!string.IsNullOrEmpty(_currentChannel))
            {
                var members = _serviceClient.GetChannelMembers(_currentChannel);
                _conversationView?.UpdateMembers(members);
            }
        }

        private void PollForMessages()
        {
            if (!string.IsNullOrEmpty(_currentChannel))
            {
                var messages = _serviceClient.GetPendingMessages(_currentUserId);
                LogDebug($"[POLLING] Received {messages.Count} public messages");
                foreach (var message in messages)
                {
                    LogDebug($"[POLLING] Message: Type={message.Type}, Sender={message.SenderId}, Content={message.Content}");
                    _conversationView?.AddMessage(message);
                }
            }
        }

        private void LogDebug(string message)
        {
            System.Diagnostics.Debug.WriteLine(message);
            Console.WriteLine(message);
            try
            {
                System.IO.File.AppendAllText("client_debug.log", $"{DateTime.Now:HH:mm:ss.fff} {message}\n");
            }
            catch { }
        }

        private void SignOut()
        {
            if (_isSigningOut)
            {
                return;
            }

            _isSigningOut = true;
            _pollingTimer.Stop();
            _pingTimer.Stop();

            if (!string.IsNullOrEmpty(_currentChannel))
            {
                _serviceClient?.LeaveChannel(_currentUserId);
            }

            _serviceClient?.SignOut(_currentUserId);
            _serviceClient?.Dispose();

            _currentUserId = null;
            _currentChannel = null;
            foreach (var privateMessageView in _privateMessageViews.Values)
            {
                privateMessageView.Close();
            }
            _privateMessageViews.Clear();

            _channelListView = null;
            _conversationView = null;

            this.Show();
            UpdateFooterState();

            _serviceClient = new ChatServiceClient();

            MainContent.Content = new SignInView();
            var signInView = MainContent.Content as SignInView;
            signInView?.SetServiceClient(_serviceClient);
            signInView.SignInSuccess += SignInView_SignInSuccess;
            signInView.SignInFailed += SignInView_SignInFailed;
            StartPingTimer();

            _isSigningOut = false;
        }

        protected override void OnClosed(EventArgs e)
        {
            _pollingTimer?.Stop();
            _pingTimer?.Stop();
            _serviceClient?.SignOut(_currentUserId);
            _serviceClient?.Dispose();
            _windowResizer?.Dispose();
            
            Application.Current.Shutdown();
            base.OnClosed(e);
        }
    }
}
