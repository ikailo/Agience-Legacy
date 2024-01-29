using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Agience.Client
{
    public class Instance : Model.Instance
    {
        public delegate Task AgentConnectedEventArgs(Agent agent);
        public event AgentConnectedEventArgs? AgentConnected;

        public delegate Task AgencyConnectedEventArgs(Agency agency);
        public event AgencyConnectedEventArgs? AgencyConnected;

        //public event Action<object?, string> LogMessage;
        private Dictionary<string, Agent> _agents = new();
        public Catalog Catalog { get; set; } = new Catalog();
        public bool IsConnected { get; private set; }

        private readonly Config _config;
        private readonly Authority _authority;

        private Broker? _broker;
        private string _clientSecret;
        private string? _access_token; 

        public Instance(Config config)
        {
            _config = config;

            Id = _config.ClientId ?? throw new ArgumentNullException("ClientId");
            _authority = new Authority(_config.AuthorityUri ?? throw new ArgumentNullException("AuthorityUri"));
            _clientSecret = _config.ClientSecret ?? throw new ArgumentNullException("ClientSecret");
        }

        public async Task Connect()
        {
            await Task.Delay(1000); // Wait for the authority to start. TODO: Skip in production.

            await _authority.InitializeAsync();

            _broker = new Broker(_config.BrokerUriOverride ?? _authority.BrokerUri ?? throw new ArgumentNullException("BrokerUri"));

            await Authenticate();

            if (_access_token == null) { throw new Exception("Access Token is null"); }

            await _broker.ConnectAsync(_access_token);

            // Subscribe to messages directed to all instances.
            await _broker.SubscribeAsync($"+/{_authority.Id}/0/-/-", _broker_ReceiveMessage);

            // Subscribe to messages directed to this instance
            await _broker.SubscribeAsync($"+/{_authority.Id}/{Id}/-/-", _broker_ReceiveMessage);

            // Publish a status message to the authority and request a list of agents and agencies.
            await _broker.PublishAsync(new Message()
            {
                Type = MessageType.EVENT,
                Topic = $"{Id}/{_authority.Id}/-/-/-",
                Payload = new Data(new()
                {
                    { "type", "instanceConnect" },
                    { "instance", JsonSerializer.Serialize<Model.Instance>(this) }
                })
            });

            IsConnected = true;
        }

        private async Task _broker_ReceiveMessage(Message message)
        {
            if (message.SenderId == null || message.Payload?.Structured == null) { return; }

            // Agent Connect Message
            if (message.Type == MessageType.EVENT && message.Payload.Structured?["type"] == "agentConnect")
            {
                var agent = JsonSerializer.Deserialize<Model.Agent>(message.Payload.Structured["agent"]);

                if (agent?.Id == null || agent.Agency?.Id == null || agent.Instance?.Id != Id)
                {
                    return; // Invalid Agent
                }

                if (!_agents.ContainsKey(agent.Id))
                {
                    _agents[agent.Id] = new Agent(_authority)
                    {
                        Id = agent.Id,
                        Name = agent.Name,
                        Instance = this,
                        Agency = new Agency(_authority)
                        {
                            Id = agent.Agency.Id,
                            Name = agent.Agency.Name
                        },
                    };

                    _agents[agent.Id].Connected += async agent => { if (AgentConnected != null) { await AgentConnected.Invoke(agent); } };
                    _agents[agent.Id].Agency!.Connected += async agency => { if (AgencyConnected != null) { await AgencyConnected.Invoke(agency); } };

                };

                if (!_agents[agent.Id].IsConnected)
                {
                    await _agents[agent.Id].Connect(_broker!);
                }
            }
        }

        public async Task Disconnect()
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