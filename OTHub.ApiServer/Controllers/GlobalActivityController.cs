using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using OTHub.APIServer.Models;
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
        public IActionResult Get([FromQuery, SwaggerParameter("How many offers you want to return per page", Required = true)] int _limit, [FromQuery, SwaggerParameter("The page number to start from. The first page is 0.", Required = true)] int _page, [FromQuery] string OfferId_like,
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
                limit = $"LIMIT {_page},{_limit}";
            }

            using (var connection =
                new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                GlobalActivityModel[] summary = connection.Query<GlobalActivityModel>(
                    $@"select * FROM
(
select Timestamp, 'New Offer' as EventName, OfferId as RelatedEntity, '' as RelatedEntity2, TransactionHash as TransactionHash, '' as Message from otcontract_holding_offercreated
union all 
select DATE_ADD(of.Timestamp, INTERVAL 1 MICROSECOND), 'Data Holder Chosen', h.Holder, h.OfferId, of.TransactionHash, '' from otoffer_holders h
join otcontract_holding_offerfinalized of on of.OfferID = h.OfferId where h.IsOriginalHolder = 1
union all
select Timestamp, 'Finalized Offer', OfferId, '', TransactionHash, '' from otcontract_holding_offerfinalized
union all 
select Timestamp, 'Offer Payout', Holder, '', TransactionHash, '' from otcontract_holding_paidout
union all 
select b.Timestamp, 'Tokens Deposited', Profile, '', TransactionHash, '' from otcontract_profile_tokensdeposited td
JOIN EthBlock b on b.BlockNumber = td.BlockNumber
union all 
select b.Timestamp, 'Tokens Withdrawn', Profile, '', TransactionHash, '' from otcontract_profile_tokenswithdrawn tw
JOIN EthBlock b on b.BlockNumber = tw.BlockNumber
union all 
select b.Timestamp, 'Identity Created', NewIdentity, '', TransactionHash, '' from otcontract_profile_identitycreated ic
JOIN EthBlock b on b.BlockNumber = ic.BlockNumber
union all 
select Timestamp, 'Litigation Initiated', HolderIdentity, OfferId, TransactionHash, '' from otcontract_litigation_litigationinitiated
union all 
select Timestamp, 'Litigation Answered', HolderIdentity, OfferId, TransactionHash, '' from otcontract_litigation_litigationanswered
union all 
select Timestamp, case when DHWasPenalized = 1 THEN 'Litigation Failed' ELSE 'Litigation Passed' END, HolderIdentity, OfferId, TransactionHash, '' from otcontract_litigation_litigationcompleted
union all 
select DATE_ADD(Timestamp, INTERVAL 1 MICROSECOND), 'Replacement Started', HolderIdentity, OfferId, TransactionHash, '' from otcontract_litigation_replacementstarted
union all 
select Timestamp, 'Data Holder Chosen as Replacement', ChosenHolder, OfferId, TransactionHash, '' from otcontract_replacement_replacementcompleted
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
                        TransactionHash_like
                    }).ToArray();

                var total = connection.ExecuteScalar<int>(@"select count(*) FROM
(
select Timestamp, 'New Offer' as EventName, OfferId as RelatedEntity, '' as RelatedEntity2, TransactionHash as TransactionHash, '' as Message from otcontract_holding_offercreated
union all 
select of.Timestamp, 'Data Holder Chosen', h.Holder, h.OfferId, of.TransactionHash, '' from otoffer_holders h
join otcontract_holding_offerfinalized of on of.OfferID = h.OfferId where h.IsOriginalHolder = 1
union all
select Timestamp, 'Finalized Offer', OfferId, '', TransactionHash, '' from otcontract_holding_offerfinalized
union all 
select Timestamp, 'Offer Payout', Holder, '', TransactionHash, '' from otcontract_holding_paidout
union all 
select b.Timestamp, 'Tokens Deposited', Profile, '', TransactionHash, '' from otcontract_profile_tokensdeposited td
JOIN EthBlock b on b.BlockNumber = td.BlockNumber
union all 
select b.Timestamp, 'Tokens Withdrawn', Profile, '', TransactionHash, '' from otcontract_profile_tokenswithdrawn tw
JOIN EthBlock b on b.BlockNumber = tw.BlockNumber
union all 
select b.Timestamp, 'Identity Created', NewIdentity, '', TransactionHash, '' from otcontract_profile_identitycreated ic
JOIN EthBlock b on b.BlockNumber = ic.BlockNumber
union all 
select Timestamp, 'Node Approved', NodeId, '', TransactionHash, '' from otcontract_approval_nodeapproved
union all 
select Timestamp, 'Litigation Initiated', HolderIdentity, OfferId, TransactionHash, '' from otcontract_litigation_litigationinitiated
union all 
select Timestamp, 'Litigation Answered', HolderIdentity, OfferId, TransactionHash, '' from otcontract_litigation_litigationanswered
union all 
select Timestamp, case when DHWasPenalized = 1 THEN 'Litigation Failed' ELSE 'Litigation Passed' END, HolderIdentity, OfferId, TransactionHash, '' from otcontract_litigation_litigationcompleted
union all 
select Timestamp, 'Replacement Started', HolderIdentity, OfferId, TransactionHash, '' from otcontract_litigation_replacementstarted
union all 
select Timestamp, 'Data Holder Chosen as Replacement', ChosenHolder, OfferId, TransactionHash, '' from otcontract_replacement_replacementcompleted
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