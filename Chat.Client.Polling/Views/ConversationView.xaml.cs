using System;
using System.Windows;
using System.Windows.Controls;
using Chat.Contracts.DataContracts;
using Chat.Contracts.SharedTypes;

namespace Chat.Client.Polling.Views
{
    public partial class ConversationView : Window
    {
        public event EventHandler<string> SendMessageRequested;
        public event EventHandler LeaveChannelRequested;
        public event EventHandler SignOutRequested;
        public event EventHandler<SharedFile> FileDownloadRequested;

        private bool _isLeavingChannel = false;

        public ConversationView()
        {
            InitializeComponent();
            this.Closing += ConversationView_Closing;
        }

        private void ConversationView_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Allow close if we're already leaving (programmatic close)
            if (_isLeavingChannel)
            {
                return;
            }

            // User clicked X - trigger sign out and prevent close
            e.Cancel = true;
            _isLeavingChannel = true;
            SignOutRequested?.Invoke(this, EventArgs.Empty);
        }

        public void CloseWindow()
        {
            _isLeavingChannel = true;
            this.Close();
        }

        public void SetChannelName(string channelName)
        {
            ChannelNameText.Text = channelName;
        }

        public void UpdateMembers(System.Collections.Generic.List<string> members)
        {
            MembersListBox.ItemsSource = members;
        }

        public void UpdateFiles(System.Collections.Generic.List<SharedFile> files)
        {
            FilesListBox.ItemsSource = files;
        }

        public void AddMessage(Message message)
        {
            string displayText = $"[{message.Timestamp:HH:mm:ss}] {message.SenderId}: {message.Content}";
            MessagesListBox.Items.Add(displayText);
            MessagesListBox.ScrollIntoView(MessagesListBox.Items[MessagesListBox.Items.Count - 1]);
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            string message = MessageTextBox.Text.Trim();
            if (!string.IsNullOrEmpty(message))
            {
                SendMessageRequested?.Invoke(this, message);
                MessageTextBox.Clear();
            }
        }

        private void LeaveButton_Click(object sender, RoutedEventArgs e)
        {
            LeaveChannelRequested?.Invoke(this, EventArgs.Empty);
        }

        private void FilesListBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (FilesListBox.SelectedItem is SharedFile selectedFile)
            {
                FileDownloadRequested?.Invoke(this, selectedFile);
            }
        }
    }
}
