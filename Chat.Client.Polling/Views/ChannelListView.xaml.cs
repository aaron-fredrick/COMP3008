using System;
using System.Windows;
using System.Windows.Controls;
using Chat.Contracts.DataContracts;

namespace Chat.Client.Polling.Views
{
    public partial class ChannelListView : Window
    {
        public event EventHandler<string> JoinChannelRequested;
        public event EventHandler<string> CreateChannelRequested;
        public event EventHandler SignOutRequested;

        public ChannelListView()
        {
            InitializeComponent();
        }

        public void SetWelcomeText(string username)
        {
            WelcomeText.Text = $"Welcome, {username}";
        }

        public void UpdateChannels(System.Collections.Generic.List<Channel> channels)
        {
            ChannelsListBox.ItemsSource = channels;
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
