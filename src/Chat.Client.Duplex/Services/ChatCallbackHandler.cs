using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.Windows.Threading;
using Chat.Contracts.CallbackContracts;
using Chat.Contracts.DataContracts;

namespace Chat.Client.Duplex.Services
{
    [CallbackBehavior(UseSynchronizationContext = false, ConcurrencyMode = ConcurrencyMode.Multiple)]
    public class ChatCallbackHandler : IChatCallback
    {
        private readonly Dispatcher _dispatcher;
        private readonly DuplexServiceClient _serviceClient;

        public ChatCallbackHandler(Dispatcher dispatcher, DuplexServiceClient serviceClient)
        {
            _dispatcher = dispatcher;
            _serviceClient = serviceClient;
        }

        // TODO: Before production release, verify WPF Dispatcher responsiveness during a deliberately slow WCF operation.
        // This is a deferred verification task; do not add artificial production delays for this check.
        public void OnChannelListChanged(List<Channel> channels)
        {
            _dispatcher.BeginInvoke(new Action(() =>
            {
                _serviceClient.OnChannelListChangedInternal(channels);
            }));
        }

        public void OnChannelMembersChanged(string channelName, List<string> members)
        {
            _dispatcher.BeginInvoke(new Action(() =>
            {
                _serviceClient.OnChannelMembersChangedInternal(channelName, members);
            }));
        }

        public void OnMessageReceived(Message message)
        {
            _dispatcher.BeginInvoke(new Action(() =>
            {
                _serviceClient.OnMessageReceivedInternal(message);
            }));
        }

        public void OnPrivateMessageReceived(Message message)
        {
            _dispatcher.BeginInvoke(new Action(() =>
            {
                _serviceClient.OnPrivateMessageReceivedInternal(message);
            }));
        }

        public void OnFileShared(SharedFile file)
        {
            _dispatcher.BeginInvoke(new Action(() =>
            {
                _serviceClient.OnFileSharedInternal(file);
            }));
        }

        public void OnPrivateFileShared(SharedFile file)
        {
            _dispatcher.BeginInvoke(new Action(() =>
            {
                _serviceClient.OnPrivateFileSharedInternal(file);
            }));
        }

        public void OnUserDisconnected(string userId)
        {
            _dispatcher.BeginInvoke(new Action(() =>
            {
                _serviceClient.OnUserDisconnectedInternal(userId);
            }));
        }
    }
}
