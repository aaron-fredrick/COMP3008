using System;
using System.Collections.Generic;
using System.Configuration;
using System.Windows;
using Chat.Client.Polling.Services;
using Chat.Client.Polling.Views;
using Chat.Client.Shared.Controls;
using Chat.Contracts.DataContracts;
using Chat.Contracts.SharedTypes;

namespace Chat.Client.Polling
{
    public partial class MainWindow : Window
    {
        private ChatServiceClient _serviceClient;
        private ChannelListView _channelListView;
        private ConversationView _conversationView;
        private readonly Dictionary<string, PrivateMessageView> _privateMessageViews;
        private readonly Dictionary<string, List<Message>> _privateMessageHistory;
        private readonly Dictionary<string, List<SharedFile>> _privateFileHistory = new Dictionary<string, List<SharedFile>>();
        private WindowResizer _windowResizer;

        public MainWindow()
        {
            InitializeComponent();
            _privateMessageViews = new Dictionary<string, PrivateMessageView>();
            _privateMessageHistory = new Dictionary<string, List<Message>>();
            _windowResizer = new WindowResizer(this);

            SubscribeCoordinatorEvents();

            InitializeFooter();
            ShowSignInView();
        }

        // ── Initialisation ───────────────────────────────────────────────────

        private void SubscribeCoordinatorEvents()
        {
            var coordinator = PollingSessionCoordinator.Instance;
            coordinator.ChannelsUpdated += OnChannelsUpdated;
            coordinator.ChannelMembersUpdated += OnChannelMembersUpdated;
            coordinator.ChannelFilesUpdated += OnChannelFilesUpdated;
            coordinator.PublicMessageReceived += OnPublicMessageReceived;
            coordinator.PrivateMessageReceived += OnPrivateMessageReceived;
            coordinator.PrivateFileReceived += OnPrivateFileReceived;
            coordinator.ConnectionStateChanged += OnConnectionStateChanged;
            coordinator.PingMsUpdated += OnPingMsUpdated;
        }

        private void InitializeFooter()
        {
            AppFooter.SignOutClicked += (s, e) => SignOut();
            AppFooter.IsLoggedIn = false;
            AppFooter.ConnectionStatus = ConnectionState.Disconnected;
        }

        private void ShowSignInView()
        {
            _serviceClient = new ChatServiceClient();
            PollingSessionCoordinator.Instance.StartSession(_serviceClient);

            var sign_in_view = new SignInView();
            sign_in_view.SetServiceClient(_serviceClient);
            sign_in_view.SignInSuccess += OnSignInSuccess;
            sign_in_view.SignInFailed += OnSignInFailed;
            MainContent.Content = sign_in_view;
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

        private void OnPublicMessageReceived(object sender, Message message)
        {
            if (message.ChannelName == PollingSessionCoordinator.Instance.CurrentChannel)
                _conversationView?.AddMessage(message);
        }

        private void OnPrivateMessageReceived(object sender, (string OtherUserId, Message Message) args)
        {
            // Store message in history
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

        private void OnConnectionStateChanged(object sender, ConnectionState state) =>
            AppFooter.ConnectionStatus = state;

        private void OnPingMsUpdated(object sender, int ping_ms) =>
            AppFooter.PingMs = ping_ms;

        // ── Sign in / out ─────────────────────────────────────────────────────

        private void OnSignInSuccess(object sender, string username)
        {
            // SignInView already called serviceClient.SignIn — just record state and continue.
            PollingSessionCoordinator.Instance.SetSignedInUser(username);

            AppFooter.CurrentUser = username;
            AppFooter.IsLoggedIn = true;
            ShowChannelListView();
        }

        private void OnSignInFailed(object sender, string username) =>
            AppFooter.ConnectionStatus = ConnectionState.Disconnected;

        private void SignOut()
        {
            ClearPrivateMessageState();
            PollingSessionCoordinator.Instance.SignOut();

            AppFooter.CurrentUser = string.Empty;
            AppFooter.IsLoggedIn = false;
            _channelListView = null;
            _conversationView = null;

            ShowSignInView();
        }

        // ── View navigation ───────────────────────────────────────────────────

        private void ShowChannelListView()
        {
            var coordinator = PollingSessionCoordinator.Instance;
            _channelListView = new ChannelListView();
            _channelListView.SetServiceClient(_serviceClient);
            _channelListView.JoinChannelRequested += OnJoinChannelRequested;
            _channelListView.CreateChannelRequested += OnCreateChannelRequested;
            _channelListView.SignOutRequested += OnSignOutRequested;
            MainContent.Content = _channelListView;
            coordinator.StartChannelListPolling();
        }

        private void ShowConversationView(string channel_name)
        {
            var coordinator = PollingSessionCoordinator.Instance;
            coordinator.StopChannelListPolling();
            
            _conversationView = new ConversationView();
            _conversationView.SetChannelName(channel_name);
            _conversationView.SetCurrentUserId(coordinator.CurrentUserId);
            _conversationView.SendMessageRequested += OnSendMessageRequested;
            _conversationView.LeaveChannelRequested += OnLeaveChannelRequested;
            _conversationView.FileDownloadRequested += OnFileDownloadRequested;
            _conversationView.FileMessageDownloadRequested += OnFileMessageDownloadRequested;
            _conversationView.PrivateMessageRequested += OnPrivateMessageRequested;
            _conversationView.FileShareRequested += OnFileShareRequested;
            _conversationView.ExportChatRequested += OnExportChatRequested;

            coordinator.RefreshChannelMembers();
            coordinator.RefreshChannelFiles();
            MainContent.Content = _conversationView;
        }

        // ── Channel list event handlers ────────────────────────────────────────

        private void OnJoinChannelRequested(object sender, string channel_name)
        {
            bool success = PollingSessionCoordinator.Instance.JoinChannel(channel_name);
            if (success)
                ShowConversationView(channel_name);
        }

        private void OnCreateChannelRequested(object sender, string channel_name)
        {
            bool success = PollingSessionCoordinator.Instance.CreateChannel(channel_name);
            if (success)
                PollingSessionCoordinator.Instance.RefreshChannels();
            else
                MessageBox.Show("Channel already exists or creation failed.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void OnSignOutRequested(object sender, EventArgs e) => SignOut();

        // ── Conversation view event handlers ──────────────────────────────────

        private void OnSendMessageRequested(object sender, string content)
        {
            var coordinator = PollingSessionCoordinator.Instance;
            coordinator.SendPublicMessage(content);

            var own_message = new Message
            {
                SenderId = coordinator.CurrentUserId,
                Content = content,
                Timestamp = DateTime.UtcNow,
                Type = MessageType.Public,
                ChannelName = coordinator.CurrentChannel,
                IsCurrentUser = true
            };
            _conversationView?.AddMessage(own_message);
        }

        private void OnLeaveChannelRequested(object sender, EventArgs e)
        {
            PollingSessionCoordinator.Instance.LeaveChannel();

            // Close all PM windows since they're only valid within the channel
            CloseAllPrivateMessageViews();

            ShowChannelListView();
        }

        private async void OnFileDownloadRequested(object sender, SharedFile file) =>
            await PollingSessionCoordinator.Instance.DownloadAndOpenFileAsync(file);

        private async void OnFileMessageDownloadRequested(object sender, Message message)
        {
            if (!message.FileId.HasValue)
                return;

            await PollingSessionCoordinator.Instance.DownloadAndOpenFileAsync(message.FileId.Value);
        }

        private async void OnFileShareRequested(object sender, EventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select a file to share",
                Filter = "All files (*.*)|*.*"
            };

            if (dialog.ShowDialog() != true)
                return;

            string file_path = dialog.FileName;
            var coordinator = PollingSessionCoordinator.Instance;
            
            string file_name = coordinator.GetFileName(file_path);

            var validation = coordinator.ValidateFile(file_path);
            if (!validation.IsValid)
            {
                MessageBox.Show(validation.ErrorMessage, "File Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            byte[] file_data = coordinator.ReadFile(file_path);
            FileType file_type = coordinator.DetermineFileType(file_name);
            bool success = await System.Threading.Tasks.Task.Run(() => coordinator.ShareFile(file_name, file_type, file_data));

            if (!success)
                return;

            coordinator.RefreshChannelFiles();

            var file_message = new Message
            {
                SenderId = coordinator.CurrentUserId,
                Content = $"Shared file: {file_name}",
                Timestamp = DateTime.UtcNow,
                Type = MessageType.File,
                ChannelName = coordinator.CurrentChannel,
                IsCurrentUser = true
            };
            _conversationView?.AddMessage(file_message);
        }

        private void OnExportChatRequested(object sender, string zipFilePath)
        {
            try
            {
                var messages = _conversationView.GetConversationItems();
                var exportService = new Chat.Client.Shared.Services.ChatExportService();
                exportService.ExportChannelChat(zipFilePath, PollingSessionCoordinator.Instance.CurrentChannel, messages, fileId => PollingSessionCoordinator.Instance.DownloadFileBytes(fileId));
                MessageBox.Show($"Chat exported successfully to:\n{zipFilePath}", "Export Chat", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to export chat: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void OnPrivateMessageRequested(object sender, string recipient_id)
        {
            if (string.Equals(recipient_id, PollingSessionCoordinator.Instance.CurrentUserId, StringComparison.OrdinalIgnoreCase))
                return;

            if (!_privateMessageViews.ContainsKey(recipient_id))
                OpenPrivateMessageView(recipient_id);
            else
                _privateMessageViews[recipient_id].Focus();
        }

        // ── Private message view management ──────────────────────────────────

        private void OpenPrivateMessageView(string recipient_id)
        {
            var view = new PrivateMessageView(recipient_id);
            view.CurrentUserId = PollingSessionCoordinator.Instance.CurrentUserId;
            view.SendMessageRequested += OnPrivateMessageSendRequested;
            view.FileUploadRequested += OnPrivateMessageFileUploadRequested;
            view.FileDownloadRequested += OnPrivateMessageFileDownloadRequested;
            view.ExportChatRequested += OnPrivateMessageExportChatRequested;
            view.Closing += OnPrivateMessageViewClosing;
            view.Owner = this;
            _privateMessageViews[recipient_id] = view;

            // Restore message history if available
            if (_privateMessageHistory.ContainsKey(recipient_id))
            {
                foreach (var message in _privateMessageHistory[recipient_id])
                {
                    view.AddMessage(message);
                }
            }
            if (_privateFileHistory.ContainsKey(recipient_id))
                foreach (var file in _privateFileHistory[recipient_id]) view.AddPendingFile(file);

            view.Show();
        }

        private void OnPrivateMessageSendRequested(object sender, string content)
        {
            var view = sender as PrivateMessageView;
            if (view == null)
                return;

            var coordinator = PollingSessionCoordinator.Instance;
            if (!coordinator.SendPrivateMessage(view.RecipientId, content))
            {
                MessageBox.Show("Private message could not be sent. The recipient may no longer be in this channel.",
                    "Private Message", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var own_message = new Message
            {
                SenderId = coordinator.CurrentUserId,
                Content = content,
                Timestamp = DateTime.UtcNow,
                Type = MessageType.Private,
                RecipientId = view.RecipientId,
                IsCurrentUser = true
            };

            // Store sent message in history
            if (!_privateMessageHistory.ContainsKey(view.RecipientId))
                _privateMessageHistory[view.RecipientId] = new List<Message>();
            _privateMessageHistory[view.RecipientId].Add(own_message);

            view.AddMessage(own_message);
            view.ClearMessageInput();
        }

        private async void OnPrivateMessageFileUploadRequested(object sender, EventArgs e)
        {
            if (!(sender is PrivateMessageView view))
                return;

            var dialog = new Microsoft.Win32.OpenFileDialog { Title = "Select a file for this private conversation", Filter = "Allowed files|*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.txt" };
            if (dialog.ShowDialog() != true)
                return;

            var coordinator = PollingSessionCoordinator.Instance;
            var validation = coordinator.ValidateFile(dialog.FileName);
            if (!validation.IsValid)
            {
                MessageBox.Show(validation.ErrorMessage, "Private file", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var sharedFile = await System.Threading.Tasks.Task.Run(() => coordinator.SharePrivateFile(view.RecipientId, coordinator.GetFileName(dialog.FileName), coordinator.DetermineFileType(dialog.FileName), coordinator.ReadFile(dialog.FileName)));
            if (sharedFile == null)
            {
                MessageBox.Show("The private file could not be shared. Both users must be in the same channel.", "Private file", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            view.AddPendingFile(sharedFile);
            if (!_privateFileHistory.ContainsKey(view.RecipientId)) _privateFileHistory[view.RecipientId] = new List<SharedFile>();
            _privateFileHistory[view.RecipientId].Add(sharedFile);
        }

        private async void OnPrivateMessageFileDownloadRequested(object sender, SharedFile file)
        {
            if (!await PollingSessionCoordinator.Instance.DownloadAndOpenPrivateFileAsync(file))
                MessageBox.Show("The file is no longer available in the current channel.", "Private file", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void OnPrivateMessageViewClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var view = sender as PrivateMessageView;
            if (view != null)
                _privateMessageViews.Remove(view.RecipientId);
        }

        private void OnPrivateMessageExportChatRequested(object sender, string zipFilePath)
        {
            var view = sender as PrivateMessageView;
            if (view == null) return;
            try
            {
                var messages = view.GetConversationItems();
                var exportService = new Chat.Client.Shared.Services.ChatExportService();
                exportService.ExportChannelChat(zipFilePath, $"Chat with {view.RecipientId}", messages, fileId => PollingSessionCoordinator.Instance.DownloadFileBytes(fileId));
                MessageBox.Show($"Chat exported successfully to:\n{zipFilePath}", "Export Chat", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to export chat: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseAllPrivateMessageViews()
        {
            var privateMessageViews = new List<PrivateMessageView>(_privateMessageViews.Values);
            foreach (var view in privateMessageViews)
                view.Close();
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
            {
                var privateMessageView = _privateMessageViews[recipientId];
                privateMessageView.Close();
                _privateMessageViews.Remove(recipientId);
            }
        }

        private void ClearPrivateMessageState()
        {
            CloseAllPrivateMessageViews();
            _privateMessageHistory.Clear();
            _privateFileHistory.Clear();
        }

        // ── Footer ────────────────────────────────────────────────────────────

        // ── Window lifecycle ──────────────────────────────────────────────────

        protected override void OnClosed(EventArgs e)
        {
            PollingSessionCoordinator.Instance.Dispose();
            _windowResizer?.Dispose();
            Application.Current.Shutdown();
            base.OnClosed(e);
        }
    }
}
