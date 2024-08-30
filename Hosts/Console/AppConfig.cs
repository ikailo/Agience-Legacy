using Agience.SDK;

namespace Agience.Hosts._Console
{
    internal class AppConfig : HostConfig
    {
        public string? CustomNtpHost { get; set; }
        public string? HostOpenAiApiKey { get; set; }
    }
}
