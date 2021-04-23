using System;

namespace OTHub.APIServer.Sql.Models.GlobalActivity
{
    public class GlobalActivityModel
    {
        public DateTime Timestamp { get; set; }
        public String EventName { get; set; }
        public String RelatedEntity { get; set; }
        public String RelatedEntity2 { get; set; }
        public String RelatedEntityName { get; set; }
        public String RelatedEntity2Name { get; set; }
        public String TransactionHash { get; set; }
        public String Message { get; set; }
        public string BlockchainDisplayName { get; set; }

        private string TransactionUrl { get; set; }

        public string RealTransactionUrl
        {
            get
            {
                return string.Format(TransactionUrl, TransactionHash);
            }
        }

    }

    public class GlobalActivityModelWithPaging
    {
        public int draw { get; set; }
        public int recordsTotal { get; set; }
        public int recordsFiltered { get; set; }

        public GlobalActivityModel[] data { get; set; }
    }
}
