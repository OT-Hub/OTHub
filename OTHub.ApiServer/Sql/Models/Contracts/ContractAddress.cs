using System;

namespace OTHub.APIServer.Sql.Models.Contracts
{
    public class ContractAddress
    {
        public String Address { get; set; }
        public bool IsLatest { get; set; }
    }
}