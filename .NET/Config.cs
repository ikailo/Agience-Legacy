using Microsoft.Extensions.Configuration;

namespace Agience.Client
{
    public class Config
    { 
        public string? AuthorityUri { get; set; }
        public string? ClientId { get; set; }
        public string? ClientSecret { get; set; }
        public string? BrokerUriOverride { get; set; }

        public Config()
        {
            var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

            new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environmentName}.json", optional: true)                
                .AddEnvironmentVariables()
                .Build()
                .Bind(this);
        }
    }
}