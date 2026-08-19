using System;
using System.Configuration;
using System.Windows;
using System.Windows.Threading;
using Chat.Client.Polling.Services;
using Chat.Client.Polling.Views;
using Chat.Contracts.DataContracts;

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

        public MainWindow()
        {
            InitializeComponent();
            _pollingInterval = int.Parse(ConfigurationManager.AppSettings["PollingInterval"] ?? "2000");
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
            _conversationView.SignOutRequested += ConversationView_SignOutRequested;
            _conversationView.FileDownloadRequested += ConversationView_FileDownloadRequested;
            
            LoadChannelMembers();
            _conversationView.Show();
            _channelListView.Hide();
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
            _conversationView.CloseWindow();
            _channelListView.Show();
            LoadChannels();
        }

        private void ConversationView_SignOutRequested(object sender, EventArgs e)
        {
            SignOut();
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

        private void LoadChannels()
        {
            var channels = _serviceClient.GetChannels();
            _channelListView?.UpdateChannels(channels);
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
                    _conversationView?.AddMessage(message);
                }

                LoadChannelMembers();
                LoadChannels();
            }
        }

        private void SignOut()
        {
            _pollingTimer.Stop();
            _serviceClient?.SignOut(_currentUserId);
            _serviceClient?.Dispose();
            
            _currentUserId = null;
            _currentChannel = null;
            
            _channelListView?.Close();
            _conversationView?.Close();
            
            this.Show();
            LoginStatusText.Text = "";
            UsernameTextBox.Text = "";
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
