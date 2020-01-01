using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using OTHub.APIServer.Models;
using OTHub.Settings;
using Swashbuckle.AspNetCore.Annotations;

namespace OTHub.APIServer.Controllers
{
    [Route(("api/[controller]"))]
    public class GlobalActivityController
    {
        private static String[] _allowedFilters = new string[]
        {
            "New Offer", "Finalized Offer", "Data Holder Chosen", "Offer Payout", "Tokens Deposited", "Tokens Withdrawn", "Identity Created", "Node Approved",
            "Litigation Initiated", "Litigation Answered", "Litigation Failed", "Litigation Passed", "Replacement Started", "Data Holder Chosen as Replacement"
        };

        [HttpGet]
        [SwaggerOperation(
            Summary = "[BETA]",
            Description = @""
        )]
        [SwaggerResponse(200, type: typeof(GlobalActivityModelWithPaging))]
        [SwaggerResponse(500, "Internal server error")]
        public GlobalActivityModelWithPaging Get([FromQuery, SwaggerParameter("How many results you want to return per page", Required = true)] int pageLength, [FromQuery, SwaggerParameter("The page number to start from. The first page is 0.", Required = true)] int start, [FromQuery] string[] filters, [FromQuery] string searchText)
        {
            if (searchText != null && searchText.Length > 200)
            {
                searchText = null;
            }

            filters = filters.Where(f => _allowedFilters.Contains(f)).Distinct().ToArray();

            if (!filters.Any())
            {
                filters = new string[] {""};
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
select Timestamp, 'Node Approved', NodeId, '', TransactionHash, '' from otcontract_approval_nodeapproved
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
WHERE x.EventName in @filters AND (COALESCE(@searchText, '') = '' OR x.RelatedEntity = @searchText OR x.RelatedEntity2 = @searchText OR x.TransactionHash = @searchText)
ORDER BY x.Timestamp DESC
LIMIT {start},{pageLength}", new
                    {
                        filters,
                        searchText
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
WHERE x.EventName in @filters AND (COALESCE(@searchText, '') = '' OR x.RelatedEntity = @searchText OR x.RelatedEntity2 = @searchText)", new
                {
                    filters,
                    searchText
                });

                return new GlobalActivityModelWithPaging
                {
                    data = summary,
                    draw = summary.Length,
                    recordsFiltered = total,
                    recordsTotal = total
                };
            }
        }
    }
}