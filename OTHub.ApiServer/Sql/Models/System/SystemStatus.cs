namespace OTHub.APIServer.Sql.Models.System
{
    public class SystemStatus
    {
        public SystemStatusGroup[] Groups { get; set; }
    }

    public class SystemStatusGroup
    {
        public string Name { get; set; }

        public SystemStatusItem[] Items { get; set; }
    }
}
