using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Chat.Contracts.DataContracts;

namespace Chat.Client.Duplex.Views
{
    public partial class ChannelListView : Window
    {
        public event EventHandler<string> JoinChannelRequested;
        public event EventHandler<string> CreateChannelRequested;
        public event EventHandler SignOutRequested;

        private string _currentUserId;
        private bool _isConnected;
        private System.Collections.Generic.List<Channel> _allChannels;
        private string _currentSearchTerm = string.Empty;

        public static readonly DependencyProperty GridColumnsProperty =
            DependencyProperty.Register("GridColumns", typeof(int), typeof(ChannelListView),
                new PropertyMetadata(1));

        public int GridColumns
        {
            get { return (int)GetValue(GridColumnsProperty); }
            set { SetValue(GridColumnsProperty, value); }
        }

        public ChannelListView()
        {
            InitializeComponent();
            InitializeFooter();
            UpdateViewToggleButtons(false);
        }

        private void InitializeFooter()
        {
            AppFooter.SettingsClicked += AppFooter_SettingsClicked;
            UpdateFooter();
        }

        public void SetConnectionStatus(bool isConnected)
        {
            _isConnected = isConnected;
            AppFooter.ConnectionStatus = isConnected 
                ? Chat.Client.Shared.Controls.ConnectionState.Connected 
                : Chat.Client.Shared.Controls.ConnectionState.Disconnected;
        }

        private void AppFooter_SettingsClicked(object sender, EventArgs e)
        {
            MessageBox.Show("Settings view will be implemented in a future task.", "Settings", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void SetWelcomeText(string username)
        {
            _currentUserId = username;
            WelcomeText.Text = $"Welcome, {username}";
            UpdateFooter();
        }

        private void UpdateFooter()
        {
            if (!string.IsNullOrEmpty(_currentUserId))
            {
                AppFooter.CurrentUser = _currentUserId;
                AppFooter.IsLoggedIn = true;
            }
            else
            {
                AppFooter.CurrentUser = string.Empty;
                AppFooter.IsLoggedIn = false;
            }
        }

        public void UpdateChannels(System.Collections.Generic.List<Channel> channels)
        {
            _allChannels = channels;
            
            if (!string.IsNullOrEmpty(_currentSearchTerm))
            {
                var filtered = _allChannels
                    .Where(c => c.Name.ToLower().Contains(_currentSearchTerm))
                    .ToList();
                
                ChannelsGrid.ItemsSource = filtered;
                ChannelsList.ItemsSource = filtered;
            }
            else
            {
                ChannelsGrid.ItemsSource = channels;
                ChannelsList.ItemsSource = channels;
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _currentSearchTerm = SearchTextBox.Text.ToLower().Trim();
            
            if (_allChannels != null)
            {
                var filtered = _allChannels
                    .Where(c => c.Name.ToLower().Contains(_currentSearchTerm))
                    .ToList();
                
                ChannelsGrid.ItemsSource = filtered;
                ChannelsList.ItemsSource = filtered;
            }
        }

        private void ListViewButton_Click(object sender, RoutedEventArgs e)
        {
            ChannelsGrid.Visibility = Visibility.Collapsed;
            ChannelsList.Visibility = Visibility.Visible;
            UpdateViewToggleButtons(true);
        }

        private void GridViewButton_Click(object sender, RoutedEventArgs e)
        {
            ChannelsGrid.Visibility = Visibility.Visible;
            ChannelsList.Visibility = Visibility.Collapsed;
            UpdateViewToggleButtons(false);
        }

        private void UpdateViewToggleButtons(bool isListView)
        {
            var activeBrush = (Brush)FindResource("BorderBrush");
            var inactiveBrush = System.Windows.Media.Brushes.Transparent;

            if (isListView)
            {
                if (((Button)ListViewButton).Template.FindName("ButtonBorder", ListViewButton) is Border listBorder) listBorder.Background = activeBrush;
                if (((Button)GridViewButton).Template.FindName("ButtonBorder", GridViewButton) is Border gridBorder) gridBorder.Background = inactiveBrush;
            }
            else
            {
                if (((Button)GridViewButton).Template.FindName("ButtonBorder", GridViewButton) is Border gridBorder) gridBorder.Background = activeBrush;
                if (((Button)ListViewButton).Template.FindName("ButtonBorder", ListViewButton) is Border listBorder) listBorder.Background = inactiveBrush;
            }
        }

        private void ChannelCard_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is Channel channel)
            {
                JoinChannelRequested?.Invoke(this, channel.Name);
            }
        }

        private void JoinButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string channelName)
            {
                JoinChannelRequested?.Invoke(this, channelName);
            }
        }

        private void NewChannelTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
        }

        private void CreateChannelButton_Click(object sender, RoutedEventArgs e)
        {
            string channelName = NewChannelTextBox.Text.Trim();
            if (!string.IsNullOrEmpty(channelName))
            {
                CreateChannelRequested?.Invoke(this, channelName);
                NewChannelTextBox.Clear();
            }
        }

        private void SignOutButton_Click(object sender, RoutedEventArgs e)
        {
            SignOutRequested?.Invoke(this, EventArgs.Empty);
        }

        private void ChannelsGrid_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateGridColumns();
        }

        private void ChannelsGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateGridColumns();
        }

        private void UpdateGridColumns()
        {
            if (ChannelsGrid.ActualWidth > 0)
            {
                double minCardWidth = 250;
                GridColumns = Math.Max(1, (int)(ChannelsGrid.ActualWidth / minCardWidth));
            }
        }
    }
}
