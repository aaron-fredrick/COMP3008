using System.Collections.Generic;
using Chat.Contracts.DataContracts;
using Chat.Contracts.CallbackContracts;

namespace Chat.Server.StateManagement
{
    public class UserSession
    {
        public string UserId { get; set; }
        public string CurrentChannel { get; set; }
        public Queue<Message> PendingChannelMessages { get; set; }
        public Queue<Message> PendingPrivateMessages { get; set; }
        public IChatCallback Callback { get; set; }

        public UserSession(string userId)
        {
            UserId = userId;
            CurrentChannel = null;
            PendingChannelMessages = new Queue<Message>();
            PendingPrivateMessages = new Queue<Message>();
            Callback = null;
        }
    }
}
