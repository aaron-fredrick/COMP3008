using System;
using System.ComponentModel;

namespace Chat.Client.Shared.ViewModels
{
    public abstract class ConversationItemViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public abstract DateTime Timestamp { get; }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
