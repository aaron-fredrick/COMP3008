using System;
using System.Runtime.Serialization;
using Chat.Contracts.SharedTypes;

namespace Chat.Contracts.DataContracts
{
    [DataContract]
    public class Message : IComparable<Message>
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

        public int CompareTo(Message other)
        {
            if (other == null) return 1;
            int timestampCompare = Timestamp.CompareTo(other.Timestamp);
            if (timestampCompare != 0) return timestampCompare;
            // If timestamps are equal, use sender ID as tiebreaker
            return string.Compare(SenderId ?? "", other.SenderId ?? "", StringComparison.Ordinal);
        }
    }
}
