using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Agience.Client.MQTT.Model
{
    public class Authority
    {
        private const string BROKER_URI_KEY = "broker_uri";
        private const string OPENID_CONFIG_PATH = "/.well-known/openid-configuration";

        private readonly Uri _authorityUri; // Expect without trailing slash
        private OpenIdConnectConfiguration? _configuration;

        public string? TokenEndpoint => _configuration?.TokenEndpoint;
        public string? BrokerUri => _configuration?.AdditionalData[BROKER_URI_KEY].ToString();        
        public string? Address => $"{Id}/-/-/-";
        public string? Id { get; private set; }

        public Authority(string authorityUri)
        {
            _authorityUri = new Uri(authorityUri);
            Id = _authorityUri.Host;            
        }

        public async Task InitializeAsync()
        {
            var configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                $"{_authorityUri.OriginalString}{OPENID_CONFIG_PATH}",
                new OpenIdConnectConfigurationRetriever());

            _configuration = await configurationManager.GetConfigurationAsync();
        }
    }
}
