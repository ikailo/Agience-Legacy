using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Agience.Client.MQTT.Model
{
    /*
    public class Authority //: Agience.Model.Authority
    {
        public string AuthorityUri { get; private set; } // "https://authority.agience.ai";        
        public string BrokerHost { get; }

        public Authority(string authorityUri)
        {
            AuthorityUri = authorityUri;
            BrokerHost = new Uri(authorityUri.Replace("authority.", "broker.")).Host; // TODO: Get from OIDC
        }
    }*/


    public class Authority
    {
        private readonly string _authorityUri;
        private OpenIdConnectConfiguration? _configuration;

        public string? TokenEndpoint => _configuration?.TokenEndpoint;
        public string? BrokerHost => _configuration?.AdditionalData["broker_host"].ToString();

        public Authority(string authorityUri)
        {
            _authorityUri = authorityUri;
        }

        public async Task InitializeAsync()
        {
            var configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                $"{_authorityUri}/.well-known/openid-configuration",
                new OpenIdConnectConfigurationRetriever());

            _configuration = await configurationManager.GetConfigurationAsync();
        }
    }
}
