using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net;
using Agience.Model;
using System.Security.Claims;
using System.Text;

namespace Agience.Client.MQTT.Model
{
    public class Instance : Agience.Model.Instance
    {
        public List<Agent> Agents { get; set; } = new List<Agent>();
        public Catalog Catalog { get; set; } = new Catalog();
        public bool IsStarted { get; set; }

        private readonly Config _config;

        private Authority _authority;

        private string _clientSecret;

        private Broker _broker;

        private string? _access_token;

        public event Action<object?, string> LogMessage;

        public Instance(Config config)
        {
            _config = config;

            Id = _config.ClientId;
            _authority = new Authority(_config.AuthorityUri); // TODO: null checks
            _clientSecret = _config.ClientSecret;
        }

        private async Task Receive(Template? template)
        {
            if (template?.Id != null && template.InstanceId != Id)
            {
                await Logger.Write($"{template.InstanceId} {template.Id} template receive");

                Catalog.Add(agent => template);
            }
        }

        public async Task Start()
        {
            await _authority.InitializeAsync();

            _broker = new Broker(_config.BrokerUriOverride ?? _authority.BrokerUri ?? throw new ArgumentNullException("BrokerUri"));

            await Authenticate();

            if (_access_token == null) { throw new Exception("Access Token is null"); }

            await _broker.ConnectAsync(_access_token);

            // HERE

            // Subscribe to the instance topic
            // await _broker.SubscribeAsync(_identity.InstanceSubscribeTopic);

            // Send a status message, expect authority to answer with agencies and agents to subscribe            
            // Handle the subscribe process as an event response
        }

        public async Task Stop()
        {
            await _broker.DisconnectAsync();
        }

        internal async Task Authenticate()
        {
            // TODO: Use a shared HttpClient instance
            using (var httpClient = new HttpClient())
            {
                var basicAuthHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{Id}:{_clientSecret}"));
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", basicAuthHeader);

                var parameters = new Dictionary<string, string>
                {
                    { "grant_type", "client_credentials" },
                    { "scope", "connect" }
                };
                
                var httpResponse = await httpClient.PostAsync(_authority.TokenEndpoint, new FormUrlEncodedContent(parameters));

                _access_token = httpResponse.IsSuccessStatusCode ?
                    (await httpResponse.Content.ReadFromJsonAsync<TokenResponse>())?.access_token :
                    throw new HttpRequestException("Unauthorized", null, httpResponse.StatusCode);
            }
        }

        internal class TokenResponse
        {
            public string? access_token { get; set; }
            public string? token_type { get; set; }
            public int? expires_in { get; set; }
        }

    }
}
