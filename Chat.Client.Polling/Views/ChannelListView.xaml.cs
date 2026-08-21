using System;
using System.Windows;
using System.Windows.Controls;
using Chat.Contracts.DataContracts;
using Chat.Client.Polling.Services;

namespace Chat.Client.Polling.Views
{
    public partial class ChannelListView : UserControl
    {
        public event EventHandler<string> JoinChannelRequested;
        public event EventHandler<string> CreateChannelRequested;
        public event EventHandler SignOutRequested;

        private string _currentUserId;
        private ChatServiceClient _serviceClient;

        public ChannelListView()
        {
            InitializeComponent();
        }

        public void SetServiceClient(ChatServiceClient serviceClient)
        {
            _serviceClient = serviceClient;
            LoadChannels();
        }

        public void SetWelcomeText(string username)
        {
            _currentUserId = username;
            WelcomeText.Text = $"Welcome, {username}";
        }

        public void UpdateChannels(System.Collections.Generic.List<Channel> channels)
        {
            ChannelsListBox.ItemsSource = channels;
        }

        private void LoadChannels()
        {
            if (_serviceClient != null)
            {
                var channels = _serviceClient.GetChannels();
                UpdateChannels(channels);
            }
        }

        private void ChannelsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ChannelsListBox.SelectedItem is Channel selectedChannel)
            {
                JoinButton.IsEnabled = true;
            }
            else
            {
                JoinButton.IsEnabled = false;
            }
        }

        private void CreateChannelButton_Click(object sender, RoutedEventArgs e)
        {
            string channelName = NewChannelTextBox.Text.Trim();
            if (!string.IsNullOrEmpty(channelName))
            {
                CreateChannelRequested?.Invoke(this, channelName);
                NewChannelTextBox.Clear();
            }
        }

        private void JoinButton_Click(object sender, RoutedEventArgs e)
        {
            if (ChannelsListBox.SelectedItem is Channel selectedChannel)
            {
                JoinChannelRequested?.Invoke(this, selectedChannel.Name);
            }
        }

        private void SignOutButton_Click(object sender, RoutedEventArgs e)
        {
            SignOutRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
