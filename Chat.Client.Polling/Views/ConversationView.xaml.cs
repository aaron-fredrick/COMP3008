using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Chat.Contracts.DataContracts;
using Chat.Contracts.SharedTypes;

namespace Chat.Client.Polling.Views
{
    public class FileSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is long bytes)
            {
                if (bytes < 1024)
                {
                    return $"{bytes} B";
                }
                else if (bytes < 1024 * 1024)
                {
                    double kb = bytes / 1024.0;
                    return $"{Math.Round(kb)} kB";
                }
                else
                {
                    double mb = bytes / (1024.0 * 1024.0);
                    return $"{Math.Round(mb)} MB";
                }
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public partial class ConversationView : Window
    {
        public event EventHandler<string> SendMessageRequested;
        public event EventHandler LeaveChannelRequested;
        public event EventHandler<SharedFile> FileDownloadRequested;
        public event EventHandler<string> PrivateMessageRequested;
        public event EventHandler FileShareRequested;

        private readonly System.Collections.Generic.SortedSet<Message> _messages;

        public ConversationView()
        {
            InitializeComponent();
            _messages = new System.Collections.Generic.SortedSet<Message>();
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
            _messages.Add(message);
            RefreshMessages();
        }

        private void RefreshMessages()
        {
            MessagesListBox.Items.Clear();
            foreach (var message in _messages)
            {
                string displayText;
                if (message.Type == MessageType.File)
                {
                    string fileName = message.Content.Replace("Shared file: ", "");
                    displayText = $"[{message.Timestamp:HH:mm:ss}] {message.SenderId} shared file: {fileName}";
                }
                else
                {
                    displayText = $"[{message.Timestamp:HH:mm:ss}] {message.SenderId}: {message.Content}";
                }
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
