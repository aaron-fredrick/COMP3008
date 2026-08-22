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

        public static readonly DependencyProperty CurrentUserIdProperty =
            DependencyProperty.Register("CurrentUserId", typeof(string), typeof(PrivateMessageView),
                new PropertyMetadata(string.Empty));

        public string CurrentUserId
        {
            get { return (string)GetValue(CurrentUserIdProperty); }
            set 
            { 
                SetValue(CurrentUserIdProperty, value); 
                MessagesListBox.Tag = value;
            }
        }

        private readonly System.Collections.Generic.SortedSet<Message> _messages;

        public PrivateMessageView(string recipientId)
        {
            InitializeComponent();
            RecipientId = recipientId;
            Title = $"DM — {recipientId}";
            RecipientText.Text = $"{recipientId}";
            _messages = new System.Collections.Generic.SortedSet<Message>();
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
