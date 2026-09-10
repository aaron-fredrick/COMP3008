using System.Runtime.Serialization;

namespace Chat.Contracts.DataContracts
{
    [DataContract]
    public class User
    {
        [DataMember]
        public string UserId { get; set; }

        [DataMember]
        public string CurrentChannel { get; set; }
    }
}
