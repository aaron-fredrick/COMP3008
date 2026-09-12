using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Chat.Contracts.DataContracts;
using Chat.Client.Polling.Services;

namespace Chat.Client.Polling.Views
{
    public partial class ChannelListView : UserControl
    {
        public event EventHandler<string> JoinChannelRequested;
        public event EventHandler<string> CreateChannelRequested;
        public event EventHandler SignOutRequested;

        private ChatServiceClient _serviceClient;
        private System.Collections.Generic.List<Channel> _allChannels;
        private string _currentSearchTerm = string.Empty;

        public ChannelListView()
        {
            InitializeComponent();
            UpdateViewToggleButtons(false);
        }

        public void SetServiceClient(ChatServiceClient serviceClient)
        {
            _serviceClient = serviceClient;
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
                if (((Button)ListViewButton).Template.FindName("ButtonBorder", ListViewButton) is Border listBorder)
                {
                    listBorder.Background = activeBrush;
                }
                
                if (((Button)GridViewButton).Template.FindName("ButtonBorder", GridViewButton) is Border gridBorder)
                {
                    gridBorder.Background = inactiveBrush;
                }
            }
            else
            {
                if (((Button)GridViewButton).Template.FindName("ButtonBorder", GridViewButton) is Border gridBorder)
                {
                    gridBorder.Background = activeBrush;
                }
                
                if (((Button)ListViewButton).Template.FindName("ButtonBorder", ListViewButton) is Border listBorder)
                {
                    listBorder.Background = inactiveBrush;
                }
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

        public static readonly DependencyProperty GridColumnsProperty =
            DependencyProperty.Register("GridColumns", typeof(int), typeof(ChannelListView),
                new PropertyMetadata(1));

        public int GridColumns
        {
            get { return (int)GetValue(GridColumnsProperty); }
            set { SetValue(GridColumnsProperty, value); }
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
