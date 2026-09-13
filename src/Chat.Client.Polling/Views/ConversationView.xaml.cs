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
        public event EventHandler<Message> FileMessageDownloadRequested;
        public event EventHandler<string> PrivateMessageRequested;
        public event EventHandler FileShareRequested;
        public event EventHandler<string> ExportChatRequested;

        public System.Collections.Generic.IEnumerable<Message> GetMessages() => _messages;

        private readonly System.Collections.Generic.SortedSet<Message> _messages;
        private readonly System.Collections.Generic.List<Chat.Client.Shared.ViewModels.MessageViewModel> _messageViewModels;

        private const double TextBoxMinHeight = 36.0;
        private const double LineHeight = 18.0;
        private const double MaxLines = 8.0;
        private const double TextBoxMaxHeight = TextBoxMinHeight + (LineHeight * (MaxLines - 1));

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
            _messageViewModels = new System.Collections.Generic.List<Chat.Client.Shared.ViewModels.MessageViewModel>();
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
            MembersSectionText.Text = $"MEMBERS — {members.Count}";

        }

        public void UpdateFiles(System.Collections.Generic.List<SharedFile> files)
        {
            FilesListBox.ItemsSource = files;
            NoFilesText.Visibility = files.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        public void AddMessage(Message message)
        {
            _messages.Add(message);
            RefreshMessages();
        }

        private void RefreshMessages()
        {
            _messageViewModels.Clear();
            Chat.Client.Shared.ViewModels.MessageViewModel previousVm = null;

            foreach (var message in _messages)
            {
                bool showMetadata = true;
                if (previousVm != null)
                {
                    if (previousVm.SenderId == message.SenderId && 
                        previousVm.Timestamp.ToString("yyyyMMddHHmm") == message.Timestamp.ToLocalTime().ToString("yyyyMMddHHmm"))
                    {
                        showMetadata = false;
                    }
                }
                
                var vm = new Chat.Client.Shared.ViewModels.MessageViewModel(message, showMetadata);
                _messageViewModels.Add(vm);
                previousVm = vm;
            }

            MessagesListBox.ItemsSource = _messageViewModels.ToList();
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
                if (System.Windows.Input.Keyboard.Modifiers == System.Windows.Input.ModifierKeys.Shift)
                {
                    int caretIndex = MessageTextBox.CaretIndex;
                    MessageTextBox.Text = MessageTextBox.Text.Insert(caretIndex, Environment.NewLine);
                    MessageTextBox.CaretIndex = caretIndex + Environment.NewLine.Length;
                    e.Handled = true;
                    return;
                }
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
                ResetTextBoxHeight();
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

        private void FileMessage_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is Chat.Client.Shared.ViewModels.MessageViewModel viewModel &&
                viewModel.Message.FileId.HasValue)
            {
                FileMessageDownloadRequested?.Invoke(this, viewModel.Message);
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

        private void ExportChatButton_Click(object sender, RoutedEventArgs e)
        {
            if (_messages.Count == 0)
            {
                MessageBox.Show("There are no messages to export.", "Export Chat", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Export Chat",
                Filter = "ZIP Archive (*.zip)|*.zip",
                FileName = $"{ChannelNameText.Text} channel chat.zip"
            };

            if (dialog.ShowDialog() == true)
            {
                ExportChatRequested?.Invoke(this, dialog.FileName);
            }
        }

        private void MessageTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateTextBoxHeight();
        }

        private void UpdateTextBoxHeight()
        {
            int lineCount = MessageTextBox.LineCount;
            double desiredHeight = TextBoxMinHeight + (LineHeight * (lineCount - 1));
            double clampedHeight = Math.Min(desiredHeight, TextBoxMaxHeight);
            MessageTextBoxBorder.Height = clampedHeight;
            
            // Center text for single-line, top-align for multiline with padding
            if (lineCount > 1)
            {
                MessageTextBox.VerticalContentAlignment = VerticalAlignment.Top;
                MessageTextBox.Padding = new Thickness(11, 8, 11, 8);
            }
            else
            {
                MessageTextBox.VerticalContentAlignment = VerticalAlignment.Center;
                MessageTextBox.Padding = new Thickness(11, 0, 11, 0);
            }
        }

        private void ResetTextBoxHeight()
        {
            MessageTextBoxBorder.Height = TextBoxMinHeight;
            MessageTextBox.VerticalContentAlignment = VerticalAlignment.Center;
            MessageTextBox.Padding = new Thickness(11, 0, 11, 0);
        }
    }
}
