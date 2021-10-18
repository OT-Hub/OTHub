using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OTHub.APIServer.Sql
{
    public class ContractsSql
    {
        public const String GetHoldingStorageAddressByAddress = @"select Address, BlockchainID from otcontract
where Type = 5 AND Address = @holdingStorageAddress and BlockchainID = @blockchainID";

        public const String GetHoldingAddressByAddress = @"select Address, BlockchainID from otcontract
where Type = 6 AND Address = @holdingAddress and BlockchainID = @blockchainID";
    }
}
