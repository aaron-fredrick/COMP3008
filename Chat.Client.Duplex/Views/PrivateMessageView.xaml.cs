using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
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
            Title = $"DM — {recipientId}";
            RecipientText.Text = $"{recipientId}";
            RecipientInitials.Text = ExtractInitials(recipientId);
            _messages = new System.Collections.Generic.SortedSet<Message>();
        }

        private static string ExtractInitials(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "?";
            return name.Length >= 2 ? name.Substring(0, 2).ToUpper() : name.ToUpper();
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
                MessagesListBox.ScrollIntoView(MessagesListBox.Items[MessagesListBox.Items.Count - 1]);
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            SendCurrentMessage();
        }

        private void MessageTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !e.IsRepeat)
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
    }
}
