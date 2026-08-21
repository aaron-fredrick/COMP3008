using System;
using System.Windows;
using Chat.Contracts.DataContracts;

namespace Chat.Client.Duplex.Views
{
    public partial class ChannelListView : Window
    {
        public event EventHandler<string> JoinChannelRequested;
        public event EventHandler<string> CreateChannelRequested;
        public event EventHandler SignOutRequested;

        private string _currentUserId;
        private bool _isConnected;

        public ChannelListView()
        {
            InitializeComponent();
            InitializeFooter();
        }

        private void InitializeFooter()
        {
            AppFooter.SettingsClicked += AppFooter_SettingsClicked;
            UpdateFooter();
        }

        public void SetConnectionStatus(bool isConnected)
        {
            _isConnected = isConnected;
            AppFooter.ConnectionStatus = isConnected 
                ? Chat.Client.Shared.Controls.ConnectionState.Connected 
                : Chat.Client.Shared.Controls.ConnectionState.Disconnected;
        }

        private void AppFooter_SettingsClicked(object sender, EventArgs e)
        {
            MessageBox.Show("Settings view will be implemented in a future task.", "Settings", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void SetWelcomeText(string username)
        {
            _currentUserId = username;
            WelcomeText.Text = $"Welcome, {username}";
            UpdateFooter();
        }

        private void UpdateFooter()
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
        }

        public void UpdateChannels(System.Collections.Generic.List<Channel> channels)
        {
            ChannelsListBox.ItemsSource = channels;
        }

        private void ChannelsListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // Removed: previously overwrote NewChannelTextBox with selected channel name.
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
            else if (!string.IsNullOrEmpty(NewChannelTextBox.Text))
            {
                JoinChannelRequested?.Invoke(this, NewChannelTextBox.Text.Trim());
            }
        }

        private void SignOutButton_Click(object sender, RoutedEventArgs e)
        {
            SignOutRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
