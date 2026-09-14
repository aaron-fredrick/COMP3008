using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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
        private readonly ObservableCollection<Chat.Client.Shared.ViewModels.MessageViewModel> _messageViewModels;

        // Bottom-following: user is considered "at bottom" when within this many pixels of the end.
        private const double BottomThreshold = 20.0;
        private bool _isAtBottom = true;
        private ScrollViewer _scrollViewer;

        private const double TextBoxMinHeight = 36.0;
        private const double LineHeight = 18.0;
        private const double MaxLines = 8.0;
        private const double MultilineVerticalPadding = 16.0;

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
            _messageViewModels = new ObservableCollection<Chat.Client.Shared.ViewModels.MessageViewModel>();

            // Set the ItemsSource once; it is never replaced — only items are added/removed.
            MessagesListBox.ItemsSource = _messageViewModels;

            Loaded += (_, __) => _scrollViewer = FindScrollViewer(MessagesListBox);
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
            if (!_messages.Add(message))
                return; // Duplicate — already present; nothing to do.

            AppendMessageViewModel(message);
        }

        /// <summary>
        /// Appends a single new ViewModel for <paramref name="message"/> and, if required,
        /// computes whether metadata should be shown based on the previous message's sender/minute.
        /// Then conditionally scrolls to the bottom only when the user was already there.
        /// </summary>
        private void AppendMessageViewModel(Message message)
        {
            bool showMetadata = true;

            if (_messageViewModels.Count > 0)
            {
                var previous = _messageViewModels[_messageViewModels.Count - 1];
                if (previous.SenderId == message.SenderId &&
                    previous.Timestamp.ToString("yyyyMMddHHmm") == message.Timestamp.ToLocalTime().ToString("yyyyMMddHHmm"))
                {
                    showMetadata = false;
                }
            }

            _messageViewModels.Add(new Chat.Client.Shared.ViewModels.MessageViewModel(message, showMetadata));

            // Only follow to bottom if the user was already at/near the bottom before the new message.
            if (_isAtBottom)
                ScrollToBottom();
        }

        private void ScrollToBottom()
        {
            // Defer until after layout so the new item's height is measured before scrolling.
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, new Action(() =>
            {
                _scrollViewer?.ScrollToEnd();
            }));
        }

        private void MessagesListBox_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (_scrollViewer == null) return;
            _isAtBottom = _scrollViewer.VerticalOffset >= _scrollViewer.ScrollableHeight - BottomThreshold;
        }

        private static ScrollViewer FindScrollViewer(DependencyObject element)
        {
            if (element is ScrollViewer sv) return sv;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
            {
                var result = FindScrollViewer(VisualTreeHelper.GetChild(element, i));
                if (result != null) return result;
            }
            return null;
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
            // Capture scroll state before resize so we can restore bottom anchoring if needed.
            bool wasAtBottom = _isAtBottom;

            int lineCount = MessageTextBox.LineCount;
            double desiredHeight;

            if (lineCount > 1)
            {
                desiredHeight = MultilineVerticalPadding + (LineHeight * lineCount) + GetBorderVerticalThickness();
                MessageTextBox.VerticalContentAlignment = VerticalAlignment.Top;
                MessageTextBox.Padding = new Thickness(11, 0, 11, 0);
                MessageTextBox.Margin = new Thickness(0, 8, 0, 8);
            }
            else
            {
                desiredHeight = TextBoxMinHeight;
                MessageTextBox.VerticalContentAlignment = VerticalAlignment.Center;
                MessageTextBox.Padding = new Thickness(11, 0, 11, 0);
                MessageTextBox.Margin = new Thickness(0);
            }

            double maximumHeight = MultilineVerticalPadding + (LineHeight * MaxLines) + GetBorderVerticalThickness();
            double clampedHeight = Math.Min(desiredHeight, maximumHeight);
            MessageTextBoxBorder.Height = clampedHeight;

            // If the user was at the bottom, preserve that anchor after the viewport height changes.
            if (wasAtBottom)
                ScrollToBottom();
        }

        private double GetBorderVerticalThickness() =>
            MessageTextBoxBorder.BorderThickness.Top + MessageTextBoxBorder.BorderThickness.Bottom;

        private void ResetTextBoxHeight()
        {
            MessageTextBoxBorder.Height = TextBoxMinHeight;
            MessageTextBox.VerticalContentAlignment = VerticalAlignment.Center;
            MessageTextBox.Padding = new Thickness(11, 0, 11, 0);
            MessageTextBox.Margin = new Thickness(0);
        }
    }
}
