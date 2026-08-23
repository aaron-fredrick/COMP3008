using System;
using System.Collections.Generic;
using System.Threading;
using Chat.Contracts.DataContracts;
using Chat.Contracts.CallbackContracts;

namespace Chat.Server.StateManagement
{
    public class UserManager
    {
        private readonly Dictionary<string, UserSession> _users;
        private readonly ReaderWriterLockSlim _lock;

        public UserManager()
        {
            _users = new Dictionary<string, UserSession>();
            _lock = new ReaderWriterLockSlim();
        }

        public bool TrySignIn(string userId, out string reason)
        {
            _lock.EnterWriteLock();
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                {
                    reason = "Username cannot be empty.";
                    return false;
                }

                if (_users.ContainsKey(userId))
                {
                    reason = $"The username '{userId}' is already in use.";
                    return false;
                }

                _users[userId] = new UserSession(userId);
                reason = null;
                return true;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void SignOut(string userId)
        {
            _lock.EnterWriteLock();
            try
            {
                if (_users.ContainsKey(userId))
                {
                    _users.Remove(userId);
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public bool IsUserSignedIn(string userId)
        {
            _lock.EnterReadLock();
            try
            {
                return _users.ContainsKey(userId);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public UserSession GetUserSession(string userId)
        {
            _lock.EnterReadLock();
            try
            {
                return _users.TryGetValue(userId, out var session) ? session : null;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public void SetUserChannel(string userId, string channelName)
        {
            _lock.EnterWriteLock();
            try
            {
                if (_users.ContainsKey(userId))
                {
                    _users[userId].CurrentChannel = channelName;
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void RegisterCallback(string userId, IChatCallback callback)
        {
            _lock.EnterWriteLock();
            try
            {
                if (_users.ContainsKey(userId))
                {
                    _users[userId].Callback = callback;
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void UnregisterCallback(string userId)
        {
            _lock.EnterWriteLock();
            try
            {
                if (_users.ContainsKey(userId))
                {
                    _users[userId].Callback = null;
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public IChatCallback GetCallback(string userId)
        {
            _lock.EnterReadLock();
            try
            {
                return _users.TryGetValue(userId, out var session) ? session.Callback : null;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public void AddPendingPrivateMessage(string userId, Message message)
        {
            _lock.EnterWriteLock();
            try
            {
                if (_users.ContainsKey(userId))
                {
                    _users[userId].PendingPrivateMessages.Enqueue(message);
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public DateTime UpdateLastPollTime(string userId)
        {
            _lock.EnterWriteLock();
            try
            {
                if (_users.ContainsKey(userId))
                {
                    _users[userId].LastPollTime = DateTime.UtcNow;
                    return _users[userId].LastPollTime;
                }
                return DateTime.UtcNow;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public DateTime GetLastPollTime(string userId)
        {
            _lock.EnterReadLock();
            try
            {
                if (_users.ContainsKey(userId))
                {
                    return _users[userId].LastPollTime;
                }
                return DateTime.UtcNow;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public Queue<Message> ConsumePendingPrivateMessages(string userId)
        {
            _lock.EnterWriteLock();
            try
            {
                if (_users.ContainsKey(userId))
                {
                    var messages = _users[userId].PendingPrivateMessages;
                    _users[userId].PendingPrivateMessages = new Queue<Message>();
                    return messages;
                }
                return new Queue<Message>();
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public List<string> GetChannelMembers(string channelName)
        {
            _lock.EnterReadLock();
            try
            {
                var members = new List<string>();
                foreach (var kvp in _users)
                {
                    if (kvp.Value.CurrentChannel == channelName)
                    {
                        members.Add(kvp.Key);
                    }
                }
                return members;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public List<string> GetAllSignedInUserIds()
        {
            _lock.EnterReadLock();
            try
            {
                return new List<string>(_users.Keys);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
    }
}
