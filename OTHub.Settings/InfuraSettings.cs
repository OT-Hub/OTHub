using System;

namespace OTHub.Settings
{
    public class InfuraSettings
    {
        public String Url { get; set; }

        public void Validate()
        {
            if (String.IsNullOrWhiteSpace(Url))
            {
                throw new Exception("Missing Url in Infura Settings.");
            }
        }
    }
}