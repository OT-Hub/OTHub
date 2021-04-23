using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using Newtonsoft.Json;
using OTHub.APIServer.Sql.Models;
using OTHub.APIServer.Sql.Models.GlobalActivity;
using OTHub.Settings;
using ServiceStack.Text;
using Swashbuckle.AspNetCore.Annotations;

namespace OTHub.APIServer.Controllers
{
    [Route(("api/[controller]"))]
    public class GlobalActivityController : Controller
    {
        //private static String[] _allowedFilters = new string[]
        //{
        //    "New Offer", "Finalized Offer", "Data Holder Chosen", "Offer Payout", "Tokens Deposited", "Tokens Withdrawn", "Identity Created", "Node Approved",
        //    "Litigation Initiated", "Litigation Answered", "Litigation Failed", "Litigation Passed", "Replacement Started", "Data Holder Chosen as Replacement"
        //};

        [HttpGet]
        [SwaggerOperation(
            Summary = "[BETA]",
            Description = @""
        )]
        [SwaggerResponse(200, type: typeof(GlobalActivityModelWithPaging))]
        [SwaggerResponse(500, "Internal server error")]
        public async Task<IActionResult> Get([FromQuery, SwaggerParameter("How many offers you want to return per page", Required = true)] int _limit, [FromQuery, SwaggerParameter("The page number to start from. The first page is 0.", Required = true)] int _page, [FromQuery] string OfferId_like,
            [FromQuery] string _sort,
            [FromQuery] string _order,
            [FromQuery] string EventName_like,
            [FromQuery] string RelatedEntity_like,
            [FromQuery] string RelatedEntity2_like,
            [FromQuery] string TransactionHash_like,
            [FromQuery] bool export,
            [FromQuery] int? exportType)
        {
            //if (searchText != null && searchText.Length > 200)
            //{
            //    searchText = null;
            //}

            //filters = filters.Where(f => _allowedFilters.Contains(f)).Distinct().ToArray();

            //if (!filters.Any())
            //{
            //    filters = new string[] {""};
            //}

            _page--;

            if (EventName_like != null && EventName_like.Length > 200)
            {
                EventName_like = null;
            }

            if (RelatedEntity_like != null && RelatedEntity_like.Length > 200)
            {
                RelatedEntity_like = null;
            }

            if (RelatedEntity2_like != null && RelatedEntity2_like.Length > 200)
            {
                RelatedEntity2_like = null;
            }

            if (TransactionHash_like != null && TransactionHash_like.Length > 200)
            {
                TransactionHash_like = null;
            }

            string orderBy = String.Empty;

            switch (_sort)
            {
                case "Timestamp":
                    orderBy = "ORDER BY Timestamp";
                    break;
                case "EventName":
                    orderBy = "ORDER BY EventName";
                    break;
                case "RelatedEntity":
                    orderBy = "ORDER BY RelatedEntity";
                    break;
                case "RelatedEntity2":
                    orderBy = "ORDER BY RelatedEntity2";
                    break;
                case "TransactionHash":
                    orderBy = "ORDER BY TransactionHash";
                    break;
            }

            if (!String.IsNullOrWhiteSpace(orderBy))
            {
                switch (_order)
                {
                    case "ASC":
                        orderBy += " ASC";
                        break;
                    case "DESC":
                        orderBy += " DESC";
                        break;
                }
            }

            string limit = string.Empty;

            if (_page >= 0 && _limit >= 0)
            {
                limit = $"LIMIT {_page * _limit},{_limit}";
            }

            using (var connection =
                new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                GlobalActivityModel[] summary = (await connection.QueryAsync<GlobalActivityModel>(
                    $@"select * FROM
(
select oc.Timestamp, 'New Offer' as EventName, oc.OfferId as RelatedEntity, '' AS RelatedEntityName, '' as RelatedEntity2, '' AS RelatedEntity2Name, bc.TransactionUrl, oc.TransactionHash as TransactionHash, '' as Message, bc.DisplayName as BlockchainDisplayName from otcontract_holding_offercreated oc
join blockchains bc on bc.ID = oc.BlockchainID
union all 
select DATE_ADD(of.Timestamp, INTERVAL 1 MICROSECOND), 'Data Holder Chosen', i.Identity, mn.DisplayName, h.OfferId, '', bc.TransactionUrl, of.TransactionHash, '', bc.DisplayName as BlockchainDisplayName from otoffer_holders h
join otcontract_holding_offerfinalized of on of.OfferID = h.OfferId 
join blockchains bc on bc.ID = h.BlockchainID
JOIN otidentity i ON i.Identity = h.Holder
LEFT JOIN mynodes mn ON mn.UserID = @userID AND mn.NodeID = i.NodeId
where h.IsOriginalHolder = 1
GROUP BY h.ID
union all
select ofi.Timestamp, 'Finalized Offer', ofi.OfferId, '', '', '', bc.TransactionUrl, ofi.TransactionHash, '', bc.DisplayName as BlockchainDisplayName from otcontract_holding_offerfinalized ofi
join blockchains bc on bc.ID = ofi.BlockchainID
union all 
select po.Timestamp, 'Offer Payout', i.Identity, mn.DisplayName, '', '', bc.TransactionUrl, po.TransactionHash, '', bc.DisplayName as BlockchainDisplayName from otcontract_holding_paidout po
join blockchains bc on bc.ID = po.BlockchainID
JOIN otidentity i ON i.Identity = po.Holder
LEFT JOIN mynodes mn ON mn.UserID = @userID AND mn.NodeID = i.NodeId
GROUP BY po.Id
union all 
select b.Timestamp, 'Tokens Deposited', i.Identity, mn.DisplayName, '', '', bc.TransactionUrl, td.TransactionHash, '', bc.DisplayName as BlockchainDisplayName from otcontract_profile_tokensdeposited td
join blockchains bc on bc.ID = td.BlockchainID
JOIN EthBlock b on b.BlockNumber = td.BlockNumber AND b.BlockchainID = bc.ID
JOIN otidentity i ON i.Identity = td.Profile
LEFT JOIN mynodes mn ON mn.UserID = @userID AND mn.NodeID = i.NodeId
GROUP BY td.Id
union all 
select b.Timestamp, 'Tokens Withdrawn', i.Identity, mn.DisplayName, '', '', bc.TransactionUrl, tw.TransactionHash, '', bc.DisplayName as BlockchainDisplayName from otcontract_profile_tokenswithdrawn tw
join blockchains bc on bc.ID = tw.BlockchainID
JOIN EthBlock b on b.BlockNumber = tw.BlockNumber AND b.BlockchainID = bc.ID
JOIN otidentity i ON i.Identity = tw.Profile
LEFT JOIN mynodes mn ON mn.UserID = @userID AND mn.NodeID = i.NodeId
GROUP BY tw.Id
union all 
select b.Timestamp, 'Identity Created', i.Identity, mn.DisplayName, '', '', bc.TransactionUrl, ic.TransactionHash, '', bc.DisplayName as BlockchainDisplayName from otcontract_profile_identitycreated ic
join blockchains bc on bc.ID = ic.BlockchainID
JOIN EthBlock b on b.BlockNumber = ic.BlockNumber AND b.BlockchainID = bc.ID
JOIN otidentity i ON i.Identity = ic.NewIdentity
LEFT JOIN mynodes mn ON mn.UserID = @userID AND mn.NodeID = i.NodeId
GROUP BY ic.TransactionHash
union all 
select li.Timestamp, 'Litigation Initiated', i.Identity, mn.DisplayName, li.OfferId, '', bc.TransactionUrl, li.TransactionHash, '', bc.DisplayName as BlockchainDisplayName from otcontract_litigation_litigationinitiated li
join blockchains bc on bc.ID = li.BlockchainID
JOIN otidentity i ON i.Identity = li.HolderIdentity
LEFT JOIN mynodes mn ON mn.UserID = @userID AND mn.NodeID = i.NodeId
GROUP BY li.TransactionHash
union all 
select la.Timestamp, 'Litigation Answered', i.Identity, mn.DisplayName, la.OfferId, '', bc.TransactionUrl, la.TransactionHash, '', bc.DisplayName as BlockchainDisplayName from otcontract_litigation_litigationanswered la
join blockchains bc on bc.ID = la.BlockchainID
JOIN otidentity i ON i.Identity = la.HolderIdentity
LEFT JOIN mynodes mn ON mn.UserID = @userID AND mn.NodeID = i.NodeId
GROUP BY la.TransactionHash
union all 
select lc.Timestamp, case when lc.DHWasPenalized = 1 THEN 'Litigation Failed' ELSE 'Litigation Passed' END, i.Identity, mn.DisplayName, lc.OfferId, '', bc.TransactionUrl, lc.TransactionHash, '', bc.DisplayName as BlockchainDisplayName from otcontract_litigation_litigationcompleted lc
join blockchains bc on bc.ID = lc.BlockchainID
JOIN otidentity i ON i.Identity = lc.HolderIdentity
LEFT JOIN mynodes mn ON mn.UserID = @userID AND mn.NodeID = i.NodeId
GROUP BY lc.TransactionHash
union all 
select DATE_ADD(rs.Timestamp, INTERVAL 1 MICROSECOND), 'Replacement Started', i.Identity, mn.DisplayName, rs.OfferId, '', bc.TransactionUrl, rs.TransactionHash, '', bc.DisplayName as BlockchainDisplayName from otcontract_litigation_replacementstarted rs
join blockchains bc on bc.ID = rs.BlockchainID
JOIN otidentity i ON i.Identity = rs.HolderIdentity
LEFT JOIN mynodes mn ON mn.UserID = @userID AND mn.NodeID = i.NodeId
GROUP BY rs.TransactionHash
union all 
select rc.Timestamp, 'Data Holder Chosen as Replacement', i.Identity, mn.DisplayName, rc.OfferId, '', bc.TransactionUrl, rc.TransactionHash, '', bc.DisplayName as BlockchainDisplayName from otcontract_replacement_replacementcompleted rc
join blockchains bc on bc.ID = rc.BlockchainID
JOIN otidentity i ON i.Identity = rc.ChosenHolder
LEFT JOIN mynodes mn ON mn.UserID = @userID AND mn.NodeID = i.NodeId
GROUP BY rc.TransactionHash
) x
WHERE 
(@EventName_like IS NULL OR x.EventName = @EventName_like) AND
(@RelatedEntity_like IS NULL OR x.RelatedEntity = @RelatedEntity_like) AND 
(@RelatedEntity2_like IS NULL OR x.RelatedEntity2 = @RelatedEntity2_like) AND 
(@TransactionHash_like IS NULL OR x.TransactionHash = @TransactionHash_like)
{orderBy}
{limit}", new
                    {
                        EventName_like,
                        RelatedEntity_like,
                        RelatedEntity2_like,
                        TransactionHash_like,
                        userID = User?.Identity?.Name
                    })).ToArray();

                var total = await connection.ExecuteScalarAsync<int>(@"select COUNT(*) FROM
(
select oc.Timestamp, 'New Offer' as EventName, oc.OfferId as RelatedEntity, '' as RelatedEntity2, oc.TransactionHash as TransactionHash, '' as Message, bc.DisplayName as BlockchainDisplayName from otcontract_holding_offercreated oc
join blockchains bc on bc.ID = oc.BlockchainID
union all 
select DATE_ADD(of.Timestamp, INTERVAL 1 MICROSECOND), 'Data Holder Chosen', i.NodeId, h.OfferId, of.TransactionHash, '', bc.DisplayName as BlockchainDisplayName from otoffer_holders h
join otcontract_holding_offerfinalized of on of.OfferID = h.OfferId 
join blockchains bc on bc.ID = h.BlockchainID
JOIN otidentity i ON i.Identity = h.Holder
where h.IsOriginalHolder = 1
GROUP BY h.ID
union all
select ofi.Timestamp, 'Finalized Offer', ofi.OfferId, '', ofi.TransactionHash, '', bc.DisplayName as BlockchainDisplayName from otcontract_holding_offerfinalized ofi
join blockchains bc on bc.ID = ofi.BlockchainID
union all 
select po.Timestamp, 'Offer Payout', i.NodeId, '', po.TransactionHash, '', bc.DisplayName as BlockchainDisplayName from otcontract_holding_paidout po
join blockchains bc on bc.ID = po.BlockchainID
JOIN otidentity i ON i.Identity = po.Holder
GROUP BY po.Id
union all 
select b.Timestamp, 'Tokens Deposited', i.NodeId, '', td.TransactionHash, '', bc.DisplayName as BlockchainDisplayName from otcontract_profile_tokensdeposited td
join blockchains bc on bc.ID = td.BlockchainID
JOIN EthBlock b on b.BlockNumber = td.BlockNumber AND b.BlockchainID = bc.ID
JOIN otidentity i ON i.Identity = td.Profile
GROUP BY td.Id
union all 
select b.Timestamp, 'Tokens Withdrawn', i.NodeId, '', tw.TransactionHash, '', bc.DisplayName as BlockchainDisplayName from otcontract_profile_tokenswithdrawn tw
join blockchains bc on bc.ID = tw.BlockchainID
JOIN EthBlock b on b.BlockNumber = tw.BlockNumber AND b.BlockchainID = bc.ID
JOIN otidentity i ON i.Identity = tw.Profile
GROUP BY tw.Id
union all 
select b.Timestamp, 'Identity Created', i.NodeId, '', ic.TransactionHash, '', bc.DisplayName as BlockchainDisplayName from otcontract_profile_identitycreated ic
join blockchains bc on bc.ID = ic.BlockchainID
JOIN EthBlock b on b.BlockNumber = ic.BlockNumber AND b.BlockchainID = bc.ID
JOIN otidentity i ON i.Identity = ic.NewIdentity
GROUP BY ic.TransactionHash
union all 
select li.Timestamp, 'Litigation Initiated', i.NodeId, li.OfferId, li.TransactionHash, '', bc.DisplayName as BlockchainDisplayName from otcontract_litigation_litigationinitiated li
join blockchains bc on bc.ID = li.BlockchainID
JOIN otidentity i ON i.Identity = li.HolderIdentity
GROUP BY li.TransactionHash
union all 
select la.Timestamp, 'Litigation Answered', i.NodeId, la.OfferId, la.TransactionHash, '', bc.DisplayName as BlockchainDisplayName from otcontract_litigation_litigationanswered la
join blockchains bc on bc.ID = la.BlockchainID
JOIN otidentity i ON i.Identity = la.HolderIdentity
GROUP BY la.TransactionHash
union all 
select lc.Timestamp, case when lc.DHWasPenalized = 1 THEN 'Litigation Failed' ELSE 'Litigation Passed' END, i.NodeId, lc.OfferId, lc.TransactionHash, '', bc.DisplayName as BlockchainDisplayName from otcontract_litigation_litigationcompleted lc
join blockchains bc on bc.ID = lc.BlockchainID
JOIN otidentity i ON i.Identity = lc.HolderIdentity
GROUP BY lc.TransactionHash
union all 
select DATE_ADD(rs.Timestamp, INTERVAL 1 MICROSECOND), 'Replacement Started', i.NodeId, rs.OfferId, rs.TransactionHash, '', bc.DisplayName as BlockchainDisplayName from otcontract_litigation_replacementstarted rs
join blockchains bc on bc.ID = rs.BlockchainID
JOIN otidentity i ON i.Identity = rs.HolderIdentity
GROUP BY rs.TransactionHash
union all 
select rc.Timestamp, 'Data Holder Chosen as Replacement', i.NodeId, rc.OfferId, rc.TransactionHash, '', bc.DisplayName as BlockchainDisplayName from otcontract_replacement_replacementcompleted rc
join blockchains bc on bc.ID = rc.BlockchainID
JOIN otidentity i ON i.Identity = rc.ChosenHolder
GROUP BY rc.TransactionHash
) x
WHERE 
(@EventName_like IS NULL OR x.EventName = @EventName_like) AND
(@RelatedEntity_like IS NULL OR x.RelatedEntity = @RelatedEntity_like) AND 
(@RelatedEntity2_like IS NULL OR x.RelatedEntity2 = @RelatedEntity2_like) AND 
(@TransactionHash_like IS NULL OR x.TransactionHash = @TransactionHash_like)", new
                {
                    EventName_like,
                    RelatedEntity_like,
                    RelatedEntity2_like,
                    TransactionHash_like
                });

                HttpContext.Response.Headers["access-control-expose-headers"] = "X-Total-Count";
                HttpContext.Response.Headers["X-Total-Count"] = total.ToString();

                if (export)
                {
                    if (exportType == 0)
                    {
                        return File(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(summary)), "application/json", "globalactivity.json", false);
                    }
                    else if (exportType == 1)
                    {
                        return File(Encoding.UTF8.GetBytes(CsvSerializer.SerializeToCsv(summary)), "text/csv", "globalactivity.csv", false);
                    }
                }

                return new OkObjectResult(summary);
            }
        }
    }
}