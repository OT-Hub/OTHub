using System;

namespace OTHub.BackendSync.Logging
{
    public struct LogLine
    {
        public String Text { get; set; }
        public Source Source { get; set; }
    }
}