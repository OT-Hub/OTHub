using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using OTHub.APIServer.Sql.Models.Contracts;
using OTHub.Settings;

namespace OTHub.APIServer.Controllers
{
    [Route("api/[controller]")]
    public class StarfleetBoardingController : Controller
    {
        [Route("")]
        [HttpGet]
        public async Task<StarfleetBoardingAddressBalanceModel[]> GetAll([FromQuery] int _limit,
            [FromQuery]int _page,
            [FromQuery] string _sort,
            [FromQuery] string _order,
            [FromQuery] string Address_like)
        {
            _page--;

            await using (var connection = new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                string orderBy = String.Empty;

                switch (_sort)
                {
                    case "Address":
                        orderBy = "ORDER BY Address";
                        break;
                    case "Amount":
                        orderBy = "ORDER BY Amount";
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

                string limitSql = string.Empty;

                if (_page >= 0 && _limit >= 0)
                {
                    limitSql = $"LIMIT {_page * _limit},{_limit}";
                }

                HttpContext.Response.Headers["access-control-expose-headers"] = "X-Total-Count";
                HttpContext.Response.Headers["X-Total-Count"] = connection.ExecuteScalar<int>(
                    @$"SELECT 
COUNT(DISTINCT Address)
FROM starfleetboarding_deposit WHERE @address is null or Address = @address", new
                    {
                        address = Address_like
                    }).ToString();

                return connection.Query<StarfleetBoardingAddressBalanceModel>(
                    @$"SELECT 
Address, 
SUM(Amount) Amount 
FROM starfleetboarding_deposit
WHERE @address is null or Address = @address
GROUP BY Address
{orderBy}
{limitSql}", new
                    {
                        address = Address_like
                    }).ToArray();
            }
        }
    }

    public class StarfleetBoardingAddressBalanceModel
    {
        public String Address { get; set; }
        public decimal Amount { get; set; }
    }
}