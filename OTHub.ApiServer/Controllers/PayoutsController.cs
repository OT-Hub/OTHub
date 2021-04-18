using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using OTHub.Settings;

namespace OTHub.APIServer.Controllers
{
    [Route("api/[controller]")]
    public class PayoutsController : Controller
    {
        [HttpGet]
        [Route("getofferidsforpayout/{identity}")]
        public async Task<IActionResult> Get([FromRoute] string identity, [FromQuery] int ignoreWithPayoutsInLastXDays = -1)
        {
            if (ignoreWithPayoutsInLastXDays > 0 || ignoreWithPayoutsInLastXDays < -999999)
            {
                ignoreWithPayoutsInLastXDays = -1;
            }

            await using (var connection =
                new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                var data = (await connection.QueryAsync<string>(@"SELECT
O.OfferID
,O.TokenAmountPerHolder
 FROM OTIdentity I
JOIN otoffer_holders H ON I.BlockchainID = H.BlockchainID AND H.Holder = I.Identity
JOIN otoffer O ON O.BlockchainID = H.BlockchainID AND O.OfferID = H.OfferID
LEFT JOIN otcontract_holding_paidout PO ON PO.BlockchainID = H.BlockchainID AND PO.OfferID = O.OfferID AND PO.Holder = I.Identity
LEFT JOIN otcontract_litigation_litigationcompleted lc ON lc.BlockchainID = H.BlockchainID AND lc.OfferId = H.OfferID AND lc.HolderIdentity = I.Identity
WHERE I.Identity = @identity
GROUP BY H.ID
HAVING COALESCE(SUM(PO.Amount), 0) != O.TokenAmountPerHolder 
AND COALESCE(MAX(lc.DHWasPenalized), 0) != 1
AND (MAX(PO.Timestamp) IS NULL OR MAX(PO.Timestamp) <= DATE_Add(NOW(), INTERVAL @ignoreWithPayoutsInLastXDays DAY))", new
                {
                    identity = identity,
                    ignoreWithPayoutsInLastXDays
                })).ToArray();

                return Ok(data.Aggregate((d, e) => d + Environment.NewLine + e));
            }
        }
    }
}