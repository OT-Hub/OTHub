using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace OTHub.BackendSync.Logging
{
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
                    else if (line.Source == Source.Misc)
                    {
                        Console.ForegroundColor = ConsoleColor.Blue;
                    }
                    else if (line.Source == Source.Tools)
                    {
                        Console.ForegroundColor = ConsoleColor.DarkGray;
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