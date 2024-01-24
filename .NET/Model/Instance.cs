using Agience.Model;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;

namespace Agience.Client.MQTT.Model
{
    public class Instance : Agience.Model.Instance
    {
        public event Action<object?, string> LogMessage;
        public List<Agent> Agents { get; set; } = new List<Agent>();
        public Catalog Catalog { get; set; } = new Catalog();
        public bool IsStarted { get; set; }       

        private readonly Config _config;
        private readonly Authority _authority;
        
        private Broker _broker;
        private string _clientSecret;        
        private string? _access_token;        

        public Instance(Config config)
        {
            _config = config;

            Id = _config.ClientId ?? throw new ArgumentNullException("ClientId");
            _authority = new Authority(_config.AuthorityUri ?? throw new ArgumentNullException("AuthorityUri")); 
            _clientSecret = _config.ClientSecret ?? throw new ArgumentNullException("ClientSecret");            
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
            //await Task.Delay(2000); // Wait for the authority to start. TODO: Skip in production.

            await _authority.InitializeAsync();

            _broker = new Broker(_config.BrokerUriOverride ?? _authority.BrokerUri ?? throw new ArgumentNullException("BrokerUri"));

            await Authenticate();

            if (_access_token == null) { throw new Exception("Access Token is null"); }

            await _broker.ConnectAsync(_access_token);

            // Subscribe to messages directed to all instances
            await _broker.SubscribeAsync($"{_authority.Id}/0/-/-");

            // Subscribe to messages directed to this instance
            await _broker.SubscribeAsync($"{_authority.Id}/{Id}/-/-");

            // TODO: What if this is an authority?
            
            // HERE

            // Send a status message, expect authority to answer with agencies and agents to subscribe            
            // Handle the subscribe process as an event response

            IsStarted = true;
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