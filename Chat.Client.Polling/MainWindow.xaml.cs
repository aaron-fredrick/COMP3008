using System;
using System.Configuration;
using System.Windows;
using System.Windows.Threading;
using Chat.Client.Polling.Services;
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
                LoginPanel.Visibility = Visibility.Collapsed;
                MainChatPanel.Visibility = Visibility.Visible;
                LoadChannels();
                _pollingTimer.Start();
            }
            else
            {
                LoginStatusText.Text = "Sign in failed. Username may already be in use.";
            }
        }

        private void SignOutButton_Click(object sender, RoutedEventArgs e)
        {
            _pollingTimer.Stop();
            _serviceClient?.SignOut(_currentUserId);
            _serviceClient?.Dispose();
            
            _currentUserId = null;
            _currentChannel = null;
            
            MainChatPanel.Visibility = Visibility.Collapsed;
            LoginPanel.Visibility = Visibility.Visible;
            LoginStatusText.Text = "";
            UsernameTextBox.Text = "";
        }

        private void LoadChannels()
        {
            var channels = _serviceClient.GetChannels();
            ChannelsListBox.ItemsSource = channels;
        }

        private void ChannelsListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (ChannelsListBox.SelectedItem is Channel selectedChannel)
            {
                JoinChannel(selectedChannel.Name);
            }
        }

        private void JoinChannel(string channelName)
        {
            if (_currentChannel != null)
            {
                _serviceClient.LeaveChannel(_currentUserId);
            }

            bool success = _serviceClient.JoinChannel(_currentUserId, channelName);
            if (success)
            {
                _currentChannel = channelName;
                LoadChannelMembers();
                MessagesListBox.Items.Clear();
            }
        }

        private void LoadChannelMembers()
        {
            if (!string.IsNullOrEmpty(_currentChannel))
            {
                var members = _serviceClient.GetChannelMembers(_currentChannel);
                MembersListBox.ItemsSource = members;
            }
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            string message = MessageTextBox.Text.Trim();
            if (!string.IsNullOrEmpty(message) && !string.IsNullOrEmpty(_currentChannel))
            {
                _serviceClient.SendMessage(_currentUserId, _currentChannel, message);
                MessageTextBox.Clear();
            }
        }

        private void PollingTimer_Tick(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(_currentUserId))
            {
                var messages = _serviceClient.GetPendingMessages(_currentUserId);
                foreach (var message in messages)
                {
                    AddMessageToUI(message);
                }

                var privateMessages = _serviceClient.GetPendingPrivateMessages(_currentUserId);
                foreach (var message in privateMessages)
                {
                    AddMessageToUI(message);
                }

                LoadChannelMembers();
            }
        }

        private void AddMessageToUI(Message message)
        {
            string displayText = $"[{message.Timestamp:HH:mm:ss}] {message.SenderId}: {message.Content}";
            MessagesListBox.Items.Add(displayText);
            MessagesListBox.ScrollIntoView(MessagesListBox.Items[MessagesListBox.Items.Count - 1]);
        }

        protected override void OnClosed(EventArgs e)
        {
            _pollingTimer?.Stop();
            _serviceClient?.SignOut(_currentUserId);
            _serviceClient?.Dispose();
            base.OnClosed(e);
        }
    }
}
