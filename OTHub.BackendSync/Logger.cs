using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace OTHub.BackendSync
{
    public enum Source
    {
        Startup,
        BlockchainSync,
        NodeUptimeAndMisc,
        NodeApi
    }

    public struct LogLine
    {
        public String Text { get; set; }
        public Source Source { get; set; }
    }

    public static class Logger
    {
        private static readonly BlockingCollection<LogLine> _queue = new BlockingCollection<LogLine>();

        static Logger()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    var line = _queue.Take();

                    var original = Console.ForegroundColor;
                    if (line.Source == Source.BlockchainSync)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                    }
                    else if (line.Source == Source.NodeUptimeAndMisc)
                    {
                        Console.ForegroundColor = ConsoleColor.Blue;
                    }
                    else if (line.Source == Source.Startup)
                    {
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                    }
                    else if (line.Source == Source.NodeApi)
                    {
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                    }

                    Console.WriteLine(line.Text);
                    Console.ForegroundColor = original;
                }
            });
        }

        public static void WriteLine(Source source, string text)
        {
            _queue.Add(new LogLine {Source = source, Text = text});
        }
    }
}