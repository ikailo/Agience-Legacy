using Microsoft.Extensions.Configuration;

namespace Agience.Client.MQTT
{
    internal class AppConfig
    {
        private readonly IConfiguration _config;

        internal AppConfig()
        {
            _config = new ConfigurationBuilder()
            .AddUserSecrets<AppConfig>()
            .Build();
        }
    }
}
