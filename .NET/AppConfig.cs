using Microsoft.Extensions.Configuration;

namespace Technologai.Agents.Interaction
{
    internal class AppConfig
    {
        private readonly IConfiguration _config;

        internal string? Authority => _config["authority"];        
        internal string? InstanceId => _config["instanceId"];
        internal string? InstanceSecret => _config["instanceSecret"];        
        internal string? AgentId => _config["agentId"];

        internal AppConfig()
        {
            _config = new ConfigurationBuilder()
            .AddUserSecrets<AppConfig>()
            .Build();
        }
    }
}
