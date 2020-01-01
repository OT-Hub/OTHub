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
using OTHub.BackendSync.Models.Database;
using OTHub.Settings;

namespace OTHub.BackendSync.Tasks
{
    public class LoadPeercacheTask : TaskRun
    {
        private static bool _checkAllOnlineOnStartup = true;

        private ConcurrentQueue<IpInfo> LoadPeerCache(Source source, out bool isFullSuccess)
        {
            isFullSuccess = true;

            //string path = "/peercache";

            //if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            //{
            //    path = @"C:\peercache";
            //}

            using (var connection = new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                var allNodeIpInfos = connection.Query<IpInfo>(@"select * from otnode_ipinfo").ToList();

                //if (Directory.Exists(path) && !IsTestNet)
                //{
                //    var files = Directory.GetFiles(path, "*.peercache", SearchOption.TopDirectoryOnly);

                //    Logger.WriteLine(source, files.Length + " peercache files found.");

                //    NodeContactCollection contactCollection =
                //        new NodeContactCollection() {Message = new Dictionary<string, NodeContact>()};

                //    foreach (var file in files)
                //    {
                //        var text = File.ReadAllLines(file);

                //        int success = 0;
                //        int fail = 0;
                //        foreach (var line in text)
                //        {
                //            if (String.IsNullOrWhiteSpace(line) || line.Trim() == "}" || line.Trim() == "c\"}")
                //                continue;

                //            string newLine = line;

                //            if (line.StartsWith("\"contact\":"))
                //            {
                //                newLine = "{" + newLine;
                //            }

                //            NodeContactEntry data;

                //            try
                //            {
                //                data = JsonConvert.DeserializeObject<NodeContactEntry>(newLine);
                //            }
                //            catch (Exception ex)
                //            {
                //                fail++;
                //                Logger.WriteLine(source, "Failed on '" + newLine + "' in file " + file + " ." +  ex.Message);
                //                isFullSuccess = false;
                //                continue;
                //            }

                //            if (data == null)
                //            {
                //                fail++;
                //                Logger.WriteLine(source, "Failed on '" + newLine + "' in file " + file + " .");
                //                isFullSuccess = false;
                //                continue;
                //            }

                //            success++;

                //            if (contactCollection.Message.ContainsKey(data._id))
                //            {
                //                if (contactCollection.Message[data._id].timestamp < data.contact.timestamp)
                //                {
                //                    contactCollection.Message.Remove(data._id);
                //                }
                //                else
                //                {
                //                    continue;
                //                }
                //            }

                //            contactCollection.Message.Add(data._id, data.contact);
                //        }

                //        //var csvFiles = Directory.GetFiles(path, "*.csv", SearchOption.TopDirectoryOnly);

                //        //Logger.WriteLine(source, csvFiles.Length + " csv files found.");

                //        //foreach (var csvFile in csvFiles)
                //        //{
                //        //    try
                //        //    {
                //        //        var lines = File.ReadAllLines(csvFile);


                //        //        Logger.WriteLine(source, csvFile + " has " + lines.Length + " lines.");

                //        //        foreach (var line in lines)
                //        //        {
                //        //            if (String.IsNullOrWhiteSpace(line))
                //        //                continue;

                //        //            var split = line.Split(',', StringSplitOptions.RemoveEmptyEntries);
                //        //            if (split.Length >= 9)
                //        //            {
                //        //                var nodeId = split[0].Trim('"');
                //        //                var ip = split[1].Trim('"');
                //        //                var port = int.Parse(split[3].Trim('"'));
                //        //                string wallet = split[7].Trim('"');
                //        //                var timestamp = long.Parse(split[9].Trim('"'));

                //        //                if (contactCollection.Message.ContainsKey(nodeId))
                //        //                {
                //        //                    if (contactCollection.Message[nodeId].timestamp < timestamp)
                //        //                    {
                //        //                        contactCollection.Message.Remove(nodeId);
                //        //                    }
                //        //                    else
                //        //                    {
                //        //                        continue;
                //        //                    }
                //        //                }

                //        //                contactCollection.Message.Add(nodeId, new NodeContact {hostname = ip, port = port, protocol = "https:", wallet = wallet, timestamp = timestamp});
                //        //            }
                //        //        }
                //        //    }
                //        //    catch (Exception e)
                //        //    {
                //        //        Logger.WriteLine(source, "Failed to load csv " + e);
                //        //    }
                //        //}

                //        if (!isFullSuccess)
                //        {
                //            if (fail < 10 && success > 100)
                //            {
                //                Logger.WriteLine(source, "Overriding 'fail' to 'success' as only one bad entry found for " + file);
                //                isFullSuccess = true;
                //            }
                //        }

                //        Logger.WriteLine(source, file + " found " + success + " nodes in peercache. There are " + fail + " bad entries.");
                //    }


                //    Logger.WriteLine(source, 
                //        $"{allNodeIpInfos.Count} nodes in database, {contactCollection.Message.Count} nodes in memory from files.");

                //    foreach (KeyValuePair<string, NodeContact> contact in contactCollection.Message)
                //    {
                //        IpInfo info = allNodeIpInfos.FirstOrDefault(i => i.NodeId == contact.Key);

                //        if (info == null)
                //        {
                //            Logger.WriteLine(source, "Adding node " + contact.Key + " (host, wallet, port): " + contact.Value.hostname + ", " + contact.Value.wallet + ", " + contact.Value.port);

                //            info = new IpInfo();
                //            info.Hostname = contact.Value.hostname;
                //            info.Wallet = contact.Value.wallet;
                //            info.Port = contact.Value.port;
                //            info.NodeId = contact.Key;
                //            info.Timestamp = new DateTime(1970, 1, 1).AddMilliseconds(contact.Value.timestamp);
                //            try
                //            {
                //                IpInfo.Insert(connection, info);
                //            }
                //            catch (Exception ex)
                //            {
                //                Logger.WriteLine(source, "Skipping node " + contact.Key + " as it failed to write to the db: " + ex.Message);

                //                continue;
                //            }

                //            allNodeIpInfos.Add(info);
                //        }
                //        else
                //        {
                //            if (info.Hostname != contact.Value.hostname 
                //                || info.Wallet != contact.Value.wallet
                //                || info.Port != contact.Value.port)
                //            {
                //                if (contact.Value.hostname != null)
                //                {
                //                    info.Hostname = contact.Value.hostname;
                //                }

                //                if (contact.Value.wallet != null)
                //                {
                //                    info.Wallet = contact.Value.wallet;
                //                }

                //                if (contact.Value.port != 0)
                //                {
                //                    info.Port = contact.Value.port;
                //                }

                //                Logger.WriteLine(source, "Updating " + info.NodeId + " (host, wallet, port): " + contact.Value.hostname + ", " + contact.Value.wallet + ", " + contact.Value.port);

                //                IpInfo.Update(connection, info);
                //            }

                //            var time = new DateTime(1970, 1, 1).AddMilliseconds(contact.Value.timestamp);
                //            if (info.Timestamp < time)
                //            {
                //                info.Timestamp = time;
                //                IpInfo.UpdateTimestamp(connection, info);
                //            }
                //        }
                //    }

                //}
                //else
                //{
                //    Logger.WriteLine(source, "Failed to find directory " + path);
                //}

                return new ConcurrentQueue<IpInfo>(allNodeIpInfos);
            }
        }


        public override async Task Execute(Source source)
        {
            DateTime start = DateTime.UtcNow;

            try
            {
                var nodes = LoadPeerCache(source, out var isFullSuccess);


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

                DateTime end = DateTime.UtcNow;

                TimeSpan diff = end - start;

                CachetLogger.UpdateMetricAndComponent(10, 9, diff, null, (int)TimeSpan.FromMinutes(5).TotalSeconds, isFullSuccess ? (int?)null : 3);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                CachetLogger.FailComponent(10);
            }
        }

        private bool? CheckIfNodeOnline(MySqlConnection connection, IpInfo node, bool checkAllOnline, bool isSecondCheck)
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
                    maxTimeout = (int)(maxTimeout * 0.75);
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

                    var request = (HttpWebRequest)WebRequest.Create(url);
                    request.Timeout = maxTimeout;
                    request.AllowAutoRedirect = false;
                    request.ServerCertificateValidationCallback = delegate (object sender, X509Certificate certificate,
                        X509Chain chain, SslPolicyErrors errors)
                    {
                        node.LastCheckedTimestamp = DateTime.UtcNow;
                        node.Timestamp = history.Timestamp;
                        history.Success = true;

                        sw.Stop();

                        history.Duration = sw.ElapsedMilliseconds > maxTimeout
                            ? (short)maxTimeout
                            : (short)sw.ElapsedMilliseconds;

                        OTNode_History.Insert(connection, history);
                        IpInfo.UpdateTimestampAndLastChecked(connection, node);

                        hasUpdatedDb = true;

                        return true;
                    };

                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();
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

        public LoadPeercacheTask() : base("Load Peercache")
        {
        }
    }
}