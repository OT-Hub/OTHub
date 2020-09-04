using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using MySql.Data.MySqlClient;
using OTHub.BackendSync.Database.Models;
using OTHub.BackendSync.Logging;
using OTHub.Settings;

namespace OTHub.BackendSync.Nodes.Tasks
{
    public class PerformOnlineNodeChecksTask : TaskRun
    {
        private static bool _checkAllOnlineOnStartup = true;

        private ConcurrentQueue<IpInfo> LoadPeerCache(Source source)
        {
            using (var connection = new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                var allNodeIpInfos = connection.Query<IpInfo>(@"select * from otnode_ipinfo").ToList();
                return new ConcurrentQueue<IpInfo>(allNodeIpInfos);
            }
        }


        public override async Task Execute(Source source)
        {
            ConcurrentQueue<IpInfo> nodes = LoadPeerCache(source);

            bool checkAllOnline = _checkAllOnlineOnStartup;

            _checkAllOnlineOnStartup = false;

            void CheckNodes()
            {
                using (var connection = new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
                {
                    while (!nodes.IsEmpty)
                    {
                        if (nodes.TryDequeue(out var node))
                        {
                            var firstCheck = CheckIfNodeOnline(connection, node, checkAllOnline, false);
                            if (firstCheck == false)
                            {
                                if ((DateTime.UtcNow - node.Timestamp).TotalHours <= 48)
                                {
                                    Thread.Sleep(200);
                                    var secondCheck = CheckIfNodeOnline(connection, node, checkAllOnline, true);
                                    if (secondCheck == true)
                                    {
                                        //Logger.WriteLine(source, node.Hostname + " is online (2nd attempt).");
                                    }
                                    else if (secondCheck == false)
                                    {
                                        //Logger.WriteLine(source, node.Hostname + " is offline (2nd attempt).");
                                    }
                                }
                            }
                            else if (firstCheck == true)
                            {
                                //Logger.WriteLine(source, node.Hostname + " is online.");
                            }
                        }
                    }
                }
            }

            var runner1 = Task.Run((Action) CheckNodes);
            var runner2 = Task.Run((Action) CheckNodes);

            await Task.WhenAll(runner1, runner2);
        }

        private bool? CheckIfNodeOnline(MySqlConnection connection, IpInfo node, bool checkAllOnline,
            bool isSecondCheck)
        {
            bool checkIfOnline = false;

            int maxTimeout = 2000;

            if (!node.LastCheckedTimestamp.HasValue)
            {
                maxTimeout = 5000;
                checkIfOnline = true;
            }
            else
            {
                if (checkAllOnline)
                {
                    checkIfOnline = true;
                    if ((DateTime.UtcNow - node.LastCheckedTimestamp.Value).TotalHours >= 12)
                    {
                        maxTimeout = 2000;
                    }
                    else
                    {
                        maxTimeout = 3500;
                    }
                }
                else if ((DateTime.UtcNow - node.Timestamp).TotalDays > 180)
                {
                    maxTimeout = 1500;

                    if ((DateTime.UtcNow - node.LastCheckedTimestamp.Value).TotalDays >= 3)
                    {
                        checkIfOnline = true;
                    }
                }
                else if ((DateTime.UtcNow - node.Timestamp).TotalDays > 90)
                {
                    if ((DateTime.UtcNow - node.LastCheckedTimestamp.Value).TotalDays >= 2)
                    {
                        checkIfOnline = true;
                    }
                }
                else if ((DateTime.UtcNow - node.Timestamp).TotalDays > 30)
                {
                    maxTimeout = 3500;

                    if ((DateTime.UtcNow - node.LastCheckedTimestamp.Value).TotalDays >= 1)
                    {
                        checkIfOnline = true;
                    }
                }
                else if ((DateTime.UtcNow - node.Timestamp).TotalDays > 7)
                {
                    maxTimeout = 5500;

                    if ((DateTime.UtcNow - node.LastCheckedTimestamp.Value).TotalHours > 6)
                    {
                        checkIfOnline = true;
                    }
                }
                else if ((DateTime.UtcNow - node.LastCheckedTimestamp.Value).TotalMinutes >= 4)
                {
                    maxTimeout = 7500;
                    checkIfOnline = true;
                }
            }

            if (checkIfOnline)
            {
                if (isSecondCheck)
                {
                    maxTimeout = (int) (maxTimeout * 0.75);
                }

                OTNode_History history = new OTNode_History();
                history.NodeId = node.NodeId;
                history.Timestamp = DateTime.UtcNow;

                bool hasUpdatedDb = false;

                Stopwatch sw = new Stopwatch();
                sw.Start();

                try
                {
                    string url = $"https://{node.Hostname}:{node.Port}/";

                    var request = (HttpWebRequest) WebRequest.Create(url);
                    request.Timeout = maxTimeout;
                    request.AllowAutoRedirect = false;
                    request.ServerCertificateValidationCallback = delegate(object sender, X509Certificate certificate,
                        X509Chain chain, SslPolicyErrors errors)
                    {
                        node.LastCheckedTimestamp = DateTime.UtcNow;
                        node.Timestamp = history.Timestamp;
                        history.Success = true;

                        sw.Stop();

                        history.Duration = sw.ElapsedMilliseconds > maxTimeout
                            ? (short) maxTimeout
                            : (short) sw.ElapsedMilliseconds;

                        OTNode_History.Insert(connection, history);
                        IpInfo.UpdateTimestampAndLastChecked(connection, node);

                        hasUpdatedDb = true;

                        return true;
                    };

                    HttpWebResponse response = (HttpWebResponse) request.GetResponse();
                }
                catch (Exception ex)
                {
                    if (!hasUpdatedDb)
                    {
                        OTNode_History.Insert(connection, history);
                    }
                }
                finally
                {
                    sw.Stop();
                }

                return history.Success;
            }

            return null;
        }

        public PerformOnlineNodeChecksTask() : base("Perform Node Online Checks")
        {
        }
    }
}