//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.IO;
//using System.Linq;
//using System.Net;
//using System.Net.Http;
//using System.Reflection;
//using System.Runtime.InteropServices;
//using System.Text.RegularExpressions;
//using System.Threading;
//using System.Threading.Tasks;
//using Dapper;
//using Microsoft.Extensions.Hosting;
//using MySql.Data.MySqlClient;
//using Newtonsoft.Json;

//namespace OTHubApi
//{
//    public class JobCreatorService : IHostedService, IDisposable
//    {
//        private Timer _timer;

//        public Task StartAsync(CancellationToken cancellationToken)
//        {

//            _timer = new Timer(DoWork, null, TimeSpan.FromSeconds(60),
//                TimeSpan.FromMinutes(90));

//            return Task.CompletedTask;
//        }

//        private void DoWork(object state)
//        {
//            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || !Program.IsTestNet)
//                return;

//            try
//            {
//                using (var client = new WebClient())
//                {
//                    var data = client.DownloadString($"{Settings.Settings.OriginTrailNode.Url}/api/balance?humanReadable=true");

//                    var balances = JsonConvert.DeserializeObject<BalancesResponse>(data);

//                    if (balances.wallet.ethBalance <= 0.05m)
//                    {
//                        return;
//                    }

//                    var used = balances.profile.minimalStake + balances.profile.reserved;
//                    var remaining = balances.profile.staked - used;

//                    if (remaining <= 50)
//                    {
//                        return;
//                    }
//                }


//                    string json;

//                using (Stream resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("OTHubApi.Abis.importfile.json"))
//                using (StreamReader reader = new StreamReader(resource))
//                {
//                    json = reader.ReadToEnd();
//                }

//                using (var connection = new MySqlConnection(Program.ConString))
//                {
//                    json = json.Replace("**offercount**", connection.ExecuteScalar<Int32>("select count(*) from otoffer where isfinalized = 1").ToString());
//                }

//                HttpClient httpClient = new HttpClient();
//                MultipartFormDataContent form = new MultipartFormDataContent();

//                form.Add(new StringContent("WOT"), "importtype");
//                form.Add(new StringContent("true"), "replicate");
//                form.Add(new StringContent(json), "importfile");
//                HttpResponseMessage response = httpClient.PostAsync($"{Settings.Settings.OriginTrailNode.Url}/api/import", form).GetAwaiter().GetResult();

//                response.EnsureSuccessStatusCode();
//                httpClient.Dispose();
//                string sd = response.Content.ReadAsStringAsync().Result;
//                Console.WriteLine(sd);
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine(ex);
//            }
//        }

//        public Task StopAsync(CancellationToken cancellationToken)
//        {
//            _timer?.Change(Timeout.Infinite, 0);

//            return Task.CompletedTask;
//        }

//        public void Dispose()
//        {
//            _timer?.Dispose();
//        }

//        public class Profile
//        {
//            public decimal minimalStake { get; set; }
//            public decimal reserved { get; set; }
//            public decimal staked { get; set; }
//        }

//        public class Wallet
//        {
//            public string address { get; set; }
//            public decimal ethBalance { get; set; }
//            public decimal tokenBalance { get; set; }
//        }

//        public class BalancesResponse
//        {
//            public Profile profile { get; set; }
//            public Wallet wallet { get; set; }
//        }
//    }
//}