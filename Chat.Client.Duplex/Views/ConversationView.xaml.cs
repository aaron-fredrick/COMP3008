using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Chat.Contracts.DataContracts;
using Chat.Contracts.SharedTypes;

namespace Chat.Client.Duplex.Views
{
    public partial class ConversationView : Window
    {
        public event EventHandler<string> SendMessageRequested;
        public event EventHandler LeaveChannelRequested;
        public event EventHandler SignOutRequested;
        public event EventHandler<SharedFile> FileDownloadRequested;
        public event EventHandler<string> PrivateMessageRequested;
        public event EventHandler FileShareRequested;

        private readonly System.Collections.Generic.SortedSet<Message> _messages;
        private readonly System.Collections.Generic.List<Chat.Client.Shared.ViewModels.MessageViewModel> _messageViewModels;

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
            InitializeFooter();
        }

        private void InitializeFooter()
        {
            AppFooter.SettingsClicked += AppFooter_SettingsClicked;
            AppFooter.SignOutClicked += (s, e) => SignOutRequested?.Invoke(this, EventArgs.Empty);
        }

        private void AppFooter_SettingsClicked(object sender, EventArgs e)
        {
            MessageBox.Show("Settings view will be implemented in a future task.", "Settings", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void SetChannelName(string channelName)
        {
            ChannelNameText.Text = channelName;
            ChannelNameSidebar.Text = channelName;
        }

        public void SetCurrentUserId(string userId)
        {
            CurrentUserId = userId;
            UpdateFooter();
        }

        private void UpdateFooter()
        {
            if (!string.IsNullOrEmpty(CurrentUserId))
            {
                AppFooter.CurrentUser = CurrentUserId;
                AppFooter.IsLoggedIn = true;
            }
            else
            {
                AppFooter.CurrentUser = string.Empty;
                AppFooter.IsLoggedIn = false;
            }
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

        public void AddSystemMessage(string text)
        {
            // System messages are shown as plain string items appended at the end of the list.
            // We clear ItemsSource to manually add items.
            MessagesListBox.ItemsSource = null;
            MessagesListBox.Items.Clear();
            foreach (var vm in _messageViewModels)
                MessagesListBox.Items.Add(vm);
            MessagesListBox.Items.Add($"— {text} —");
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

        private void LeaveButton_Click(object sender, RoutedEventArgs e)
        {
            LeaveChannelRequested?.Invoke(this, EventArgs.Empty);
        }

        private void FilesListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (FilesListBox.SelectedItem is SharedFile selectedFile)
            {
                FileDownloadRequested?.Invoke(this, selectedFile);
            }
        }

        private void MembersListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
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
