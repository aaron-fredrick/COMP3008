using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Chat.Contracts.DataContracts;
using Chat.Contracts.SharedTypes;

namespace Chat.Client.Duplex.Views
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
        private readonly ObservableCollection<Chat.Client.Shared.ViewModels.MessageViewModel> _messageViewModels;

        // Bottom-following: user is considered "at bottom" when within this many pixels of the end.
        private const double BottomThreshold = 20.0;
        private bool _isAtBottom = true;
        private ScrollViewer _scrollViewer;

        private const double TextBoxMinHeight = 36.0;
        private const double LineHeight = 18.0;
        private const double MaxLines = 8.0;
        private const double MultilineVerticalPadding = 16.0;

        public PrivateMessageView(string recipientId)
        {
            InitializeComponent();
            FilesListBox.MouseDoubleClick += FilesListBox_MouseDoubleClick;
            RecipientId = recipientId;
            Title = $"DM — {recipientId}";
            RecipientText.Text = $"{recipientId}";
            _messages = new System.Collections.Generic.SortedSet<Message>();
            _messageViewModels = new ObservableCollection<Chat.Client.Shared.ViewModels.MessageViewModel>();

            // Set the ItemsSource once; it is never replaced — only items are added/removed.
            MessagesListBox.ItemsSource = _messageViewModels;

            Loaded += (_, __) => _scrollViewer = FindScrollViewer(MessagesListBox);

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
            if (!_messages.Add(message))
                return; // Duplicate — already present; nothing to do.

            AppendMessageViewModel(message);
        }

        /// <summary>
        /// Appends a single new ViewModel for <paramref name="message"/> and computes whether
        /// metadata should be shown based on the previous message. Conditionally scrolls to
        /// the bottom only when the user was already there.
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

            if (_isAtBottom)
                ScrollToBottom();
        }

        private void ScrollToBottom()
        {
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

        private void FileMessage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is Chat.Client.Shared.ViewModels.MessageViewModel viewModel &&
                viewModel.Message.FileId.HasValue)
            {
                FileDownloadRequested?.Invoke(this, new SharedFile { FileId = viewModel.Message.FileId.Value, FileName = viewModel.Content.Replace("Shared file: ", string.Empty) });
            }
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

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            SendCurrentMessage();
        }

        private void MessageTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !e.IsRepeat)
            {
                if (Keyboard.Modifiers == ModifierKeys.Shift)
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

        private void MessageTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateTextBoxHeight();
        }

        private void UpdateTextBoxHeight()
        {
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
