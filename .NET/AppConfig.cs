using Microsoft.Extensions.Configuration;

namespace Agience.Agents_Console
{
    internal class AppConfig : Client.MQTT.Config
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
