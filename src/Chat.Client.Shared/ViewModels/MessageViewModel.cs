using System;
using System.ComponentModel;
using Chat.Contracts.DataContracts;
using Chat.Contracts.SharedTypes;

namespace Chat.Client.Shared.ViewModels
{
    public class MessageViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public Message Message { get; }

        /// <summary>
        /// True if this message is the first in a continuous block from the same user within the same minute.
        /// Determines whether to show the avatar and timestamp/username header.
        /// Mutable so that the previous message's flag can be updated in-place when a new consecutive
        /// message arrives from the same sender, without rebuilding the entire collection.
        /// </summary>
        private bool _showMetadata;
        public bool ShowMetadata
        {
            get => _showMetadata;
            set
            {
                if (_showMetadata == value) return;
                _showMetadata = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowMetadata)));
            }
        }

        public MessageViewModel(Message message, bool showMetadata)
        {
            Message = message;
            _showMetadata = showMetadata;
        }

        public string SenderId => Message.SenderId;
        public string Content => Message.Content;
        public MessageType Type => Message.Type;
        public bool IsCurrentUser => Message.IsCurrentUser;
        public Guid? FileId => Message.FileId;

        // Convert the server's UTC/local timestamp to the client's local time offset
        public DateTime Timestamp => Message.Timestamp.ToLocalTime();

        public string HeaderText
        {
            get
            {
                if (IsCurrentUser)
                    return Timestamp.ToString("HH:mm");
                else
                    return $"{SenderId} · {Timestamp.ToString("HH:mm")}";
            }
        }
    }
}
