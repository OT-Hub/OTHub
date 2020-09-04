using System.Collections.Generic;
using Newtonsoft.Json;

namespace OTHub.BackendSync.Nodes.Models
{

    //public class NodeContactWrapper
    //{

    //    public NodeContact[] contact { get; set; }


    //    public string header { get; set; }
    //}

    //    public class NodeContact
    //    {
    //        public string agent { get; set; }
    //        public string hostname { get; set; }
    //        public int index { get; set; }
    //        public string network_id { get; set; }
    //        public int port { get; set; }
    //        public string protocol { get; set; }
    //        public long timestamp { get; set; }
    //        public string wallet { get; set; }
    //        public string xpub { get; set; }
    //    }

    public partial class ContactResponse
    {
        [JsonProperty("contact")]
        public Dictionary<string, string> Contact { get; set; }
    }

    public partial class ContactClass
    {
        [JsonProperty("hostname")]
        public string Hostname { get; set; }

        [JsonProperty("identity")]
        public string Identity { get; set; }

        [JsonProperty("network_id")]
        public string NetworkId { get; set; }

        [JsonProperty("nonce")]
        public long Nonce { get; set; }

        [JsonProperty("port")]
        public long Port { get; set; }

        [JsonProperty("proof")]
        public string Proof { get; set; }

        [JsonProperty("protocol")]
        public string Protocol { get; set; }

        [JsonProperty("pubkey")]
        public string Pubkey { get; set; }

        [JsonProperty("timestamp")]
        public long Timestamp { get; set; }

        [JsonProperty("wallet")]
        public string Wallet { get; set; }
    }

    public partial struct ContactElement
    {
        public ContactClass ContactClass;
        public string String;

        public static implicit operator ContactElement(ContactClass ContactClass) => new ContactElement { ContactClass = ContactClass };
        public static implicit operator ContactElement(string String) => new ContactElement { String = String };
    }

 

}