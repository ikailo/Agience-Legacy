namespace Agience.SDK
{
    public class HostConfig
    {
        public string? AuthorityUri { get; set; }
        public string? HostName { get; set; } // TODO: Eliminate HostName parameter in Config. It is set by the Authority.
        public string? HostId { get; set; }
        public string? HostSecret { get; set; }       
    }
}