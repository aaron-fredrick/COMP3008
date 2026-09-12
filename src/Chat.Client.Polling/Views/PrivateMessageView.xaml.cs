using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Chat.Contracts.DataContracts;
using Chat.Contracts.SharedTypes;

namespace Chat.Client.Polling.Views
{
    public partial class PrivateMessageView : Window
    {
        public event EventHandler<string> SendMessageRequested;
        public event EventHandler FileUploadRequested;
        public event EventHandler<SharedFile> FileDownloadRequested;
        public event EventHandler<string> ExportChatRequested;

        public ObservableCollection<SharedFile> PendingFiles { get; } = new ObservableCollection<SharedFile>();

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
        private readonly System.Collections.Generic.List<Chat.Client.Shared.ViewModels.MessageViewModel> _messageViewModels;

        public PrivateMessageView(string recipientId)
        {
            InitializeComponent();
            FilesListBox.MouseDoubleClick += FilesListBox_MouseDoubleClick;
            RecipientId = recipientId;
            Title = $"DM — {recipientId}";
            RecipientText.Text = $"{recipientId}";
            _messages = new System.Collections.Generic.SortedSet<Message>();
            _messageViewModels = new System.Collections.Generic.List<Chat.Client.Shared.ViewModels.MessageViewModel>();
            ConfigureMessageAlignment();
        }

        private void ConfigureMessageAlignment()
        {
            var style = new System.Windows.Style(typeof(System.Windows.Controls.ListBoxItem));
            style.Setters.Add(new System.Windows.Setter(System.Windows.Controls.Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Left));
            var ownMessageTrigger = new System.Windows.DataTrigger { Binding = new System.Windows.Data.Binding("IsCurrentUser"), Value = true };
            ownMessageTrigger.Setters.Add(new System.Windows.Setter(System.Windows.Controls.Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Right));
            style.Triggers.Add(ownMessageTrigger);
            MessagesListBox.ItemContainerStyle = style;
        }

        public void AddMessage(Message message)
        {
            _messages.Add(message);
            RefreshMessages();
        }

        public void ClearMessageInput()
        {
            MessageTextBox.Clear();
        }

        public void AddPendingFile(SharedFile file)
        {
            if (file != null && !PendingFiles.Any(existing => existing.FileId != Guid.Empty && existing.FileId == file.FileId))
                PendingFiles.Add(file);
        }

        private void FilesListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var file = FilesListBox.SelectedItem as SharedFile;
            if (file != null && file.FileId != Guid.Empty) FileDownloadRequested?.Invoke(this, file);
        }

        private void UploadFileButton_Click(object sender, RoutedEventArgs e)
        {
            FileUploadRequested?.Invoke(this, EventArgs.Empty);
        }

        private void ExportChatButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Export DM Chat",
                FileName = $"Chat with {RecipientId}",
                DefaultExt = ".zip",
                Filter = "ZIP archive|*.zip"
            };

            if (dialog.ShowDialog() == true)
                ExportChatRequested?.Invoke(this, dialog.FileName);
        }

        public System.Collections.Generic.IEnumerable<Message> GetMessages() => _messages;

        private void RefreshMessages()
        {
            _messageViewModels.Clear();
            Chat.Client.Shared.ViewModels.MessageViewModel previous = null;
            foreach (var message in _messages)
            {
                bool showMetadata = previous == null || previous.SenderId != message.SenderId ||
                    previous.Timestamp.ToString("yyyyMMddHHmm") != message.Timestamp.ToLocalTime().ToString("yyyyMMddHHmm");
                var viewModel = new Chat.Client.Shared.ViewModels.MessageViewModel(message, showMetadata);
                _messageViewModels.Add(viewModel);
                previous = viewModel;
            }

            MessagesListBox.ItemsSource = _messageViewModels.ToList();
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
            }
        }
    }
}
