using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Dapper;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using OTHub.BackendSync.Database.Models;
using OTHub.BackendSync.Logging;
using OTHub.BackendSync.Nodes.Models;
using OTHub.Settings;

namespace OTHub.BackendSync.Nodes.Tasks
{
    public class SearchForNewlyCreatedNodesTask : TaskRun
    {
        public SearchForNewlyCreatedNodesTask() : base("Search for Newly Created Nodes on the ODN")
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
                var nodesToCheck = connection.Query<string>($@"select distinct I.NodeId FROM (
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
LEFT JOIN otnode_history h ON h.NodeId = i.NodeId AND h.Timestamp >= DATE_ADD(NOW(), INTERVAL -1 DAY)
WHERE 
I.VERSION != 0
GROUP BY NewIdentity, I.NodeId
HAVING (MAX(h.Id) IS NULL OR MAX(h.Success) != 1) AND MAX(x.Timestamp) > DATE_ADD(NOW(), INTERVAL -3 MONTH)
ORDER BY MAX(x.Timestamp) DESC")
                    .ToArray();

                //{(OTHubSettings.Instance.Blockchain.Network == BlockchainNetwork.Testnet ? "" : "HAVING MAX(x.Timestamp) >= DATE_Add(NOW(), INTERVAL - 8 WEEK)")} 

                Console.WriteLine("Found " + nodesToCheck.Length + " nodes to check via API.");


                //var lists = Split<string>(nodesToCheck, nodesToCheck.Length / 2);



                int counter = 0;

                foreach (var nodeToCheck in nodesToCheck)
                {
                    counter++;

                    try
                    {
                        string urlText =
                            $"{OTHubSettings.Instance.OriginTrailNode.Url}/api/latest/network/get-contact/" +
                            nodeToCheck;

                        Logger.WriteLine(source,
                            "Trying " + nodeToCheck + " via node API (" + counter + " of " + nodesToCheck.Length +
                            "): " + urlText);

                        string strData = GetRequest(urlText);

                        //Good for diagnostics
                        //Logger.WriteLine(source, "Response from node: " + strData);



                        var dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(strData);
                        var array = dict["contact"] as Newtonsoft.Json.Linq.JArray;
                        var node = JsonConvert.DeserializeObject<ContactClass>(array.Last.ToString());

                        bool isOnline = false;

                        Stopwatch sw = new Stopwatch();
                        sw.Start();

                        string url = $"https://{node.Hostname}:{node.Port}/";


                        Logger.WriteLine(source, "Node url to test is " + url);

                        try
                        {



                            var request = (HttpWebRequest)WebRequest.Create(url);
                            request.Timeout = (int)TimeSpan.FromSeconds(15).TotalMilliseconds;
                            request.AllowAutoRedirect = false;
                            request.ServerCertificateValidationCallback = delegate (object sender,
                                X509Certificate certificate,
                                X509Chain chain, SslPolicyErrors errors)
                            {

                                isOnline = true;

                                return true;
                            };

                            HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync();
                        }
                        catch (Exception ex)
                        {

                        }
                        finally
                        {
                            sw.Stop();
                        }

                        if (isOnline)
                        {
                            var info = new IpInfo();
                            info.Hostname = node.Hostname;
                            info.Wallet = node.Wallet;
                            info.Port = (int)node.Port;
                            info.NodeId = nodeToCheck;
                            info.NetworkId = node.NetworkId;

                            info.Timestamp = new DateTime(1970, 1, 1).AddMilliseconds(node.Timestamp);

                            if (info.Timestamp.Year < DateTime.Now.Year)
                            {
                                info.Timestamp = DateTime.UtcNow;
                            }


                            info.LastCheckedTimestamp = DateTime.UtcNow;


                            if (connection.ExecuteScalar<bool>(
                                @"SELECT NOT EXISTS (SELECT 1 FROM OTNode_IPInfo IP WHERE IP.NodeId = @nodeId)",
                                new { nodeId = nodeToCheck }))
                            {
                                Logger.WriteLine(source,
                                    "Found " + nodeToCheck + " via node API at " + url + ". Inserting node ...");
                                IpInfo.Insert(connection, info);
                            }
                            else
                            {
                                Logger.WriteLine(source,
                                    "Found " + nodeToCheck + " via node API at " + url + ". Updating node ...");

                                IpInfo.UpdateHost(connection, nodeToCheck, node.Hostname, (int)node.Port,
                                    info.Timestamp, info.LastCheckedTimestamp, info.NetworkId);
                            }

                            OTNode_History history = new OTNode_History();
                            history.NodeId = nodeToCheck;
                            history.Timestamp = DateTime.UtcNow;
                            history.Success = true;

                            sw.Stop();

                            history.Duration = sw.ElapsedMilliseconds > 15000
                                ? (short)15000
                                : (short)sw.ElapsedMilliseconds;

                            OTNode_History.Insert(connection, history);
                        }
                    }
                    catch (Exception e)
                    {
                        if (!e.Message.Contains("The operation has timed out"))
                        {
                            var message = e.GetBaseException()?.ToString();

                            Logger.WriteLine(source,
                                "Error checking node online (LoadNodesViaAPI): " + message);
                        }
                        else
                        {
                            Logger.WriteLine(source, "Error which is silently ignored: " + e.GetBaseException().Message);
                        }
                    }
                }
            }
        }

        private static List<List<T>> Split<T>(ICollection<T> collection, int size)
        {
            var chunks = new List<List<T>>();
            var chunkCount = collection.Count() / size;

            if (collection.Count % size > 0)
                chunkCount++;

            for (var i = 0; i < chunkCount; i++)
                chunks.Add(collection.Skip(i * size).Take(size).ToList());

            return chunks;
        }

        private class TimeoutWebClient : WebClient
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