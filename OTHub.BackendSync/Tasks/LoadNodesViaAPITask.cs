using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using OTHelperNetStandard.Models.Database;
using OTHelperNetStandard.Models.Generated;
using OTHub.Settings;

namespace OTHelperNetStandard.Tasks
{
    public class LoadNodesViaAPITask : TaskRun
    {
        public LoadNodesViaAPITask() : base("Load Nodes via API")
        {
        }

        public async override Task Execute(Source source)
        {
            if (String.IsNullOrWhiteSpace(OTHubSettings.Instance.OriginTrailNode.Url))
            {
                Logger.WriteLine(source, "Node URL is not specified in the settings skipping LoadNodesViaAPI...");
                return;
            }

            using (var connection = new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                var nodesToCheck = connection.Query<string>($@"select I.NodeId FROM (
select r.NewIdentity, MAX(Timestamp) Timestamp from otcontract_profile_identitycreated r
join ethblock b on r.BlockNumber = b.BlockNumber
GROUP BY r.NewIdentity
union all
select r.NewIdentity, MAX(Timestamp) from otcontract_profile_identitytransferred r
join ethblock b on r.BlockNumber = b.BlockNumber
GROUP BY r.NewIdentity
union all
select r.Profile, MAX(Timestamp) from otcontract_profile_profilecreated r
join ethblock b on r.BlockNumber = b.BlockNumber
group by r.Profile
union all
select r.Profile, MAX(Timestamp) from otcontract_profile_tokensdeposited r
join ethblock b on r.BlockNumber = b.BlockNumber
group by r.Profile
union all
select r.Profile, MAX(Timestamp) from otcontract_profile_tokensreleased r
join ethblock b on r.BlockNumber = b.BlockNumber
group by r.Profile
union all
select r.Profile, MAX(Timestamp) from otcontract_profile_tokensreserved r
join ethblock b on r.BlockNumber = b.BlockNumber
group by r.Profile
union all
select r.Profile, MAX(Timestamp) from otcontract_profile_tokenswithdrawn r
join ethblock b on r.BlockNumber = b.BlockNumber
group by r.Profile
union all
select h.Holder, MAX(b.Timestamp) from otoffer_holders h
join otoffer o on o.offerid = h.offerid
join ethblock b on b.blocknumber = o.finalizedblocknumber
group by h.Holder
union all
select p.holder, MAX(b.Timestamp) from otcontract_holding_paidout p
join ethblock b on b.blocknumber = p.blocknumber
group by p.holder) x
JOIN OTIdentity i on x.NewIdentity = i.Identity
WHERE 
Version != 0 {(OTHubSettings.Instance.Blockchain.Network == BlockchainNetwork.Testnet ? "" : " AND i.Approved = 1")} 
AND (NOT EXISTS (SELECT 1 FROM OTNode_IPInfo IP WHERE IP.NodeId = I.NodeId) 
OR (SELECT COUNT(*) FROM OTNode_History H WHERE H.NodeId = I.NodeId AND Success = 1 AND h.Timestamp >= DATE_Add(NOW(), INTERVAL -7 DAY)) = 0)
GROUP BY NewIdentity, I.NodeId
{(OTHubSettings.Instance.Blockchain.Network == BlockchainNetwork.Testnet ? "" : "HAVING MAX(x.Timestamp) >= DATE_Add(NOW(), INTERVAL - 4 WEEK)")} 
ORDER BY MAX(x.Timestamp) DESC")
                    .ToArray();

                Console.WriteLine("Found " + nodesToCheck.Length + " nodes to check via API.");

                int counter = 0;
                foreach (var nodeToCheck in nodesToCheck)
                {
                    counter++;
                    try
                    {
                        Logger.WriteLine(source, "Trying " + nodeToCheck + " via node API (" + counter + " of " + nodesToCheck.Length + ")");

                        string strData = GetRequest($"{OTHubSettings.Instance.OriginTrailNode.Url}/api/network/get-contact/" + nodeToCheck);

                        NodeContact data = JsonConvert.DeserializeObject<NodeContact>(strData);

                        bool isOnline = false;

                        try
                        {
                            string url = $"https://{data.hostname}:{data.port}/";

                            var request = (HttpWebRequest) WebRequest.Create(url);
                            request.Timeout = counter <= 10 ? 10000 : 5000;
                            request.AllowAutoRedirect = false;
                            request.ServerCertificateValidationCallback = delegate(object sender,
                                X509Certificate certificate,
                                X509Chain chain, SslPolicyErrors errors)
                            {

                                isOnline = true;

                                return true;
                            };

                            HttpWebResponse response = (HttpWebResponse) request.GetResponse();
                        }
                        catch (Exception ex)
                        {

                        }

                        if (isOnline)
                        {
                     

                            var info = new IpInfo();
                            info.Hostname = data.hostname;
                            info.Wallet = data.wallet;
                            info.Port = data.port;
                            info.NodeId = nodeToCheck;

                            info.Timestamp = new DateTime(1970, 1, 1).AddMilliseconds(data.timestamp);

                            if (info.Timestamp.Year < DateTime.Now.Year)
                            {
                                info.Timestamp = DateTime.UtcNow;
                            }


                            info.LastCheckedTimestamp = DateTime.UtcNow;


                            if (connection.ExecuteScalar<bool>(@"SELECT NOT EXISTS (SELECT 1 FROM OTNode_IPInfo IP WHERE IP.NodeId = @nodeId)", new {nodeId = nodeToCheck}))
                            {
                                Logger.WriteLine(source, "Found " + nodeToCheck + " via node API! Inserting...");
                                IpInfo.Insert(connection, info);
                            }
                            else
                            {
                                Logger.WriteLine(source, "Found " + nodeToCheck + " via node API! Updating...");

                                IpInfo.UpdateHost(connection, nodeToCheck, data.hostname, data.port, info.Timestamp, info.LastCheckedTimestamp);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        if (!e.Message.Contains("The operation has timed out"))
                        {
                            Logger.WriteLine(source, "Error checking node online (LoadNodesViaAPI): " + e.Message);
                        }
                    }
                }
            }
        }

        private class TimeoutWebClient : System.Net.WebClient
        {
            public int Timeout { get; set; }

            protected override WebRequest GetWebRequest(Uri uri)
            {
                WebRequest lWebRequest = base.GetWebRequest(uri);
                lWebRequest.Timeout = Timeout;
                ((HttpWebRequest)lWebRequest).ReadWriteTimeout = Timeout;
                return lWebRequest;
            }
        }

        private string GetRequest(string sURL)
        {
            using (var lWebClient = new TimeoutWebClient())
            {
                lWebClient.Timeout = (int)TimeSpan.FromSeconds(70).TotalMilliseconds;
                return lWebClient.DownloadString(sURL);
            }
        }
    }
}