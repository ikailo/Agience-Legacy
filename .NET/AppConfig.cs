using Microsoft.Extensions.Configuration;

namespace Agience.Agents_Console
{
    internal class AppConfig
    {
        private readonly IConfiguration _config;

        internal string AuthorityUri => _config["authorityUri"] ?? throw new ArgumentNullException("authorityUri");
        internal string ClientId => _config["clientId"] ?? throw new ArgumentNullException("clientId");
        internal string ClientSecret => _config["clientSecret"] ?? throw new ArgumentNullException("clientSecret");
        public string? AgentId => _config["agentId"] ?? throw new ArgumentNullException("agentId");

        internal AppConfig()
        {
            _config = new ConfigurationBuilder()
            .AddUserSecrets<AppConfig>()
            .Build();
        }
    }
}
