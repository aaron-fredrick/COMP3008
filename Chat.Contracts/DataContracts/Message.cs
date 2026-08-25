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

        [DataMember]
        public Guid? FileId { get; set; }

        // Server-assigned, monotonically increasing sequence within a channel.
        // Used by polling clients as a deterministic delivery boundary.
        [DataMember]
        public long Sequence { get; set; }

        public bool IsCurrentUser { get; set; }

        public int CompareTo(Message other)
        {
            if (other == null) return 1;
            int timestampCompare = Timestamp.CompareTo(other.Timestamp);
            if (timestampCompare != 0) return timestampCompare;
            int sequenceCompare = Sequence.CompareTo(other.Sequence);
            if (sequenceCompare != 0) return sequenceCompare;
            return string.Compare(SenderId ?? "", other.SenderId ?? "", StringComparison.Ordinal);
        }
    }
}
