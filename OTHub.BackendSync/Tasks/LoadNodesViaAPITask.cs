using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Dapper;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using OTHub.BackendSync.Models.Database;
using OTHub.BackendSync.Models.Generated;
using OTHub.Settings;

namespace OTHub.BackendSync.Tasks
{
    public class LoadNodesViaAPITask : TaskRun
    {
        public LoadNodesViaAPITask() : base("Load Nodes via API")
        {
        }

        private static readonly object _lock = new object();

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
Version != 0
GROUP BY NewIdentity, I.NodeId
ORDER BY MAX(x.Timestamp) DESC")
                    .ToArray();

                //{(OTHubSettings.Instance.Blockchain.Network == BlockchainNetwork.Testnet ? "" : "HAVING MAX(x.Timestamp) >= DATE_Add(NOW(), INTERVAL - 8 WEEK)")} 

                Console.WriteLine("Found " + nodesToCheck.Length + " nodes to check via API.");


                var lists = Split<string>(nodesToCheck, nodesToCheck.Length / 12);

                List<Task> tasks = new List<Task>();

                foreach (var list in lists)
                {

                    tasks.Add(Task.Run(async () =>
                    {
                        int counter = 0;

                        foreach (var nodeToCheck in list)
                        {
                            counter++;

                            try
                            {
                                string urlText =
                                    $"{OTHubSettings.Instance.OriginTrailNode.Url}/api/latest/network/get-contact/" +
                                    nodeToCheck;

                                Logger.WriteLine(source,
                                    "Trying " + nodeToCheck + " via node API (" + counter + " of " + list.Count +
                                    "): " + urlText);

                                string strData = GetRequest(urlText);

                                NodeContact data = JsonConvert.DeserializeObject<NodeContact>(strData);

                                bool isOnline = false;

                                try
                                {
                                    string url = $"https://{data.hostname}:{data.port}/";

                                    var request = (HttpWebRequest) WebRequest.Create(url);
                                    request.Timeout = (int) TimeSpan.FromSeconds(140).TotalMilliseconds;
                                    request.AllowAutoRedirect = false;
                                    request.ServerCertificateValidationCallback = delegate(object sender,
                                        X509Certificate certificate,
                                        X509Chain chain, SslPolicyErrors errors)
                                    {

                                        isOnline = true;

                                        return true;
                                    };

                                    HttpWebResponse response = (HttpWebResponse) await request.GetResponseAsync();
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
                                    info.NetworkId = data.network_id;

                                    info.Timestamp = new DateTime(1970, 1, 1).AddMilliseconds(data.timestamp);

                                    if (info.Timestamp.Year < DateTime.Now.Year)
                                    {
                                        info.Timestamp = DateTime.UtcNow;
                                    }


                                    info.LastCheckedTimestamp = DateTime.UtcNow;


                                    lock (_lock)
                                    {
                                        if (connection.ExecuteScalar<bool>(
                                            @"SELECT NOT EXISTS (SELECT 1 FROM OTNode_IPInfo IP WHERE IP.NodeId = @nodeId)",
                                            new {nodeId = nodeToCheck}))
                                        {
                                            Logger.WriteLine(source,
                                                "Found " + nodeToCheck + " via node API! Inserting...");
                                            IpInfo.Insert(connection, info);
                                        }
                                        else
                                        {
                                            Logger.WriteLine(source,
                                                "Found " + nodeToCheck + " via node API! Updating...");

                                            IpInfo.UpdateHost(connection, nodeToCheck, data.hostname, data.port,
                                                info.Timestamp, info.LastCheckedTimestamp, info.NetworkId);
                                        }
                                    }
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
                            }
                        }
                    }));
                }

                Task.WaitAll(tasks.ToArray());
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