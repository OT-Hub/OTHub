using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using OTHub.APIServer.Sql;
using OTHub.APIServer.Sql.Models;
using OTHub.APIServer.Sql.Models.System;
using OTHub.Settings;
using Swashbuckle.AspNetCore.Annotations;

namespace OTHub.APIServer.Controllers
{
    [Route("api/[controller]")]
    public class SystemController : Controller
    {
        [HttpGet]
        [SwaggerResponse(200, type: typeof(SystemStatus))]
        [SwaggerResponse(500, "Internal server error")]
        public async Task<SystemStatus> Get()
        {
            SystemStatus status = new SystemStatus();

            await using (var connection = new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                SystemStatusItem[] items = (await connection.QueryAsync<SystemStatusItem>(SystemSql.GetSql)).ToArray();

                SystemStatusGroup[] groups = items.GroupBy(i => i.BlockchainDisplayName != null ? i.BlockchainDisplayName : i.ParentName ?? i.Name)
                    .Select(g => new SystemStatusGroup() {Name = g.Key, Items = g.OrderBy(g => g.ID).ToList()})
                    .ToArray();

                foreach (SystemStatusGroup group in groups)
                {
                    SystemStatusItem[] itemsWhichNeedMoving = group.Items.Where(it => it.ParentName != null).ToArray();

                    foreach (SystemStatusItem systemStatusItem in itemsWhichNeedMoving)
                    {
                        SystemStatusItem foundItem = group.Items.FirstOrDefault(i => i.Name == systemStatusItem.ParentName);
                        if (foundItem != null)
                        {
                            group.Items.Remove(systemStatusItem);
                            foundItem.Children.Add(systemStatusItem);
                        }
                    }
                }

                status.Groups = groups;
            }

            return status;
        }
    }
}