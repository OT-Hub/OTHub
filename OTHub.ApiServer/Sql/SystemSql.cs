using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OTHub.APIServer.Sql
{
    public static class SystemSql
    {
        public const string GetSql =
            @"select Name, LastSuccessDateTime, LastTriedDateTime, Success from systemstatus ORDER BY Name";
    }
}
