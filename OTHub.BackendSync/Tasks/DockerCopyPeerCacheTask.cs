//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Runtime.InteropServices;
//using System.Text;
//using System.Threading.Tasks;

//namespace OTHelperNetStandard.Tasks
//{
//    public class DockerCopyPeerCacheTask : TaskRun
//    {
//        public override async Task Execute(Source source)
//        {
//            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
//            {
//                var escapedArgs = "docker cp otnode:/ot-node/data/peercache /home/ubuntu/updater".Replace("\"", "\\\"");

//                var process = new Process()
//                {
//                    StartInfo = new ProcessStartInfo
//                    {
//                        FileName = "/bin/bash",
//                        Arguments = $"-c \"{escapedArgs}\"",
//                        RedirectStandardOutput = true,
//                        UseShellExecute = false,
//                        CreateNoWindow = true,
//                    }
//                };
//                process.Start();
//                string result = process.StandardOutput.ReadToEnd();
//                if (!String.IsNullOrWhiteSpace(result))
//                {
//                    Console.WriteLine(result);
//                }

//                process.WaitForExit();
//            }
//        }

//        public DockerCopyPeerCacheTask() : base("Docker Copy Peercache")
//        {
//        }
//    }
//}