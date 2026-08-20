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

        private void ChannelsListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (ChannelsListBox.SelectedItem is Channel selectedChannel)
            {
                NewChannelTextBox.Text = selectedChannel.Name;
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
