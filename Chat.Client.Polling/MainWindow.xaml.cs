using System;
using System.Configuration;
using System.Windows;
using System.Windows.Threading;
using Chat.Client.Polling.Services;
using Chat.Client.Polling.Views;
using Chat.Contracts.DataContracts;
using Chat.Contracts.SharedTypes;

namespace Chat.Client.Polling
{
    public partial class MainWindow : Window
    {
        private ChatServiceClient _serviceClient;
        private DispatcherTimer _pollingTimer;
        private string _currentUserId;
        private string _currentChannel;
        private int _pollingInterval;
        private ChannelListView _channelListView;
        private ConversationView _conversationView;
        private System.Collections.Generic.Dictionary<string, PrivateMessageView> _privateMessageViews;
        private bool _isSigningOut = false;

        public MainWindow()
        {
            InitializeComponent();
            _pollingInterval = int.Parse(ConfigurationManager.AppSettings["PollingInterval"] ?? "2000");
            _privateMessageViews = new System.Collections.Generic.Dictionary<string, PrivateMessageView>();
            InitializePollingTimer();
        }

        private void InitializePollingTimer()
        {
            _pollingTimer = new DispatcherTimer();
            _pollingTimer.Interval = TimeSpan.FromMilliseconds(_pollingInterval);
            _pollingTimer.Tick += PollingTimer_Tick;
        }

        private void SignInButton_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameTextBox.Text.Trim();
            if (string.IsNullOrEmpty(username))
            {
                LoginStatusText.Text = "Please enter a username";
                return;
            }

            _serviceClient = new ChatServiceClient();
            bool success = _serviceClient.SignIn(username);

            if (success)
            {
                _currentUserId = username;
                ShowChannelListView();
                _pollingTimer.Start();
            }
            else
            {
                LoginStatusText.Text = "Sign in failed. Username may already be in use.";
            }
        }

        private void ShowChannelListView()
        {
            _channelListView = new ChannelListView();
            _channelListView.SetWelcomeText(_currentUserId);
            _channelListView.JoinChannelRequested += ChannelListView_JoinChannelRequested;
            _channelListView.CreateChannelRequested += ChannelListView_CreateChannelRequested;
            _channelListView.SignOutRequested += ChannelListView_SignOutRequested;
            _channelListView.Closing += ChannelListView_Closing;
            
            LoadChannels();
            _channelListView.Show();
            this.Hide();
        }

        private void ShowConversationView(string channelName)
        {
            _conversationView = new ConversationView();
            _conversationView.SetChannelName(channelName);
            _conversationView.SendMessageRequested += ConversationView_SendMessageRequested;
            _conversationView.LeaveChannelRequested += ConversationView_LeaveChannelRequested;
            _conversationView.FileDownloadRequested += ConversationView_FileDownloadRequested;
            _conversationView.PrivateMessageRequested += ConversationView_PrivateMessageRequested;
            _conversationView.FileShareRequested += ConversationView_FileShareRequested;
            _conversationView.Closing += ConversationView_Closing;

            LoadChannelMembers();
            _conversationView.Show();
            _channelListView.Hide();
        }

        private void ChannelListView_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Prevent re-entrancy during sign out
            if (_isSigningOut)
            {
                return;
            }
            // User clicked X on channel list - sign out
            SignOut();
        }

        private void ConversationView_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Prevent re-entrancy during sign out
            if (_isSigningOut)
            {
                return;
            }
            // User clicked X on conversation - sign out
            SignOut();
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
                
                // Display own message immediately
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
            _conversationView.Close();
            _channelListView.Show();
            LoadChannels();
        }

        private void ConversationView_FileDownloadRequested(object sender, SharedFile file)
        {
            var downloadedFile = _serviceClient.GetFile(_currentChannel, file.FileName);
            if (downloadedFile != null && downloadedFile.FileData != null)
            {
                System.IO.File.WriteAllBytes(file.FileName, downloadedFile.FileData);
                System.Diagnostics.Process.Start(file.FileName);
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
                string fileName = System.IO.Path.GetFileName(filePath);
                byte[] fileData = System.IO.File.ReadAllBytes(filePath);

                System.Diagnostics.Debug.WriteLine($"[FILE SHARE] Selected file: {fileName}, Size: {fileData.Length} bytes");

                // Check file size limit (2MB)
                const long maxFileSize = 2 * 1024 * 1024; // 2MB in bytes
                if (fileData.Length > maxFileSize)
                {
                    System.Diagnostics.Debug.WriteLine($"[FILE SHARE] File too large: {fileData.Length} bytes");
                    MessageBox.Show($"File size exceeds 2MB limit. Current size: {FormatFileSize(fileData.Length)}", "File Too Large", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Determine file type
                FileType fileType = FileType.Unsupported;
                string extension = System.IO.Path.GetExtension(fileName).ToLower();
                if (extension == ".jpg")
                {
                    fileType = FileType.Jpg;
                }
                else if (extension == ".jpeg")
                {
                    fileType = FileType.Jpeg;
                }
                else if (extension == ".png")
                {
                    fileType = FileType.Png;
                }
                else if (extension == ".gif")
                {
                    fileType = FileType.Gif;
                }
                else if (extension == ".bmp")
                {
                    fileType = FileType.Bmp;
                }
                else if (extension == ".txt")
                {
                    fileType = FileType.Txt;
                }

                System.Diagnostics.Debug.WriteLine($"[FILE SHARE] File type: {fileType}, Uploading to channel: {_currentChannel}");

                bool success = _serviceClient.ShareFile(_currentUserId, _currentChannel, fileName, fileType, fileData);
                System.Diagnostics.Debug.WriteLine($"[FILE SHARE] Upload result: {success}");

                if (success)
                {
                    LoadChannelFiles();
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

        private string FormatFileSize(long bytes)
        {
            if (bytes < 1024)
            {
                return $"{bytes} B";
            }
            else if (bytes < 1024 * 1024)
            {
                double kb = bytes / 1024.0;
                return $"{Math.Round(kb)} kB";
            }
            else
            {
                double mb = bytes / (1024.0 * 1024.0);
                return $"{Math.Round(mb)} MB";
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
                privateMessageView.SendMessageRequested += PrivateMessageView_SendMessageRequested;
                privateMessageView.Closing += PrivateMessageView_Closing;
                privateMessageView.Owner = _conversationView;
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

        private void PollingTimer_Tick(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(_currentUserId))
            {
                var messages = _serviceClient.GetPendingMessages(_currentUserId);
                foreach (var message in messages)
                {
                    _conversationView?.AddMessage(message);
                }

                var privateMessages = _serviceClient.GetPendingPrivateMessages(_currentUserId);
                foreach (var message in privateMessages)
                {
                    // Determine which private message view should receive this
                    string otherUserId = (message.SenderId == _currentUserId) ? message.RecipientId : message.SenderId;

                    if (!_privateMessageViews.ContainsKey(otherUserId))
                    {
                        var privateMessageView = new PrivateMessageView(otherUserId);
                        privateMessageView.SendMessageRequested += PrivateMessageView_SendMessageRequested;
                        privateMessageView.Closing += PrivateMessageView_Closing;
                        privateMessageView.Owner = _conversationView;
                        _privateMessageViews[otherUserId] = privateMessageView;
                        privateMessageView.Show();
                    }

                    _privateMessageViews[otherUserId].AddMessage(message);
                }

                LoadChannelMembers();
                LoadChannelFiles();
                LoadChannels();
            }
        }

        private void SignOut()
        {
            if (_isSigningOut)
            {
                return;
            }

            _isSigningOut = true;
            _pollingTimer.Stop();

            // Leave channel if in one
            if (!string.IsNullOrEmpty(_currentChannel))
            {
                _serviceClient?.LeaveChannel(_currentUserId);
            }

            // Sign out from server
            _serviceClient?.SignOut(_currentUserId);
            _serviceClient?.Dispose();

            _currentUserId = null;
            _currentChannel = null;

            // Close all private message views
            foreach (var privateMessageView in _privateMessageViews.Values)
            {
                privateMessageView.Close();
            }
            _privateMessageViews.Clear();

            _channelListView?.Close();
            _conversationView?.Close();

            this.Show();
            LoginStatusText.Text = "";
            UsernameTextBox.Text = "";

            _isSigningOut = false;
        }

        protected override void OnClosed(EventArgs e)
        {
            _pollingTimer?.Stop();
            _serviceClient?.SignOut(_currentUserId);
            _serviceClient?.Dispose();
            
            _channelListView?.Close();
            _conversationView?.Close();
            
            Application.Current.Shutdown();
            base.OnClosed(e);
        }
    }
}
