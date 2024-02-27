using Microsoft.Extensions.Configuration;

namespace Agience.Agents._Console
{
    internal class AppConfig : Client.Config
    {
        public string? AgentId { get; set; }

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
