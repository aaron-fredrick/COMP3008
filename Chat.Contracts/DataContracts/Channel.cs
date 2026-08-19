using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Chat.Contracts.DataContracts
{
    [DataContract]
    public class Channel
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public List<string> Members { get; set; }
    }
}
