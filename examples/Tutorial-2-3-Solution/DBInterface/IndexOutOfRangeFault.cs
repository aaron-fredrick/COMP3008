// Alex Starling - Distributed Computing - 2021
using System.Runtime.Serialization;

namespace DBInterface
{
    
    // This is just a basic fault so you can get an idea on how to use them
    [DataContract]
    public class IndexOutOfRangeFault
    {
        [DataMember]
        public string Issue { get; set; }
    }
}
