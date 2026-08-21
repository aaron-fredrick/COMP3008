using System;
using System.Configuration;
using System.Windows;
using System.Windows.Threading;
using Chat.Client.Duplex.Services;
using Chat.Client.Duplex.Views;
using Chat.Client.Shared.Services;
using Chat.Contracts.DataContracts;
using Chat.Contracts.SharedTypes;

namespace Chat.Client.Duplex
{
    public partial class MainWindow : Window
    {
        private DuplexServiceClient _serviceClient;
        private string _currentUserId;
        private string _currentChannel;
        private ChannelListView _channelListView;
        private ConversationView _conversationView;
        private System.Collections.Generic.Dictionary<string, PrivateMessageView> _privateMessageViews;
        private bool _isSigningOut = false;
        private bool _channelListClosing = false;
        private bool _conversationClosing = false;
        private ValidationService _validationService;
        private FileHelperService _fileHelperService;

        public MainWindow()
        {
            InitializeComponent();
            _privateMessageViews = new System.Collections.Generic.Dictionary<string, PrivateMessageView>();
            _validationService = new ValidationService();
            _fileHelperService = new FileHelperService();
            InitializeFooter();
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

        private void SignInButton_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameTextBox.Text.Trim();
            
            var validationResult = _validationService.ValidateUsername(username);
            if (!validationResult.IsValid)
            {
                LoginStatusText.Text = validationResult.ErrorMessage;
                return;
            }

            LoginStatusText.Text = "Connecting to server...";
            AppFooter.ConnectionStatus = Chat.Client.Shared.Controls.ConnectionState.Connecting;
            SignInButton.IsEnabled = false;

            try
            {
                _serviceClient = new DuplexServiceClient(Dispatcher);
                _serviceClient.MessageReceived += ServiceClient_MessageReceived;
                _serviceClient.PrivateMessageReceived += ServiceClient_PrivateMessageReceived;
                _serviceClient.FileShared += ServiceClient_FileShared;
                _serviceClient.ChannelListChanged += ServiceClient_ChannelListChanged;
                _serviceClient.ChannelMembersChanged += ServiceClient_ChannelMembersChanged;
                _serviceClient.UserDisconnected += ServiceClient_UserDisconnected;
                _serviceClient.ConnectionLost += ServiceClient_ConnectionLost;

                bool success = _serviceClient.SignIn(username);

                if (success)
                {
                    _currentUserId = username;
                    UpdateFooterState();
                    ShowChannelListView();
                }
                else
                {
                    LoginStatusText.Text = "Sign in failed. Username may already be in use.";
                    AppFooter.ConnectionStatus = Chat.Client.Shared.Controls.ConnectionState.Disconnected;
                    SignInButton.IsEnabled = true;
                }
            }
            catch (Exception)
            {
                LoginStatusText.Text = "Server not responding. Please check if the server is running.";
                AppFooter.ConnectionStatus = Chat.Client.Shared.Controls.ConnectionState.Disconnected;
                SignInButton.IsEnabled = true;
            }
        }

        private void UsernameTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                SignInButton_Click(sender, e);
            }
        }

        private void ShowChannelListView()
        {
            _channelListView = new ChannelListView();
            _channelListView.SetWelcomeText(_currentUserId);
            _channelListView.SetConnectionStatus(_serviceClient.IsConnected);
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
            _conversationView.SetCurrentUserId(_currentUserId);
            _conversationView.SendMessageRequested += ConversationView_SendMessageRequested;
            _conversationView.LeaveChannelRequested += ConversationView_LeaveChannelRequested;
            _conversationView.FileDownloadRequested += ConversationView_FileDownloadRequested;
            _conversationView.PrivateMessageRequested += ConversationView_PrivateMessageRequested;
            _conversationView.FileShareRequested += ConversationView_FileShareRequested;
            _conversationView.Closing += ConversationView_Closing;

            LoadChannelMembers();
            LoadChannelFiles();
            _conversationView.Show();
            _channelListView.Hide();
        }

        private void ChannelListView_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_isSigningOut)
            {
                return;
            }
            _channelListClosing = true;
            SignOut();
        }

        private void ConversationView_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_isSigningOut)
            {
                return;
            }
            _conversationClosing = true;
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
            var downloadedFile = _serviceClient.GetFile(file.FileId);
            if (downloadedFile != null && downloadedFile.FileData != null)
            {
                string downloadsPath = _fileHelperService.GetDownloadsPath();
                bool saved = _fileHelperService.SaveFile(downloadedFile.FileData, file.FileName, downloadsPath);
                
                if (saved)
                {
                    string filePath = System.IO.Path.Combine(downloadsPath, file.FileName);
                    _fileHelperService.OpenFile(filePath);
                }
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
                
                var validationResult = _validationService.ValidateFile(filePath);
                if (!validationResult.IsValid)
                {
                    MessageBox.Show(validationResult.ErrorMessage, "File Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                byte[] fileData = _fileHelperService.ReadFile(filePath);
                FileType fileType = _validationService.DetermineFileType(fileName);

                bool success = _serviceClient.ShareFile(_currentUserId, _currentChannel, fileName, fileType, fileData);

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

        private void ConversationView_PrivateMessageRequested(object sender, string recipientId)
        {
            if (recipientId == _currentUserId)
            {
                return;
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
                _serviceClient.SendPrivateMessage(_currentUserId, privateMessageView.RecipientId, message);
            }
        }

        private void PrivateMessageView_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (sender is PrivateMessageView privateMessageView)
            {
                _privateMessageViews.Remove(privateMessageView.RecipientId);
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

        private void ServiceClient_MessageReceived(object sender, Message message)
        {
            if (message.ChannelName == _currentChannel)
            {
                _conversationView?.AddMessage(message);
            }
        }

        private void ServiceClient_PrivateMessageReceived(object sender, Message message)
        {
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

        private void ServiceClient_FileShared(object sender, SharedFile file)
        {
            if (file.ChannelName == _currentChannel)
            {
                LoadChannelFiles();
            }
        }

        private void ServiceClient_ChannelListChanged(object sender, EventArgs e)
        {
            LoadChannels();
        }

        private void ServiceClient_ChannelMembersChanged(object sender, string channelName)
        {
            if (channelName == _currentChannel)
            {
                LoadChannelMembers();
            }
        }

        private void ServiceClient_UserDisconnected(object sender, string disconnectedUserId)
        {
            LoadChannelMembers();
            _conversationView?.AddSystemMessage($"{disconnectedUserId} has left the channel.");
        }

        private void ServiceClient_ConnectionLost(object sender, EventArgs e)
        {
            MessageBox.Show("Connection to the server was lost. You have been signed out.", "Connection Lost", MessageBoxButton.OK, MessageBoxImage.Error);
            SignOut();
        }

        private void SignOut()
        {
            if (_isSigningOut)
            {
                return;
            }

            _isSigningOut = true;

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

            if (!_channelListClosing)
            {
                _channelListView?.Close();
            }
            if (!_conversationClosing)
            {
                _conversationView?.Close();
            }

            this.Show();
            LoginStatusText.Text = "";
            UsernameTextBox.Text = "";
            UpdateFooterState();
            SignInButton.IsEnabled = true;

            _channelListClosing = false;
            _conversationClosing = false;
            _isSigningOut = false;
        }

        protected override void OnClosed(EventArgs e)
        {
            _serviceClient?.SignOut(_currentUserId);
            _serviceClient?.Dispose();
            
            _channelListView?.Close();
            _conversationView?.Close();
            
            Application.Current.Shutdown();
            base.OnClosed(e);
        }
    }
}

