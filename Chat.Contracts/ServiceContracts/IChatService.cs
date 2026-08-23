using System;
using System.Collections.Generic;
using System.ServiceModel;
using Chat.Contracts.DataContracts;
using Chat.Contracts.SharedTypes;

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
        bool SendPrivateMessage(string senderId, string recipientId, string content);

        [OperationContract]
        bool ShareFile(string uploaderId, string channelName, string fileName, FileType fileType, byte[] fileData);

        [OperationContract]
        SharedFile GetFile(string userId, Guid fileId);

        [OperationContract]
        List<SharedFile> GetChannelFiles(string channelName);

        [OperationContract]
        List<Message> GetPendingMessages(string userId);

        [OperationContract]
        List<Message> GetPendingPrivateMessages(string userId);

        [OperationContract]
        string Ping(string userId, byte[] hash);
    }
}
