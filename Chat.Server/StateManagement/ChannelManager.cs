using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Chat.Contracts.DataContracts;

namespace Chat.Server.StateManagement
{
    public class ChannelManager
    {
        private readonly Dictionary<string, Channel> _channels;
        private readonly ReaderWriterLockSlim _lock;

        public ChannelManager()
        {
            _channels = new Dictionary<string, Channel>();
            _lock = new ReaderWriterLockSlim();
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
            try
            {
                return _channels.ContainsKey(channelName);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public List<Channel> GetChannels()
        {
            _lock.EnterReadLock();
            try
            {
                return _channels.Values.Select(c => new Channel
                {
                    Name = c.Name,
                    Members = new List<string>(c.Members)
                }).ToList();
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public List<string> GetChannelMembers(string channelName)
        {
            _lock.EnterReadLock();
            try
            {
                if (_channels.ContainsKey(channelName))
                {
                    return new List<string>(_channels[channelName].Members);
                }
                return new List<string>();
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public bool JoinChannel(string userId, string channelName, out string previousChannel)
        {
            _lock.EnterWriteLock();
            try
            {
                previousChannel = null;

                if (!_channels.ContainsKey(channelName))
                {
                    return false;
                }

                var channel = _channels[channelName];

                if (channel.Members.Contains(userId))
                {
                    return true;
                }

                channel.Members.Add(userId);
                return true;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void LeaveChannel(string userId, string channelName)
        {
            _lock.EnterWriteLock();
            try
            {
                if (_channels.ContainsKey(channelName))
                {
                    _channels[channelName].Members.Remove(userId);
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void RemoveUserFromAllChannels(string userId)
        {
            _lock.EnterWriteLock();
            try
            {
                foreach (var channel in _channels.Values)
                {
                    channel.Members.Remove(userId);
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }
    }
}
