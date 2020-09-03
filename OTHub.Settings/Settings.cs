using System;
using Microsoft.Extensions.Configuration;

namespace OTHub.Settings
{
    public class OTHubSettings
    {
        public InfuraSettings Infura { get; set; }
        public BlockchainSettings Blockchain { get; set; }
        public OriginTrailNodeSettings OriginTrailNode { get; set; }
        public MariaDBSettings MariaDB { get; set; }
        public WebServerSettings WebServer { get; set; }

        public static OTHubSettings Instance { get; private set; }
    
        public OTHubSettings()
        {
            Instance = this;
        }

        public void Validate()
        {
            if (Infura == null)
            {
                throw new Exception("Invalid or missing Infura settings");
            }

            Infura.Validate();

            if (Blockchain == null)
            {
                throw new Exception("Invalid or missing Blockchain settings");
            }

            Blockchain.Validate();

            if (OriginTrailNode == null)
            {
                throw new Exception("Invalid or missing OriginTrailNode settings");
            }

            OriginTrailNode.Validate();

            if (MariaDB == null)
            {
                throw new Exception("Invalid or missing MariaDB settings");
            }

            MariaDB.Validate();

            if (WebServer == null)
            {
                throw new Exception("Invalid or missing WebServer settings");
            }

            WebServer.Validate();
        }

        public void Load(IConfiguration configuration)
        {
            Infura = configuration.GetSection("Infura")
                .Get<InfuraSettings>();

            Blockchain = configuration.GetSection("Blockchain")
                .Get<BlockchainSettings>();

            OriginTrailNode = configuration.GetSection("OriginTrailNode")
                .Get<OriginTrailNodeSettings>();

            MariaDB = configuration.GetSection("MariaDB")
                .Get<MariaDBSettings>();
        
            WebServer = configuration.GetSection("WebServer")
                .Get<WebServerSettings>();


            Validate();
        }
    }
}