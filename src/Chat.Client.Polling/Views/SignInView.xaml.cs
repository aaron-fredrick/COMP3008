using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Chat.Client.Polling.Services;
using Chat.Client.Shared.Services;

namespace Chat.Client.Polling.Views
{
    public partial class SignInView : UserControl
    {
        private ChatServiceClient _serviceClient;
        private ValidationService _validationService;

        public event EventHandler<string> SignInSuccess;
        public event EventHandler<string> SignInFailed;

        public SignInView()
        {
            InitializeComponent();
            _validationService = new ValidationService();
        }

        public void SetServiceClient(ChatServiceClient serviceClient)
        {
            _serviceClient = serviceClient;
        }

        public string Username => UsernameTextBox.Text;

        public void SetStatus(string status)
        {
            LoginStatusText.Text = status;
        }

        public void SetSignInEnabled(bool enabled)
        {
            SignInButton.IsEnabled = enabled;
        }

        private async void SignInButton_Click(object sender, RoutedEventArgs e)
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
                if (_serviceClient == null)
                {
                    SetStatus("Service client not initialized. Please restart the application.");
                    SetSignInEnabled(true);
                    return;
                }

                bool success = await System.Threading.Tasks.Task.Run(() => _serviceClient.SignIn(username));

                if (success)
                {
                    SignInSuccess?.Invoke(this, username);
                }
                else
                {
                    SetStatus("Sign in failed. Username may already be in use.");
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
            {
                SignInButton_Click(sender, e);
            }
        }
    }
}
