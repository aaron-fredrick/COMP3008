using System;
using System.Collections.Generic;
using Chat.Contracts.DataContracts;
using Chat.Contracts.CallbackContracts;

namespace Chat.Server.StateManagement
{
    public class UserSession
    {
        public string UserId { get; set; }
        public string CurrentChannel { get; set; }
        public DateTime LastPollTime { get; set; }
        public Queue<Message> PendingPrivateMessages { get; set; }
        public IChatCallback Callback { get; set; }

        public UserSession(string userId)
        {
            UserId = userId;
            CurrentChannel = null;
            LastPollTime = DateTime.UtcNow;
            PendingPrivateMessages = new Queue<Message>();
            Callback = null;
        }
    }
}
