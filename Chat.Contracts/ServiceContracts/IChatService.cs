using System;
using System.Collections.Generic;
using System.ServiceModel;
using Chat.Contracts.DataContracts;

namespace Chat.Contracts.ServiceContracts
{
    [ServiceContract]
    public interface IChatService
    {
        [OperationContract]
        bool SignIn(string userId);

        [OperationContract]
        void SignOut(string userId);

        [OperationContract]
        List<Channel> GetChannels();

        [OperationContract]
        bool CreateChannel(string channelName);

        [OperationContract]
        bool JoinChannel(string userId, string channelName);

        [OperationContract]
        void LeaveChannel(string userId);

        [OperationContract]
        List<string> GetChannelMembers(string channelName);

        [OperationContract]
        void SendMessage(string senderId, string channelName, string content);

        [OperationContract]
        void SendPrivateMessage(string senderId, string recipientId, string content);

        [OperationContract]
        bool ShareFile(string uploaderId, string channelName, string fileName, FileType fileType, byte[] fileData);

        [OperationContract]
        SharedFile GetFile(string channelName, string fileName);
    }
}
