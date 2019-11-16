//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Linq;
//using System.Runtime.InteropServices;
//using System.Text.RegularExpressions;
//using System.Threading;
//using System.Threading.Tasks;
//using Cyotek.Collections.Generic;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;

//namespace OTHubApi
//{
//    internal class DockerMonitorService : IHostedService, IDisposable
//    {
//        internal static CircularBuffer<MessageItem> Buffer { get; } = new CircularBuffer<MessageItem>(2000, true);
//        private Timer _timer;
//        private Process _process;

//        public Task StartAsync(CancellationToken cancellationToken)
//        {

//            _timer = new Timer(DoWork, null, TimeSpan.Zero,
//                TimeSpan.FromMinutes(1));

//            return Task.CompletedTask;
//        }

//        private void DoWork(object state)
//        {
//            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
//                return;

//            try
//            {
//                if (_process != null)
//                {
//                    if (!_process.HasExited)
//                        return;
//                }
//            }
//            catch (Exception e)
//            {
//                Console.WriteLine(e);
//                _process = null;
//            }

//            Console.WriteLine("Before docker logs");

//            var escapedArgs = "docker logs --since 24h -f otnode";

//            _process = new Process()
//            {
//                StartInfo = new ProcessStartInfo
//                {
//                    FileName = "/bin/bash",
//                    Arguments = $"-c \"{escapedArgs}\"",
//                    RedirectStandardOutput = true,
//                    UseShellExecute = false,
//                    CreateNoWindow = true,
//                    RedirectStandardError = true
//                }
//            };
//            _process.Exited += ProcessOnExited;

//            _process.OutputDataReceived += ProcessOnOutputDataReceived;
//            _process.ErrorDataReceived += ProcessOnErrorDataReceived;

//            _process.Start();

//            _process.BeginOutputReadLine();
//            _process.BeginErrorReadLine();

//            Console.WriteLine("After docker logs");
//        }

//        private void ProcessOnExited(object sender, EventArgs e)
//        {
//            Console.WriteLine("Exited process for docker logs");
//            Thread.Sleep(10000);
//        }

//        private void ProcessOnErrorDataReceived(object sender, DataReceivedEventArgs e)
//        {
//            //Console.WriteLine("Docker logs: " + e.Data);

//            if (e?.Data == null)
//                return;

//            var data = e.Data;

//            if (data.Contains("Registered with apiKey"))
//                return;

//            DateTime? date = null;
//            var dateIndex = data.IndexOf(" ");
//            if (dateIndex > 0)
//            {
//                var strDate = data.Substring(0, dateIndex);
//                data = data.Substring(dateIndex);
//                if (DateTime.TryParse(strDate, out var dateParse))
//                {
//                    date = dateParse;
//                }
//            }

//            if (data.Contains("http"))
//            {
//                string re1 = "((?:http|https)(?::\\/{2}[\\w]+)(?:[\\/|\\.]?)(?:[^\\s\"]*))";    // HTTP URL 1

//                Regex r = new Regex(re1, RegexOptions.IgnoreCase | RegexOptions.Singleline);
//                data = r.Replace(data, "https://xx.xx.xx.xx");
//            }

//            Buffer.Put(new MessageItem {Date = date, Message = data});
//        }

//        private void ProcessOnOutputDataReceived(object sender, DataReceivedEventArgs e)
//        {
//            //Console.WriteLine("Docker logs: " + e.Data);

//            string data = e.Data;

//            if (data == null)
//                return;

//            if (data.Contains("Registered with apiKey") || data.Contains("\"node_wallet\"") 
//                                                        || data.Contains("\"management_wallet\"")
//                                                        || data.Contains("\"node_private_key\""))
//                return;

//            if (data.Contains("KADemlia error. "))
//            {
//                var splitData = data.Split(new[] {"Request:"}, StringSplitOptions.RemoveEmptyEntries);

//                if (splitData.Length > 1)
//                {
//                    data = splitData[0];
//                }
//            }

//            DateTime? date = null;

//            var dateIndex = data.IndexOf(" ");
//            if (dateIndex > 0)
//            {
//                var strDate = data.Substring(0, dateIndex);
//                data = data.Substring(dateIndex);
//                if (DateTime.TryParse(strDate, out var dateParse))
//                {
//                    date = dateParse;
//                }
//            }

//            if (data.Contains("http"))
//            {
//                string re1 = "((?:http|https)(?::\\/{2}[\\w]+)(?:[\\/|\\.]?)(?:[^\\s\"]*))";    // HTTP URL 1

//                Regex r = new Regex(re1, RegexOptions.IgnoreCase | RegexOptions.Singleline);
//                data = r.Replace(data, "https://xx.xx.xx.xx");
//            }

//            Buffer.Put(new MessageItem { Date = date, Message = data });
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
//    }
//}