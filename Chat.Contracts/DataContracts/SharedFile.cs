using System;
using System.Runtime.Serialization;
using Chat.Contracts.SharedTypes;

namespace Chat.Contracts.DataContracts
{
    [DataContract]
    public class SharedFile
    {
        [DataMember]
        public Guid FileId { get; set; }

        [DataMember]
        public string FileName { get; set; }

        [DataMember]
        public FileType FileType { get; set; }

        [DataMember]
        public long FileSize { get; set; }

        [DataMember]
        public string UploaderId { get; set; }

        [DataMember]
        public DateTime UploadedAt { get; set; }

        [DataMember]
        public string ChannelName { get; set; }

        // Null for channel files; populated for a private file share.
        [DataMember]
        public string RecipientId { get; set; }

        [DataMember]
        public byte[] FileData { get; set; }
    }
}
