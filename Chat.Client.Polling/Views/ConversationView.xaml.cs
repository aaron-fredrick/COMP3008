using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Chat.Contracts.DataContracts;
using Chat.Contracts.SharedTypes;

namespace Chat.Client.Polling.Views
{
    public partial class ConversationView : UserControl
    {
        public event EventHandler<string> SendMessageRequested;
        public event EventHandler LeaveChannelRequested;
        public event EventHandler<SharedFile> FileDownloadRequested;
        public event EventHandler<string> PrivateMessageRequested;
        public event EventHandler FileShareRequested;

        private readonly System.Collections.Generic.SortedSet<Message> _messages;

        public static readonly DependencyProperty CurrentUserIdProperty =
            DependencyProperty.Register("CurrentUserId", typeof(string), typeof(ConversationView),
                new PropertyMetadata(string.Empty));

        public string CurrentUserId
        {
            get { return (string)GetValue(CurrentUserIdProperty); }
            set { SetValue(CurrentUserIdProperty, value); }
        }

        public ConversationView()
        {
            InitializeComponent();
            _messages = new System.Collections.Generic.SortedSet<Message>();
        }

        public void SetChannelName(string channelName)
        {
            ChannelNameText.Text = channelName;
            ChannelNameSidebar.Text = channelName;
        }

        public void SetCurrentUserId(string userId)
        {
            CurrentUserId = userId;
        }

        public void UpdateMembers(System.Collections.Generic.List<string> members)
        {
            MembersListBox.ItemsSource = members;
            OnlineMembersListBox.ItemsSource = members;
            MembersSectionText.Text = $"MEMBERS — {members.Count}";
            MemberCountText.Text = $"{members.Count} members · {members.Count} online";
        }

        public void UpdateFiles(System.Collections.Generic.List<SharedFile> files)
        {
            FilesListBox.ItemsSource = files;
        }

        public void AddMessage(Message message)
        {
            _messages.Add(message);
            RefreshMessages();
        }

        private void RefreshMessages()
        {
            MessagesListBox.ItemsSource = _messages.ToList();
            if (MessagesListBox.Items.Count > 0)
            {
                MessagesListBox.ScrollIntoView(MessagesListBox.Items[MessagesListBox.Items.Count - 1]);
            }
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            SendCurrentMessage();
        }

        private void MessageTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter && !e.IsRepeat)
            {
                e.Handled = true;
                SendCurrentMessage();
            }
        }

        private void SendCurrentMessage()
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

        private void MembersListBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (MembersListBox.SelectedItem is string selectedMember)
            {
                PrivateMessageRequested?.Invoke(this, selectedMember);
            }
        }

        private void ShareFileButton_Click(object sender, RoutedEventArgs e)
        {
            FileShareRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
