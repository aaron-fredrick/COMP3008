using System;
using System.Windows.Controls;
using System.Windows.Input;
using Chat.Client.Duplex.Services;
using Chat.Client.Shared.Services;

namespace Chat.Client.Duplex.Views
{
    public partial class SignInView : UserControl
    {
        public event EventHandler<string> SignInSuccess;
        public event EventHandler<string> SignInFailed;

        private readonly ValidationService _validationService;

        public SignInView()
        {
            InitializeComponent();
            _validationService = new ValidationService();
        }

        public void SetStatus(string status)
        {
            LoginStatusText.Text = status;
        }

        public void SetSignInEnabled(bool enabled)
        {
            SignInButton.IsEnabled = enabled;
        }

        private async void SignInButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            string username = UsernameTextBox.Text.Trim();

            var validationResult = _validationService.ValidateUsername(username);
            if (!validationResult.IsValid)
            {
                SetStatus(validationResult.ErrorMessage);
                return;
            }

            SetStatus("Connecting to server...");
            SetSignInEnabled(false);

            try
            {
                var coordinator = DuplexSessionCoordinator.Instance;
                coordinator.StartSession(Dispatcher);

                bool success = await coordinator.SignInAsync(username);

                if (success)
                {
                    SignInSuccess?.Invoke(this, username);
                }
                else
                {
                    SetStatus("Sign in failed. Username may already be in use or invalid.");
                    SetSignInEnabled(true);
                    SignInFailed?.Invoke(this, username);
                }
            }
            catch (Exception ex)
            {
                SetStatus($"Server not responding: {ex.Message}");
                SetSignInEnabled(true);
                SignInFailed?.Invoke(this, username);
            }
        }

        private void UsernameTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                SignInButton_Click(sender, e);
        }
    }
}
