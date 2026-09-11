using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Chat.Client.Shared.Services;

namespace Chat.Client.Shared.Controls
{
    public partial class ApplicationFooter : UserControl
    {
        public static readonly DependencyProperty CurrentUserProperty =
            DependencyProperty.Register(nameof(CurrentUser), typeof(string), typeof(ApplicationFooter),
                new PropertyMetadata(string.Empty, OnCurrentUserChanged));

        public static readonly DependencyProperty IsLoggedInProperty =
            DependencyProperty.Register(nameof(IsLoggedIn), typeof(bool), typeof(ApplicationFooter),
                new PropertyMetadata(false, OnIsLoggedInChanged));

        public static readonly DependencyProperty ConnectionStatusProperty =
            DependencyProperty.Register(nameof(ConnectionStatus), typeof(ConnectionState), typeof(ApplicationFooter),
                new PropertyMetadata(ConnectionState.Disconnected, OnConnectionStatusChanged));

        public static readonly DependencyProperty PingMsProperty =
            DependencyProperty.Register(nameof(PingMs), typeof(int), typeof(ApplicationFooter),
                new PropertyMetadata(0, OnPingMsChanged));

        public string CurrentUser
        {
            get => (string)GetValue(CurrentUserProperty);
            set => SetValue(CurrentUserProperty, value);
        }

        public bool IsLoggedIn
        {
            get => (bool)GetValue(IsLoggedInProperty);
            set => SetValue(IsLoggedInProperty, value);
        }

        public ConnectionState ConnectionStatus
        {
            get => (ConnectionState)GetValue(ConnectionStatusProperty);
            set => SetValue(ConnectionStatusProperty, value);
        }

        public int PingMs
        {
            get => (int)GetValue(PingMsProperty);
            set => SetValue(PingMsProperty, value);
        }

        public ApplicationFooter()
        {
            InitializeComponent();
            UpdateUserStatus();
            UpdateConnectionStatus();
            UpdatePing();
        }

        private static void OnCurrentUserChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var footer = (ApplicationFooter)d;
            footer.UpdateUserStatus();
        }

        private static void OnIsLoggedInChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var footer = (ApplicationFooter)d;
            footer.UpdateUserStatus();
        }

        private static void OnConnectionStatusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var footer = (ApplicationFooter)d;
            footer.UpdateConnectionStatus();
        }

        private static void OnPingMsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var footer = (ApplicationFooter)d;
            footer.UpdatePing();
        }

        private void UpdateUserStatus()
        {
            if (IsLoggedIn && !string.IsNullOrEmpty(CurrentUser))
            {
                UserStatusText.Text = CurrentUser;
                UserStatusText.Foreground = new SolidColorBrush(Color.FromRgb(76, 175, 80)); // Green
            }
            else
            {
                UserStatusText.Text = "Not logged in";
                UserStatusText.Foreground = new SolidColorBrush(Color.FromRgb(255, 152, 0)); // Orange
            }
        }

        private void UpdateConnectionStatus()
        {
            switch (ConnectionStatus)
            {
                case ConnectionState.Connected:
                    ConnectionStatusText.Text = "Connected";
                    ConnectionStatusText.Foreground = new SolidColorBrush(Color.FromRgb(76, 175, 80)); // Green
                    break;
                case ConnectionState.Connecting:
                    ConnectionStatusText.Text = "Connecting...";
                    ConnectionStatusText.Foreground = new SolidColorBrush(Color.FromRgb(255, 152, 0)); // Orange
                    break;
                case ConnectionState.Disconnected:
                default:
                    ConnectionStatusText.Text = "Disconnected";
                    ConnectionStatusText.Foreground = new SolidColorBrush(Color.FromRgb(255, 82, 82)); // Red
                    break;
            }
        }

        private void UpdatePing()
        {
            if (PingMs > 0 && ConnectionStatus == ConnectionState.Connected)
            {
                PingText.Text = $"● {PingMs} ms";
            }
            else
            {
                PingText.Text = "";
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new EndpointSettingsDialog
            {
                Owner = Window.GetWindow(this)
            };

            if (dialog.ShowDialog() == true)
            {
                // Endpoint changes invalidate the current WCF client.
                // The existing parent sign-out path tears down the session and returns to login.
                SignOutClicked?.Invoke(this, EventArgs.Empty);
            }
        }


        public event EventHandler SignOutClicked;

        private void SignOutButton_Click(object sender, RoutedEventArgs e)
        {
            UserMenuButton.IsChecked = false;
            SignOutClicked?.Invoke(this, EventArgs.Empty);
        }
    }

    public enum ConnectionState
    {
        Disconnected,
        Connecting,
        Connected
    }
}
