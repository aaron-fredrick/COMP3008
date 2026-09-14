using System.Collections.Generic;
using System.ServiceModel;
using Chat.Contracts.DataContracts;

namespace Chat.Contracts.CallbackContracts
{
    [ServiceContract]
    public interface IChatCallback
    {
        [OperationContract(IsOneWay = true)]
        void OnChannelListChanged(List<Channel> channels);

        [OperationContract(IsOneWay = true)]
        void OnChannelMembersChanged(string channelName, List<string> members);

        [OperationContract(IsOneWay = true)]
        void OnMessageReceived(Message message);

        [OperationContract(IsOneWay = true)]
        void OnPrivateMessageReceived(Message message);

        [OperationContract(IsOneWay = true)]
        void OnFileShared(SharedFile file);

        [OperationContract(IsOneWay = true)]
        void OnPrivateFileShared(SharedFile file);

        [OperationContract(IsOneWay = true)]
        void OnUserDisconnected(string userId);
    }
}
