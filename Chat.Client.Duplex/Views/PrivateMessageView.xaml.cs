using System;
using System.Windows;
using Chat.Contracts.DataContracts;

namespace Chat.Client.Duplex.Views
{
    public partial class PrivateMessageView : Window
    {
        public event EventHandler<string> SendMessageRequested;

        public string RecipientId { get; }

        private readonly System.Collections.Generic.SortedSet<Message> _messages;

        public PrivateMessageView(string recipientId)
        {
            InitializeComponent();
            RecipientId = recipientId;
            Title = $"Private Conversation with: {recipientId}";
            RecipientText.Text = $"Private Conversation with: {recipientId}";
            _messages = new System.Collections.Generic.SortedSet<Message>();
        }

        public void AddMessage(Message message)
        {
            _messages.Add(message);
            RefreshMessages();
        }

        private void RefreshMessages()
        {
            MessagesListBox.Items.Clear();
            foreach (var message in _messages)
            {
                string displayText = $"[{message.Timestamp:HH:mm:ss}] {message.SenderId}: {message.Content}";
                MessagesListBox.Items.Add(displayText);
            }
            if (MessagesListBox.Items.Count > 0)
            {
                MessagesListBox.ScrollIntoView(MessagesListBox.Items[MessagesListBox.Items.Count - 1]);
            }
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
    }
}
