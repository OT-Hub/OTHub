//using System;
//using System.Collections.Generic;
//using System.Globalization;
//using System.Threading.Tasks;
//using Microsoft.AspNetCore.SignalR;

//namespace OTHub.APIServer
//{
//    public class LogHub : Hub
//    {
//        public async Task GetAll()
//        {
//            var logs = DockerMonitorService.Buffer.ToArray();

//            await Clients.Caller.SendAsync("GetAll", new AllMessagesLog {Data = logs.Reverse().ToList()});
//        }

//        public async Task GetAfterDate(string strDate)
//        {
//            var date = DateTime.Parse(strDate, null, DateTimeStyles.AssumeUniversal);

//            var logs = DockerMonitorService.Buffer.ToArray().Where(l => l.Date > date);

//            await Clients.Caller.SendAsync("GetAfterDate", new AllMessagesLog { Data = logs.ToList() });
//        }

//        public override async Task OnConnectedAsync()
//        {
//            await base.OnConnectedAsync();
//        }
//    }

//    public class AllMessagesLog
//    {
//        public List<MessageItem> Data { get; set; }
//    }

//    public class MessageItem
//    {
//        public DateTime? Date { get; set; }
//        public String Message { get; set; }
//    }
//}