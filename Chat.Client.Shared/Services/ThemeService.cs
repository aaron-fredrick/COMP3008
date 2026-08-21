using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Chat.Client.Shared.Services
{
    public class ThemeService : INotifyPropertyChanged
    {
        private static ThemeService _instance;
        public static ThemeService Instance => _instance ?? (_instance = new ThemeService());

        private bool _isDarkTheme = true;
        private bool _forceLightTheme = false;

        public bool IsDarkTheme
        {
            get => _isDarkTheme && !_forceLightTheme;
            set
            {
                if (_isDarkTheme != value)
                {
                    _isDarkTheme = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool ForceLightTheme
        {
            get => _forceLightTheme;
            set
            {
                if (_forceLightTheme != value)
                {
                    _forceLightTheme = value;
                    OnPropertyChanged(nameof(IsDarkTheme));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void ToggleTheme()
        {
            if (!_forceLightTheme)
            {
                IsDarkTheme = !IsDarkTheme;
            }
        }

        public void SetTheme(bool isDark)
        {
            if (!_forceLightTheme)
            {
                IsDarkTheme = isDark;
            }
        }
    }
}
