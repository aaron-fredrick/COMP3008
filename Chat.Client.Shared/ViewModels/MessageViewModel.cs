using System;
using Chat.Contracts.DataContracts;
using Chat.Contracts.SharedTypes;

namespace Chat.Client.Shared.ViewModels
{
    public class MessageViewModel
    {
        public Message Message { get; }

        /// <summary>
        /// True if this message is the first in a continuous block from the same user within the same minute.
        /// Determines whether to show the avatar and timestamp/username header.
        /// </summary>
        public bool ShowMetadata { get; }

        public MessageViewModel(Message message, bool showMetadata)
        {
            Message = message;
            ShowMetadata = showMetadata;
        }

        public string SenderId => Message.SenderId;
        public string Content => Message.Content;
        public MessageType Type => Message.Type;
        public bool IsCurrentUser => Message.IsCurrentUser;

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
