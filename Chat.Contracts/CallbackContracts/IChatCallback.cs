using System;
using System.ServiceModel;
using Chat.Contracts.DataContracts;

namespace Chat.Contracts.CallbackContracts
{
    [ServiceContract]
    public interface IChatCallback
    {
        [OperationContract(IsOneWay = true)]
        void OnChannelListChanged();

        [OperationContract(IsOneWay = true)]
        void OnChannelMembersChanged(string channelName);

        [OperationContract(IsOneWay = true)]
        void OnMessageReceived(Message message);

        [OperationContract(IsOneWay = true)]
        void OnPrivateMessageReceived(Message message);

        [OperationContract(IsOneWay = true)]
        void OnFileShared(SharedFile file);
    }
}
