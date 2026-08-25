using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Channels;
using Chat.Contracts.ServiceContracts;
using Chat.Contracts.CallbackContracts;
using Chat.Contracts.DataContracts;
using Chat.Contracts.SharedTypes;
using Chat.Server.StateManagement;
using Chat.Server.FileStorage;
using Chat.Server.Logging;

namespace Chat.Server.Services
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple, UseSynchronizationContext = false)]
    public class ChatService : IChatService, IDuplexChatService
    {
        private readonly UserManager _userManager;
        private readonly ChannelManager _channelManager;
        private readonly MessageRouter _messageRouter;
        private readonly CallbackManager _callbackManager;
        private readonly FileHandler _fileHandler;
        private readonly object _membershipTransitionLock;

        public ChatService() : this(50) { }
        public ChatService(int maxMessages)