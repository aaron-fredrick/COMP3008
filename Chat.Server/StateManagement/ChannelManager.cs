using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Chat.Contracts.DataContracts;
using Chat.Server.Logging;

namespace Chat.Server.StateManagement
{
    public class ChannelManager
    {
        private readonly Dictionary<string, Channel> _channels;
        private readonly Dictionary<string, Queue<Message>> _channelMessages;
        private readonly Dictionary<string, long> _channelSequences;
        private readonly ReaderWriterLockSlim _lock;
        private readonly int _maxMessages;

        public ChannelManager(int maxMessages = 50)
        {
            _channels = new Dictionary<string, Channel>();
            _channelMessages = new Dictionary<string, Queue<Message>>();
            _channelSequences = new Dictionary<string, long>();
            _lock = new ReaderWriterLockSlim();
            _maxMessages = maxMessages;
        }

        public bool TryCreateChannel(string channelName, out string reason)
        {
            _lock.EnterWriteLock();
            try
            {
                if (string.IsNullOrWhiteSpace(channelName))
                {
                    reason = "Channel name cannot be empty.";
                    return false;
                }

                if (_channels.ContainsKey(channelName))
                {
                    reason = $"A channel named '{channelName}' already exists.";
                    return false;
                }

                _channels[channelName] = new Channel
                {
                    Name = channelName,
                    Members = new List<string>()
                };
                _channelMessages[channelName] = new Queue<Message>();
                _channelSequences[channelName] = 0;

                reason = null;
                return true;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public bool ChannelExists(string channelName)
        {
            _lock.EnterReadLock();
            try { return _channels.ContainsKey(channelName); }
            finally { _lock.ExitReadLock(); }
        }

        public List<Channel> GetChannels()
        {
            _lock.EnterReadLock();
            try
            {
                return _channels.Values.Select(c => new Channel
                {
                    Name = c.Name,
                    Members = new List<string>(c.Members),
                    UserCount = c.Members.Count
                }).ToList();
            }
            finally { _lock.ExitReadLock(); }
        }

        public List<string> GetChannelMembers(string channelName)
        {
            _lock.EnterReadLock();
            try
            {
                return _channels.ContainsKey(channelName)
                    ? new List<string>(_channels[channelName].Members)
                    : new List<string>();
            }
            finally { _lock.ExitReadLock(); }
        }

        public bool JoinChannel(string userId, string channelName, out string previousChannel)
        {
            _lock.EnterWriteLock();
            try
            {
                previousChannel = null;
                if (!_channels.ContainsKey(channelName)) return false;
                var channel = _channels[channelName];
                if (channel.Members.Contains(userId)) return true;
                channel.Members.Add(userId);
                return true;
            }
            finally { _lock.ExitWriteLock(); }
        }

        public void LeaveChannel(string userId, string channelName)
        {
            _lock.EnterWriteLock();
            try
            {
                if (_channels.ContainsKey(channelName))
                    _channels[channelName].Members.Remove(userId);
            }
            finally { _lock.ExitWriteLock(); }
        }

        public void RemoveUserFromAllChannels(string userId)
        {
            _lock.EnterWriteLock();
            try
            {
                foreach (var channel in _channels.Values) channel.Members.Remove(userId);
            }
            finally { _lock.ExitWriteLock(); }
        }

        public long GetCurrentSequence(string channelName)
        {
            _lock.EnterReadLock();
            try
            {
                return _channelSequences.ContainsKey(channelName) ? _channelSequences[channelName] : 0;
            }
            finally { _lock.ExitReadLock(); }
        }

        public void AddChannelMessage(string channelName, Message message)
        {
            _lock.EnterWriteLock();
            try
            {
                if (!_channelMessages.ContainsKey(channelName)) return;

                long sequence = ++_channelSequences[channelName];
                message.Sequence = sequence;
                var queue = _channelMessages[channelName];

                if (queue.Count >= _maxMessages)
                {
                    var evicted = queue.Dequeue();
                    ServerLogger.Info(
                        $"[QUEUE] #{channelName} at capacity ({_maxMessages}). " +
                        $"Evicted oldest message from '{evicted.SenderId}' sent at {evicted.Timestamp:HH:mm:ss}.");
                }

                queue.Enqueue(message);
                ServerLogger.Info(
                    $"[QUEUE] #{channelName} +1 message from '{message.SenderId}'. " +
                    $"Queue size: {queue.Count}/{_maxMessages}.");
            }
            finally { _lock.ExitWriteLock(); }
        }

        public List<Message> GetMessagesSince(string channelName, DateTime since, string requestingUserId)
        {
            _lock.EnterReadLock();
            try
            {
                if (!_channelMessages.ContainsKey(channelName)) return new List<Message>();
                var channel = _channels[channelName];
                if (!channel.Members.Contains(requestingUserId)) return new List<Message>();

                return _channelMessages[channelName]
                    .Where(m => m.Timestamp > since && m.SenderId != requestingUserId)
                    .ToList();
            }
            finally { _lock.ExitReadLock(); }
        }

        public List<Message> GetMessagesSinceSequence(string channelName, long sequence, string requestingUserId)
        {
            _lock.EnterReadLock();
            try
            {
                if (!_channelMessages.ContainsKey(channelName)) return new List<Message>();
                var channel = _channels[channelName];
                if (!channel.Members.Contains(requestingUserId)) return new List<Message>();

                return _channelMessages[channelName]
                    .Where(m => m.Sequence > sequence && m.SenderId != requestingUserId)
                    .ToList();
            }
            finally { _lock.ExitReadLock(); }
        }
    }
}
