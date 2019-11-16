using System;

namespace OTHub.Settings
{
    public class WebServerSettings
    {
        public String AccessControlAllowOrigin { get; set; }

        public void Validate()
        {
            if (String.IsNullOrWhiteSpace(AccessControlAllowOrigin))
            {
                throw new Exception("AccessControlAllowOrigin is missing in WebServer settings.");
            }
        }
    }
}