using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using Chat.Client.Duplex.Services;
using Chat.Client.Duplex.Views;
using Chat.Client.Shared.Controls;
using Chat.Contracts.DataContracts;
using Chat.Contracts.SharedTypes;

namespace Chat.Client.Duplex
{
    public partial class MainWindow : Window
    {
        private ChannelListView _channelListView;
        private ConversationView _conversationView;
        private readonly Dictionary<string, PrivateMessageView> _privateMessageViews;
        private readonly Dictionary<string, List<Message>> _privateMessageHistory;
        private readonly Dictionary<string, List<SharedFile>> _privateFileHistory = new Dictionary<string, List<SharedFile>>();
        private bool _isSigningOut = false;
        private bool _channelListClosing = false;
        private bool _conversationClosing = false;

        public MainWindow()
        {
            InitializeComponent();
            _privateMessageViews = new Dictionary<string, PrivateMessageView>();
            _privateMessageHistory = new Dictionary<string, List<Message>>();
            InitializeFooter();
            
            // Subscribe to global session coordinator events
            SubscribeCoordinatorEvents();
        }

        private void SubscribeCoordinatorEvents()
        {
            var coordinator = DuplexSessionCoordinator.Instance;
            
            coordinator.ChannelsUpdated += Coordinator_ChannelsUpdated;
            coordinator.ChannelMembersUpdated += Coordinator_ChannelMembersUpdated;
            coordinator.ChannelFilesUpdated += Coordinator_ChannelFilesUpdated;
            coordinator.PublicMessageReceived += Coordinator_PublicMessageReceived;
            coordinator.PrivateMessageReceived += Coordinator_PrivateMessageReceived;
            coordinator.PrivateFileReceived += Coordinator_PrivateFileReceived;
            coordinator.ConnectionStateChanged += Coordinator_ConnectionStateChanged;
            coordinator.UserDisconnected += Coordinator_UserDisconnected;
            coordinator.SystemMessageReceived += Coordinator_SystemMessageReceived;
        }

        private void InitializeFooter()
        {
            AppFooter.SettingsClicked += AppFooter_SettingsClicked;
            AppFooter.SignOutClicked += (s, e) => SignOut();
            UpdateFooterState();
        }

        private void AppFooter_SettingsClicked(object sender, EventArgs e)
        {
            MessageBox.Show("Settings view will be implemented in a future task.", "Settings", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void UpdateFooterState()
        {
            var coordinator = DuplexSessionCoordinator.Instance;

            if (coordinator.IsSignedIn)
            {
                AppFooter.CurrentUser = coordinator.CurrentUserId;
                AppFooter.IsLoggedIn = true;
            }
            else
            {
                AppFooter.CurrentUser = string.Empty;
                AppFooter.IsLoggedIn = false;
            }

            AppFooter.ConnectionStatus = coordinator.IsConnected ? ConnectionState.Connected : ConnectionState.Disconnected;
        }

        private void SignInButton_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameTextBox.Text.Trim();
            var coordinator = DuplexSessionCoordinator.Instance;

            LoginStatusText.Text = "Connecting to server...";
            AppFooter.ConnectionStatus = ConnectionState.Connecting;
            SignInButton.IsEnabled = false;

            try
            {
                coordinator.StartSession(Dispatcher);
                bool success = coordinator.SignIn(username);

                if (success)
                {
                    UpdateFooterState();
                    ShowChannelListView();
                }
                else
                {
                    LoginStatusText.Text = "Sign in failed. Username may already be in use or invalid.";
                    AppFooter.ConnectionStatus = ConnectionState.Disconnected;
                    SignInButton.IsEnabled = true;
                }
            }
            catch (Exception)
            {
                LoginStatusText.Text = "Server not responding. Please check if the server is running.";
                AppFooter.ConnectionStatus = ConnectionState.Disconnected;
                SignInButton.IsEnabled = true;
            }
        }

        private void UsernameTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                SignInButton_Click(sender, e);
            }
        }

        private void ShowChannelListView()
        {
            var coordinator = DuplexSessionCoordinator.Instance;

            _channelListView = new ChannelListView();
            _channelListView.SetWelcomeText(coordinator.CurrentUserId);
            _channelListView.SetConnectionStatus(coordinator.IsConnected);
            _channelListView.JoinChannelRequested += ChannelListView_JoinChannelRequested;
            _channelListView.CreateChannelRequested += ChannelListView_CreateChannelRequested;
            _channelListView.SignOutRequested += ChannelListView_SignOutRequested;
            _channelListView.Closing += ChannelListView_Closing;
            
            coordinator.RefreshChannels();
            _channelListView.Show();
            this.Hide();
        }

        private void ShowConversationView(string channelName)
        {
            var coordinator = DuplexSessionCoordinator.Instance;

            _conversationView = new ConversationView();
            _conversationView.SetChannelName(channelName);
            _conversationView.SetCurrentUserId(coordinator.CurrentUserId);
            _conversationView.SendMessageRequested += ConversationView_SendMessageRequested;
            _conversationView.LeaveChannelRequested += ConversationView_LeaveChannelRequested;
            _conversationView.FileDownloadRequested += ConversationView_FileDownloadRequested;
            _conversationView.FileMessageDownloadRequested += ConversationView_FileMessageDownloadRequested;
            _conversationView.PrivateMessageRequested += ConversationView_PrivateMessageRequested;
            _conversationView.FileShareRequested += ConversationView_FileShareRequested;
            _conversationView.SignOutRequested += (s, e) => SignOut();
            _conversationView.Closing += ConversationView_Closing;

            coordinator.RefreshChannelMembers();
            coordinator.RefreshChannelFiles();
            
            _conversationView.Show();
            _channelListView.Hide();
        }

        private void ChannelListView_Closing(object sender, CancelEventArgs e)
        {
            if (_isSigningOut) return;
            _channelListClosing = true;
            SignOut();
        }

        private void ConversationView_Closing(object sender, CancelEventArgs e)
        {
            if (_isSigningOut) return;
            _conversationClosing = true;
            SignOut();
        }

        private void ChannelListView_JoinChannelRequested(object sender, string channelName)
        {
            bool success = DuplexSessionCoordinator.Instance.JoinChannel(channelName);
            if (success)
            {
                ShowConversationView(channelName);
            }
        }

        private void ChannelListView_CreateChannelRequested(object sender, string channelName)
        {
            bool success = DuplexSessionCoordinator.Instance.CreateChannel(channelName);
            if (success)
            {
                DuplexSessionCoordinator.Instance.RefreshChannels();
            }
            else
            {
                MessageBox.Show("Channel already exists or creation failed.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ChannelListView_SignOutRequested(object sender, EventArgs e) => SignOut();

        private void ConversationView_SendMessageRequested(object sender, string message) =>
            DuplexSessionCoordinator.Instance.SendPublicMessage(message);

        private void ConversationView_LeaveChannelRequested(object sender, EventArgs e)
        {
            DuplexSessionCoordinator.Instance.LeaveChannel();

            // Private messaging is valid only while both users remain in the channel.
            CloseAllPrivateMessageViews();

            _conversationView.Close();
            _channelListView.Show();
            DuplexSessionCoordinator.Instance.RefreshChannels();
        }

        private void ConversationView_FileDownloadRequested(object sender, SharedFile file) =>
            DuplexSessionCoordinator.Instance.DownloadAndOpenFile(file);

        private void ConversationView_FileMessageDownloadRequested(object sender, Message message)
        {
            if (!message.FileId.HasValue)
                return;

            DuplexSessionCoordinator.Instance.DownloadAndOpenFile(new SharedFile
            {
                FileId = message.FileId.Value,
                FileName = message.Content.Replace("Shared file: ", string.Empty)
            });
        }

        private void ConversationView_FileShareRequested(object sender, EventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select a file to share",
                Filter = "All files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                var coordinator = DuplexSessionCoordinator.Instance;
                
                var validationResult = coordinator.ValidateFile(filePath);
                if (!validationResult.IsValid)
                {
                    MessageBox.Show(validationResult.ErrorMessage, "File Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string fileName = coordinator.GetFileName(filePath);
                byte[] fileData = coordinator.ReadFile(filePath);
                FileType fileType = coordinator.DetermineFileType(fileName);

                bool success = coordinator.ShareFile(fileName, fileType, fileData);
                if (success)
                {
                    coordinator.RefreshChannelFiles();
                }
            }
        }

        private void ConversationView_PrivateMessageRequested(object sender, string recipientId)
        {
            var coordinator = DuplexSessionCoordinator.Instance;
            if (recipientId == coordinator.CurrentUserId) return;

            if (!_privateMessageViews.ContainsKey(recipientId))
            {
                OpenPrivateMessageView(recipientId);
            }
            else
            {
                _privateMessageViews[recipientId].Focus();
            }
        }

        private void OpenPrivateMessageView(string recipientId)
        {
            var privateMessageView = new PrivateMessageView(recipientId);
            privateMessageView.CurrentUserId = DuplexSessionCoordinator.Instance.CurrentUserId;
            privateMessageView.SendMessageRequested += PrivateMessageView_SendMessageRequested;
            privateMessageView.FileUploadRequested += DuplexPrivateMessageFileUploadRequested;
            privateMessageView.FileDownloadRequested += DuplexPrivateMessageFileDownloadRequested;
            privateMessageView.Closing += PrivateMessageView_Closing;
            privateMessageView.Owner = _conversationView;

            _privateMessageViews[recipientId] = privateMessageView;

            // Restore message history if available
            if (_privateMessageHistory.ContainsKey(recipientId))
            {
                foreach (var message in _privateMessageHistory[recipientId])
                {
                    privateMessageView.AddMessage(message);
                }
            }
            if (_privateFileHistory.ContainsKey(recipientId))
                foreach (var file in _privateFileHistory[recipientId]) privateMessageView.AddPendingFile(file);

            privateMessageView.Show();
        }

        private void PrivateMessageView_SendMessageRequested(object sender, string message)
        {
            if (sender is PrivateMessageView view)
            {
                if (!DuplexSessionCoordinator.Instance.SendPrivateMessage(view.RecipientId, message))
                {
                    MessageBox.Show("Private message could not be sent. The recipient may no longer be in this channel.",
                        "Private Message", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Add own message directly to view
                var ownMessage = new Message
                {
                    SenderId = DuplexSessionCoordinator.Instance.CurrentUserId,
                    Content = message,
                    Timestamp = DateTime.UtcNow,
                    Type = MessageType.Private,
                    RecipientId = view.RecipientId,
                    IsCurrentUser = true
                };

                // Store sent message in history
                if (!_privateMessageHistory.ContainsKey(view.RecipientId))
                    _privateMessageHistory[view.RecipientId] = new List<Message>();
                _privateMessageHistory[view.RecipientId].Add(ownMessage);

                view.AddMessage(ownMessage);
                view.ClearMessageInput();
            }
        }

        private void DuplexPrivateMessageFileUploadRequested(object sender, EventArgs e)
        {
            if (!(sender is PrivateMessageView view))
                return;

            var dialog = new Microsoft.Win32.OpenFileDialog { Title = "Select a file for this private conversation", Filter = "Allowed files|*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.txt" };
            if (dialog.ShowDialog() != true)
                return;

            var coordinator = DuplexSessionCoordinator.Instance;
            var validation = coordinator.ValidateFile(dialog.FileName);
            if (!validation.IsValid)
            {
                MessageBox.Show(validation.ErrorMessage, "Private file", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var sharedFile = coordinator.SharePrivateFile(view.RecipientId, coordinator.GetFileName(dialog.FileName), coordinator.DetermineFileType(dialog.FileName), coordinator.ReadFile(dialog.FileName));
            if (sharedFile == null)
            {
                MessageBox.Show("The private file could not be shared. Both users must be in the same channel.", "Private file", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            view.AddPendingFile(sharedFile);
            if (!_privateFileHistory.ContainsKey(view.RecipientId)) _privateFileHistory[view.RecipientId] = new List<SharedFile>();
            _privateFileHistory[view.RecipientId].Add(sharedFile);
        }

        private void DuplexPrivateMessageFileDownloadRequested(object sender, SharedFile file)
        {
            if (!DuplexSessionCoordinator.Instance.DownloadAndOpenPrivateFile(file))
                MessageBox.Show("The file is no longer available in the current channel.", "Private file", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void PrivateMessageView_Closing(object sender, CancelEventArgs e)
        {
            if (sender is PrivateMessageView view)
            {
                _privateMessageViews.Remove(view.RecipientId);
            }
        }

        // ── Coordinator Event Handlers ────────────────────────────────────────

        private void Coordinator_ChannelsUpdated(object sender, List<Channel> channels) =>
            _channelListView?.UpdateChannels(channels);

        private void Coordinator_ChannelMembersUpdated(object sender, List<string> members)
        {
            _conversationView?.UpdateMembers(members);
            ClosePrivateMessageViewsForUnavailableMembers(members);
        }

        private void Coordinator_ChannelFilesUpdated(object sender, List<SharedFile> files) =>
            _conversationView?.UpdateFiles(files);

        private void Coordinator_PublicMessageReceived(object sender, Message message) =>
            _conversationView?.AddMessage(message);

        private void Coordinator_PrivateMessageReceived(object sender, (string OtherUserId, Message Message) args)
        {
            // Store message in history
            if (!_privateMessageHistory.ContainsKey(args.OtherUserId))
                _privateMessageHistory[args.OtherUserId] = new List<Message>();
            _privateMessageHistory[args.OtherUserId].Add(args.Message);

            if (!_privateMessageViews.ContainsKey(args.OtherUserId))
            {
                OpenPrivateMessageView(args.OtherUserId);
            }
            _privateMessageViews[args.OtherUserId].AddMessage(args.Message);
        }

        private void Coordinator_PrivateFileReceived(object sender, (string OtherUserId, SharedFile File) args)
        {
            if (!_privateFileHistory.ContainsKey(args.OtherUserId)) _privateFileHistory[args.OtherUserId] = new List<SharedFile>();
            if (!_privateFileHistory[args.OtherUserId].Exists(file => file.FileId == args.File.FileId)) _privateFileHistory[args.OtherUserId].Add(args.File);
            if (!_privateMessageViews.ContainsKey(args.OtherUserId)) OpenPrivateMessageView(args.OtherUserId);
            _privateMessageViews[args.OtherUserId].AddPendingFile(args.File);
        }

        private void Coordinator_UserDisconnected(object sender, string userId)
        {
            ClosePrivateMessageView(userId);
        }

        private void Coordinator_SystemMessageReceived(object sender, string message) =>
            _conversationView?.AddSystemMessage(message);

        private void Coordinator_ConnectionStateChanged(object sender, ConnectionState state)
        {
            UpdateFooterState();
            
            if (state == ConnectionState.Disconnected && !_isSigningOut && DuplexSessionCoordinator.Instance.CurrentUserId != null)
            {
                MessageBox.Show("Connection to the server was lost. You have been signed out.", "Connection Lost", MessageBoxButton.OK, MessageBoxImage.Error);
                SignOut();
            }
        }

        private void SignOut()
        {
            if (_isSigningOut) return;
            _isSigningOut = true;

            DuplexSessionCoordinator.Instance.SignOut();

            ClearPrivateMessageState();

            if (!_channelListClosing) _channelListView?.Close();
            if (!_conversationClosing) _conversationView?.Close();

            this.Show();
            LoginStatusText.Text = "";
            UsernameTextBox.Text = "";
            UpdateFooterState();
            SignInButton.IsEnabled = true;

            _channelListClosing = false;
            _conversationClosing = false;
            _isSigningOut = false;
        }

        private void ClosePrivateMessageView(string recipientId)
        {
            if (!_privateMessageViews.TryGetValue(recipientId, out var privateMessageView))
                return;

            privateMessageView.Close();
            _privateMessageViews.Remove(recipientId);
        }

        private void CloseAllPrivateMessageViews()
        {
            var privateMessageViews = new List<PrivateMessageView>(_privateMessageViews.Values);
            foreach (var privateMessageView in privateMessageViews)
                privateMessageView.Close();
            _privateMessageViews.Clear();
        }

        private void ClosePrivateMessageViewsForUnavailableMembers(List<string> members)
        {
            var availableMemberIds = new HashSet<string>(members);
            var unavailableRecipientIds = new List<string>();

            foreach (var privateMessageView in _privateMessageViews)
            {
                if (!availableMemberIds.Contains(privateMessageView.Key))
                    unavailableRecipientIds.Add(privateMessageView.Key);
            }

            foreach (var recipientId in unavailableRecipientIds)
                ClosePrivateMessageView(recipientId);
        }

        private void ClearPrivateMessageState()
        {
            CloseAllPrivateMessageViews();
            _privateMessageHistory.Clear();
            _privateFileHistory.Clear();
        }

        protected override void OnClosed(EventArgs e)
        {
            DuplexSessionCoordinator.Instance.Dispose();
            
            _channelListView?.Close();
            _conversationView?.Close();
            
            Application.Current.Shutdown();
            base.OnClosed(e);
        }
    }
}
