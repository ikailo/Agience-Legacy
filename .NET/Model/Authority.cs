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

        private readonly string _authorityUri;

        private OpenIdConnectConfiguration? _configuration;

        public string? TokenEndpoint => _configuration?.TokenEndpoint;
        public string? BrokerUri => _configuration?.AdditionalData[BROKER_URI_KEY].ToString();

        public Authority(string authorityUri)
        {
            _authorityUri = authorityUri;
        }

        public async Task InitializeAsync()
        {
            var configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                $"{_authorityUri}{OPENID_CONFIG_PATH}",
                new OpenIdConnectConfigurationRetriever());

            _configuration = await configurationManager.GetConfigurationAsync();
        }
    }
}
