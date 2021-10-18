using System;

namespace OTHub.Messaging
{
    public class OfferFinalizedMessage
    {
        public string OfferID { get; set; }
        public String Holder1 { get; set; }
        public String Holder2 { get; set; }
        public String Holder3 { get; set; }
        public DateTime Timestamp { get; set; }
        public int BlockchainID { get; set; }

        public OfferFinalizedMessage()
        {
            
        }
    }
}
