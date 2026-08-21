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

        // Dependency property for dynamic card width
        public static readonly DependencyProperty CardWidthProperty =
            DependencyProperty.Register("CardWidth", typeof(double), typeof(ChannelListView),
                new PropertyMetadata(250.0));

        public double CardWidth
        {
            get { return (double)GetValue(CardWidthProperty); }
            set { SetValue(CardWidthProperty, value); }
        }

        public ChannelListView()
        {
            InitializeComponent();
            UpdateViewToggleButtons(false); // Initialize with grid view active (matches HTML default)
        }

        public void SetServiceClient(ChatServiceClient serviceClient)
        {
            _serviceClient = serviceClient;
            LoadChannels();
        }

        public void UpdateChannels(System.Collections.Generic.List<Channel> channels)
        {
            _allChannels = channels;
            
            // Apply current search filter if there is one
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

        private void LoadChannels()
        {
            if (_serviceClient != null)
            {
                var channels = _serviceClient.GetChannels();
                UpdateChannels(channels);
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
            if (isListView)
            {
                // Highlight list view button
                var listBorder = ((Button)ListViewButton).Template.FindName("ButtonBorder", ListViewButton) as Border;
                if (listBorder != null)
                {
                    listBorder.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255));
                }
                
                // Reset grid view button
                var gridBorder = ((Button)GridViewButton).Template.FindName("ButtonBorder", GridViewButton) as Border;
                if (gridBorder != null)
                {
                    gridBorder.Background = System.Windows.Media.Brushes.Transparent;
                }
            }
            else
            {
                // Highlight grid view button
                var gridBorder = ((Button)GridViewButton).Template.FindName("ButtonBorder", GridViewButton) as Border;
                if (gridBorder != null)
                {
                    gridBorder.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255));
                }
                
                // Reset list view button
                var listBorder = ((Button)ListViewButton).Template.FindName("ButtonBorder", ListViewButton) as Border;
                if (listBorder != null)
                {
                    listBorder.Background = System.Windows.Media.Brushes.Transparent;
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
            // Placeholder visibility is handled by XAML triggers
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
            AdjustMarginsForLastItems();
        }

        private void ChannelsGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateGridColumns();
            AdjustMarginsForLastItems();
        }

        private void AdjustMarginsForLastItems()
        {
            var wrapPanel = FindVisualChild<WrapPanel>(ChannelsGrid);
            if (wrapPanel != null && wrapPanel.ActualWidth > 0)
            {
                double currentX = 0;
                
                foreach (UIElement child in wrapPanel.Children)
                {
                    if (child is ContentPresenter cp)
                    {
                        double childWidth = cp.ActualWidth;
                        double childRight = currentX + childWidth;
                        
                        // If this child is at or near the right edge, remove right margin
                        if (childRight >= wrapPanel.ActualWidth - 10) // 10px tolerance
                        {
                            cp.Margin = new Thickness(0, 0, 0, 6);
                        }
                        else
                        {
                            cp.Margin = new Thickness(0, 0, 6, 6);
                        }
                        
                        currentX += childWidth + 6; // 6px gap
                        
                        // If we've wrapped to the next line
                        if (currentX > wrapPanel.ActualWidth)
                        {
                            currentX = 0;
                        }
                    }
                }
            }
        }

        private static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T result)
                {
                    return result;
                }
                
                var childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null)
                {
                    return childOfChild;
                }
            }
            return null;
        }

        private void UpdateGridColumns()
        {
            if (ChannelsGrid.ActualWidth > 0)
            {
                // Calculate number of columns based on available width
                // Minimum card width of 500px (twice the previous 250px), with 12px gap (6px on right of each item except last in row)
                double minCardWidth = 500;
                double gap = 12;
                int columns = Math.Max(1, (int)((ChannelsGrid.ActualWidth + gap) / (minCardWidth + gap)));
                
                // Calculate card width based on columns
                // Only account for gaps between columns (columns - 1 gaps)
                double cardWidth = (ChannelsGrid.ActualWidth - ((columns - 1) * gap)) / columns;
                
                // Update the CardWidth dependency property
                CardWidth = cardWidth;
            }
        }
    }
}
