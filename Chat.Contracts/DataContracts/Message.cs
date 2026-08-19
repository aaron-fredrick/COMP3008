using System;
using System.Runtime.Serialization;
using Chat.Contracts.SharedTypes;

namespace Chat.Contracts.DataContracts
{
    [DataContract]
    public class Message
    {
        [DataMember]
        public string SenderId { get; set; }

        [DataMember]
        public string Content { get; set; }

        [DataMember]
        public DateTime Timestamp { get; set; }

        [DataMember]
        public MessageType Type { get; set; }

        [DataMember]
        public string ChannelName { get; set; }

        [DataMember]
        public string RecipientId { get; set; }
    }
}
