using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OTHub.APIServer.Sql
{
    public static class SystemSql
    {
        public const string GetSql =
            @"SELECT s.ID, 
s.Name, s.LastSuccessDateTime, s.LastTriedDateTime, s.Success, s.IsRunning, s.NextRunDateTime, b.BlockchainName, b.NetworkName, s.ParentName
from systemstatus s 
LEFT JOIN blockchains b ON b.id = s.BlockchainID 
ORDER BY b.id, s.Name";
    }
}
