using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Chat.Client.Shared.Services;

namespace Chat.Client.Shared.Controls
{
    public partial class CustomTitleBar : UserControl
    {
        private readonly ThemeService _themeService;

        public CustomTitleBar()
        {
            InitializeComponent();
            _themeService = ThemeService.Instance;
            _themeService.PropertyChanged += OnThemeChanged;
            ApplyTheme();
        }

        private void OnThemeChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ThemeService.IsDarkTheme))
            {
                ApplyTheme();
            }
        }

        private void ApplyTheme()
        {
            if (_themeService.IsDarkTheme)
            {
                // Dark theme
                BackgroundBrush = new SolidColorBrush(Color.FromRgb(45, 45, 45)); // #2d2d2d
                ForegroundBrush = new SolidColorBrush(Colors.White);
            }
            else
            {
                // Light theme
                BackgroundBrush = new SolidColorBrush(Color.FromRgb(245, 245, 245)); // #f5f5f5
                ForegroundBrush = new SolidColorBrush(Colors.Black);
            }
        }

        public SolidColorBrush BackgroundBrush
        {
            get => (SolidColorBrush)GetValue(BackgroundBrushProperty);
            set => SetValue(BackgroundBrushProperty, value);
        }

        public static readonly DependencyProperty BackgroundBrushProperty =
            DependencyProperty.Register(nameof(BackgroundBrush), typeof(SolidColorBrush), typeof(CustomTitleBar));

        public SolidColorBrush ForegroundBrush
        {
            get => (SolidColorBrush)GetValue(ForegroundBrushProperty);
            set => SetValue(ForegroundBrushProperty, value);
        }

        public static readonly DependencyProperty ForegroundBrushProperty =
            DependencyProperty.Register(nameof(ForegroundBrush), typeof(SolidColorBrush), typeof(CustomTitleBar));

        private void TitleText_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                var window = Window.GetWindow(this);
                if (window != null)
                {
                    if (window.WindowState == WindowState.Maximized)
                    {
                        window.WindowState = WindowState.Normal;
                    }
                    else
                    {
                        window.WindowState = WindowState.Maximized;
                    }
                }
            }
            else
            {
                var window = Window.GetWindow(this);
                if (window != null)
                {
                    window.DragMove();
                }
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(this);
            if (window != null)
            {
                window.WindowState = WindowState.Minimized;
            }
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(this);
            if (window != null)
            {
                if (window.WindowState == WindowState.Maximized)
                {
                    window.WindowState = WindowState.Normal;
                }
                else
                {
                    window.WindowState = WindowState.Maximized;
                }
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(this);
            if (window != null)
            {
                window.Close();
            }
        }
    }
}
