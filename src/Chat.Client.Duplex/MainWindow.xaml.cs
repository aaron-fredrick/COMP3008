using System;
using System.Collections.Generic;
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
        private Chat.Client.Shared.Controls.WindowResizer _windowResizer;

        public MainWindow()
        {
            InitializeComponent();
            _privateMessageViews = new Dictionary<string, PrivateMessageView>();
            _privateMessageHistory = new Dictionary<string, List<Message>>();
            _windowResizer = new Chat.Client.Shared.Controls.WindowResizer(this);

            SubscribeCoordinatorEvents();

            InitializeFooter();
            ShowSignInView();
        }

        // ── Initialisation ───────────────────────────────────────────────────

        private void SubscribeCoordinatorEvents()
        {
            var coordinator = DuplexSessionCoordinator.Instance;
            coordinator.ChannelsUpdated += OnChannelsUpdated;
            coordinator.ChannelMembersUpdated += OnChannelMembersUpdated;
            coordinator.ChannelFilesUpdated += OnChannelFilesUpdated;
            coordinator.PublicMessageReceived += OnPublicMessageReceived;
            coordinator.PrivateMessageReceived += OnPrivateMessageReceived;
            coordinator.PrivateFileReceived += OnPrivateFileReceived;
            coordinator.ConnectionStateChanged += OnConnectionStateChanged;
            coordinator.UserDisconnected += OnUserDisconnected;
            coordinator.SystemMessageReceived += OnSystemMessageReceived;
        }

        private void InitializeFooter()
        {
            AppFooter.SettingsClicked += AppFooter_SettingsClicked;
            AppFooter.SignOutClicked += (s, e) => SignOut();
            AppFooter.IsLoggedIn = false;
            AppFooter.ConnectionStatus = ConnectionState.Disconnected;
        }

        private void ShowSignInView()
        {
            var signInView = new SignInView();
            signInView.SignInSuccess += OnSignInSuccess;
            signInView.SignInFailed += OnSignInFailed;
            MainContent.Content = signInView;
        }

        // ── Coordinator event handlers ────────────────────────────────────────

        private void OnChannelsUpdated(object sender, List<Channel> channels) =>
            _channelListView?.UpdateChannels(channels);

        private void OnChannelMembersUpdated(object sender, List<string> members)
        {
            _conversationView?.UpdateMembers(members);
            ClosePrivateMessageViewsForUnavailableMembers(members);
        }

        private void OnChannelFilesUpdated(object sender, List<SharedFile> files) =>
            _conversationView?.UpdateFiles(files);

        private void OnPublicMessageReceived(object sender, Message message) =>
            _conversationView?.AddMessage(message);

        private void OnPrivateMessageReceived(object sender, (string OtherUserId, Message Message) args)
        {
            if (!_privateMessageHistory.ContainsKey(args.OtherUserId))
                _privateMessageHistory[args.OtherUserId] = new List<Message>();
            _privateMessageHistory[args.OtherUserId].Add(args.Message);

            if (!_privateMessageViews.ContainsKey(args.OtherUserId))
                OpenPrivateMessageView(args.OtherUserId);

            _privateMessageViews[args.OtherUserId].AddMessage(args.Message);
        }

        private void OnPrivateFileReceived(object sender, (string OtherUserId, SharedFile File) args)
        {
            if (!_privateFileHistory.ContainsKey(args.OtherUserId)) _privateFileHistory[args.OtherUserId] = new List<SharedFile>();
            if (!_privateFileHistory[args.OtherUserId].Exists(file => file.FileId == args.File.FileId)) _privateFileHistory[args.OtherUserId].Add(args.File);
            if (!_privateMessageViews.ContainsKey(args.OtherUserId)) OpenPrivateMessageView(args.OtherUserId);
            _privateMessageViews[args.OtherUserId].AddPendingFile(args.File);
        }

        private void OnConnectionStateChanged(object sender, ConnectionState state)
        {
            AppFooter.ConnectionStatus = state;

            if (state == ConnectionState.Disconnected && DuplexSessionCoordinator.Instance.CurrentUserId != null)
            {
                MessageBox.Show("Connection to the server was lost. You have been signed out.", "Connection Lost", MessageBoxButton.OK, MessageBoxImage.Error);
                SignOut();
            }
        }

        private void OnUserDisconnected(object sender, string userId) =>
            ClosePrivateMessageView(userId);

        private void OnSystemMessageReceived(object sender, string message) =>
            _conversationView?.AddSystemMessage(message);

        // ── Sign in / out ─────────────────────────────────────────────────────

        private void OnSignInSuccess(object sender, string username)
        {
            AppFooter.CurrentUser = username;
            AppFooter.IsLoggedIn = true;
            AppFooter.ConnectionStatus = ConnectionState.Connected;
            ShowChannelListView();
        }

        private void OnSignInFailed(object sender, string username) =>
            AppFooter.ConnectionStatus = ConnectionState.Disconnected;

        private void SignOut()
        {
            ClearPrivateMessageState();
            DuplexSessionCoordinator.Instance.SignOut();

            AppFooter.CurrentUser = string.Empty;
            AppFooter.IsLoggedIn = false;
            _channelListView = null;
            _conversationView = null;

            ShowSignInView();
        }

        // ── View navigation ───────────────────────────────────────────────────

        private void ShowChannelListView()
        {
            var coordinator = DuplexSessionCoordinator.Instance;

            _channelListView = new ChannelListView();
            _channelListView.SetWelcomeText(coordinator.CurrentUserId);
            _channelListView.JoinChannelRequested += OnJoinChannelRequested;
            _channelListView.CreateChannelRequested += OnCreateChannelRequested;
            _channelListView.SignOutRequested += OnSignOutRequested;

            MainContent.Content = _channelListView;
            _ = coordinator.RefreshChannelsAsync();
        }

        private void ShowConversationView(string channelName)
        {
            var coordinator = DuplexSessionCoordinator.Instance;

            _conversationView = new ConversationView();
            _conversationView.SetChannelName(channelName);
            _conversationView.SetCurrentUserId(coordinator.CurrentUserId);
            _conversationView.SendMessageRequested += OnSendMessageRequested;
            _conversationView.LeaveChannelRequested += OnLeaveChannelRequested;
            _conversationView.FileDownloadRequested += OnFileDownloadRequested;
            _conversationView.FileMessageDownloadRequested += OnFileMessageDownloadRequested;
            _conversationView.PrivateMessageRequested += OnPrivateMessageRequested;
            _conversationView.FileShareRequested += OnFileShareRequested;

            MainContent.Content = _conversationView;
            _ = coordinator.RefreshChannelMembersAsync();
            _ = coordinator.RefreshChannelFilesAsync();
        }

        // ── Channel list event handlers ────────────────────────────────────────

        private async void OnJoinChannelRequested(object sender, string channelName)
        {
            bool success = await DuplexSessionCoordinator.Instance.JoinChannelAsync(channelName);
            if (success)
                ShowConversationView(channelName);
        }

        private async void OnCreateChannelRequested(object sender, string channelName)
        {
            bool success = await DuplexSessionCoordinator.Instance.CreateChannelAsync(channelName);
            if (!success)
                MessageBox.Show("Channel already exists or creation failed.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void OnSignOutRequested(object sender, EventArgs e) => SignOut();

        // ── Conversation view event handlers ──────────────────────────────────

        private void OnSendMessageRequested(object sender, string content) =>
            DuplexSessionCoordinator.Instance.SendPublicMessage(content);

        private async void OnLeaveChannelRequested(object sender, EventArgs e)
        {
            await DuplexSessionCoordinator.Instance.LeaveChannelAsync();
            CloseAllPrivateMessageViews();
            ShowChannelListView();
        }

        private void OnFileDownloadRequested(object sender, SharedFile file) =>
            DuplexSessionCoordinator.Instance.DownloadAndOpenFile(file);

        private void OnFileMessageDownloadRequested(object sender, Message message)
        {
            if (!message.FileId.HasValue)
                return;

            DuplexSessionCoordinator.Instance.DownloadAndOpenFile(new SharedFile
            {
                FileId = message.FileId.Value,
                FileName = message.Content.Replace("Shared file: ", string.Empty)
            });
        }

        private void OnFileShareRequested(object sender, EventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select a file to share",
                Filter = "All files (*.*)|*.*"
            };

            if (dialog.ShowDialog() != true)
                return;

            string filePath = dialog.FileName;
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

            // Duplex: file-shared notification arrives via callback — no manual refresh needed.
            coordinator.ShareFile(fileName, fileType, fileData);
        }

        private void OnPrivateMessageRequested(object sender, string recipientId)
        {
            if (string.Equals(recipientId, DuplexSessionCoordinator.Instance.CurrentUserId, StringComparison.OrdinalIgnoreCase))
                return;

            if (!_privateMessageViews.ContainsKey(recipientId))
                OpenPrivateMessageView(recipientId);
            else
                _privateMessageViews[recipientId].Focus();
        }

        // ── Private message view management ──────────────────────────────────

        private void OpenPrivateMessageView(string recipientId)
        {
            var view = new PrivateMessageView(recipientId);
            view.CurrentUserId = DuplexSessionCoordinator.Instance.CurrentUserId;
            view.SendMessageRequested += OnPrivateMessageSendRequested;
            view.FileUploadRequested += OnPrivateMessageFileUploadRequested;
            view.FileDownloadRequested += OnPrivateMessageFileDownloadRequested;
            view.Closing += OnPrivateMessageViewClosing;
            view.Owner = this;
            _privateMessageViews[recipientId] = view;

            if (_privateMessageHistory.ContainsKey(recipientId))
            {
                foreach (var message in _privateMessageHistory[recipientId])
                    view.AddMessage(message);
            }
            if (_privateFileHistory.ContainsKey(recipientId))
                foreach (var file in _privateFileHistory[recipientId]) view.AddPendingFile(file);

            view.Show();
        }

        private void OnPrivateMessageSendRequested(object sender, string content)
        {
            var view = sender as PrivateMessageView;
            if (view == null)
                return;

            var coordinator = DuplexSessionCoordinator.Instance;
            if (!coordinator.SendPrivateMessage(view.RecipientId, content))
            {
                MessageBox.Show("Private message could not be sent. The recipient may no longer be in this channel.",
                    "Private Message", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var ownMessage = new Message
            {
                SenderId = coordinator.CurrentUserId,
                Content = content,
                Timestamp = DateTime.UtcNow,
                Type = MessageType.Private,
                RecipientId = view.RecipientId,
                IsCurrentUser = true
            };

            if (!_privateMessageHistory.ContainsKey(view.RecipientId))
                _privateMessageHistory[view.RecipientId] = new List<Message>();
            _privateMessageHistory[view.RecipientId].Add(ownMessage);

            view.AddMessage(ownMessage);
            view.ClearMessageInput();
        }

        private void OnPrivateMessageFileUploadRequested(object sender, EventArgs e)
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

        private void OnPrivateMessageFileDownloadRequested(object sender, SharedFile file)
        {
            if (!DuplexSessionCoordinator.Instance.DownloadAndOpenPrivateFile(file))
                MessageBox.Show("The file is no longer available in the current channel.", "Private file", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void OnPrivateMessageViewClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var view = sender as PrivateMessageView;
            if (view != null)
                _privateMessageViews.Remove(view.RecipientId);
        }

        private void ClosePrivateMessageView(string recipientId)
        {
            if (!_privateMessageViews.TryGetValue(recipientId, out var view))
                return;

            view.Close();
            _privateMessageViews.Remove(recipientId);
        }

        private void CloseAllPrivateMessageViews()
        {
            var views = new List<PrivateMessageView>(_privateMessageViews.Values);
            foreach (var view in views)
                view.Close();
            _privateMessageViews.Clear();
        }

        private void ClosePrivateMessageViewsForUnavailableMembers(List<string> members)
        {
            var availableMemberIds = new HashSet<string>(members);
            var unavailableRecipientIds = new List<string>();

            foreach (var kvp in _privateMessageViews)
            {
                if (!availableMemberIds.Contains(kvp.Key))
                    unavailableRecipientIds.Add(kvp.Key);
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

        // ── Footer ────────────────────────────────────────────────────────────

        private void AppFooter_SettingsClicked(object sender, EventArgs e) =>
            MessageBox.Show("Settings view will be implemented in a future task.", "Settings", MessageBoxButton.OK, MessageBoxImage.Information);

        // ── Window lifecycle ──────────────────────────────────────────────────

        protected override void OnClosed(EventArgs e)
        {
            DuplexSessionCoordinator.Instance.Dispose();
            _windowResizer?.Dispose();
            Application.Current.Shutdown();
            base.OnClosed(e);
        }
    }
}
