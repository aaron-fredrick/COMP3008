using System;
using System.Threading;
using Chat.Contracts.DataContracts;
using Chat.Contracts.CallbackContracts;

namespace Chat.Server.StateManagement
{
    public class CallbackManager
    {
        private readonly UserManager _userManager;
        private readonly ChannelManager _channelManager;
        private readonly ReaderWriterLockSlim _lock;

        public CallbackManager(UserManager userManager, ChannelManager channelManager)
        {
            _userManager = userManager;
            _channelManager = channelManager;
            _lock = new ReaderWriterLockSlim();
        }

        public void RegisterCallback(string userId, IChatCallback callback)
        {
            _userManager.RegisterCallback(userId, callback);
        }

        public void UnregisterCallback(string userId)
        {
            _userManager.UnregisterCallback(userId);
        }

        public void NotifyMessageReceived(string channelName, Message message)
        {
            _lock.EnterReadLock();
            try
            {
                var members = _userManager.GetChannelMembers(channelName, _channelManager);
                foreach (var memberId in members)
                {
                    var callback = _userManager.GetCallback(memberId);
                    if (callback != null)
                    {
                        try
                        {
                            callback.OnMessageReceived(message);
                        }
                        catch
                        {
                        }
                    }
                }
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public void NotifyPrivateMessageReceived(string recipientId, Message message)
        {
            _lock.EnterReadLock();
            try
            {
                var callback = _userManager.GetCallback(recipientId);
                if (callback != null)
                {
                    try
                    {
                        callback.OnPrivateMessageReceived(message);
                    }
                    catch
                    {
                    }
                }
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public void NotifyChannelListChanged()
        {
            _lock.EnterReadLock();
            try
            {
                var callbacks = _userManager;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public void NotifyChannelMembersChanged(string channelName)
        {
            _lock.EnterReadLock();
            try
            {
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public void NotifyFileShared(SharedFile file)
        {
            _lock.EnterReadLock();
            try
            {
                var callback = _userManager.GetCallback(file.UploaderId);
                if (callback != null)
                {
                    try
                    {
                        callback.OnFileShared(file);
                    }
                    catch
                    {
                    }
                }
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
    }
}
