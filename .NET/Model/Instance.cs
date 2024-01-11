using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net;

namespace Agience.Client.MQTT.Model
{
    public class Instance : Agience.Model.Instance
    {
        public List<Agent> Agents { get; set; } = new List<Agent>();
        public Catalog Catalog { get; set; }
        
        public bool IsStarted { get; set; }

        private Authority _authority;

        private string _clientId;
        private string _clientSecret;

        private Identity _identity;
        private Broker _broker;
        private Catalog _catalog;

        private JwtSecurityToken? _access_token;
        
        public event Action<object?, string> LogMessage;

        public Instance(string authorityUri, string clientId, string clientSecret)
        {
            _authority = new Authority(authorityUri);
            _clientId = clientId;
            _clientSecret = clientSecret;
        }
              

        public Agent CreateAgent(Action<Agent> configure)
        {
            throw new NotImplementedException();
            /*
            var newAgent = new Agent();
            configure(newAgent);
            Agents.Add(newAgent);
            return newAgent;*/
        }


        


        private async Task Receive(Template? template)
        {
            if (template?.Id != null && template.InstanceId != _identity.InstanceId)
            {
                await Logger.Write($"{template.InstanceId} {template.Id} template receive");

                Catalog.Add(agent => template);
            }
        }

        public async Task Start()
        {

            //await Identity.Authenticate(Authority.BrokerUri);

            //await Logger.Write($"Authenticated");

            await _broker.ConnectAsync();

            await Logger.Write($"Connected");

            //await _broker.SubscribeAsync(_identity.InstanceSubscribeTopic);

            //await Logger.Write($"Subscribed {_identity.InstanceSubscribeTopic}");

            await _broker.Send(new Status(Id));

            //await Task.Delay(5000); // Wait here for a bit to sync up Templates
        }

        public async Task Stop()
        {
            await _broker.DisconnectAsync();
        }

        internal class TokenResponse
        {
            public string? access_token { get; set; }
            public string? token_type { get; set; }
            public int? expires_in { get; set; }
        }

    }
}
