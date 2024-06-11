using Microsoft.Extensions.Configuration;

namespace Agience.Hosts._Console
{
    internal class AppConfig : SDK.HostConfig
    {
        public string? OpenAiApiKey { get; set; }
        public string? CustomNtpHost { get; set; }
    }
}
