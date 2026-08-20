using Chat.Contracts.DataContracts;
using Chat.Contracts.SharedTypes;

namespace Chat.Client.Shared.Models
{
    public class MessageDisplayModel
    {
        public Message Message { get; set; }
        public bool IsOwnMessage { get; set; }
        public bool IsPrivateMessage { get; set; }

        public MessageDisplayModel(Message message, string currentUserId)
        {
            Message = message;
            IsOwnMessage = message.SenderId == currentUserId;
            IsPrivateMessage = message.Type == MessageType.Private;
        }
    }
}
