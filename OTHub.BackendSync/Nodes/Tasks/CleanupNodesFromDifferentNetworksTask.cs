using Dapper;
using Newtonsoft.Json;
using OTHub.BackendSync.Database.Models;
using OTHub.BackendSync.Logging;
using OTHub.BackendSync.Nodes.Models;
using OTHub.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using MySqlConnector;

namespace OTHub.BackendSync.Nodes.Tasks
{
    public class CleanupNodesFromDifferentNetworksTask : TaskRun
    {
        public CleanupNodesFromDifferentNetworksTask() : base("Cleanup Nodes from Different Networks")
        {

        }

        public override async Task Execute(Source source)
        {
            using (var connection = new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
 

                string[] nodesToCheck = connection.Query<String>(@"SELECT distinct i.NodeId
	  FROM otnode_ipinfov2 i
	  WHERE i.LastCheckedGetContactTimestamp IS NULL OR i.LastCheckedGetContactTimestamp <= DATE_Add(NOW(), INTERVAL -23 HOUR)").ToArray();

                foreach (string nodeToCheck in nodesToCheck)
                {
                    string urlText =
              $"{OTHubSettings.Instance.OriginTrailNode.Url}/api/latest/network/get-contact/" +
              nodeToCheck;

                    ContactClass node;

                    try
                    {
                        string strData = GetRequest(urlText);

                        Dictionary<string, object> dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(strData);
                        Newtonsoft.Json.Linq.JArray array = dict["contact"] as Newtonsoft.Json.Linq.JArray;
                        node = JsonConvert.DeserializeObject<ContactClass>(array.Last.ToString());

                        string nodeIDInResponse = array.First.ToString();

                        //Wrong responses come back... not sure why
                        //Maybe these are correct in the P2P network stack but until that's understood more lets not ruin the data in othub
                        if (nodeIDInResponse?.ToLower() != nodeToCheck.ToLower())
                        {
                            IpInfo.UnknownResponse(connection, nodeToCheck);

                            continue;
                        }
                    }
                    catch (Exception e)
                    {
                        if (!e.Message.Contains("The operation has timed out"))
                        {
                            var message = e.GetBaseException()?.ToString();

                            Logger.WriteLine(source,
                                "Error in CleanupNodesFromDifferentNetworksTask for node ID " + nodeToCheck + ": " + message);
                        }
                        else
                        {
                            Logger.WriteLine(source, "Error which is silently ignored for node ID " + nodeToCheck + ": " + e.GetBaseException().Message);
                        }

                        continue;
                    }

                    IpInfo info = new IpInfo()
                    {
                        NodeId = nodeToCheck,
                        Hostname = node.Hostname,
                        Port = (int)node.Port,
                        NetworkId = node.NetworkId,
                        Wallet = node.Wallet,
                        Timestamp = new DateTime(1970, 1, 1).AddMilliseconds(node.Timestamp)
                    };

                    IpInfo.UpdateForCleanupNodesFromDifferentNetworks(connection, info);
                }
            }
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