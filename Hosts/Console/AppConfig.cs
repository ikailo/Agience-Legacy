using Microsoft.Extensions.Configuration;

namespace Agience.Hosts._Console
{
    internal class AppConfig : SDK.HostConfig
    {
        public string? AgentId { get; set; }

        public string? OpenAiApiKey { get; set; }
        public string? CustomNtpHost { get; set; }


        internal AppConfig()
            : base()
        {
            var configuration = new ConfigurationBuilder()
                .AddUserSecrets<AppConfig>()
                .Build();

            configuration.Bind(this);
        }
    }
}
