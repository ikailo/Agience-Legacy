using Microsoft.SemanticKernel;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Agience.Client
{
    public class Host
    {
        public event Func<AgentBuilder, Task>? AgentBuilding;
        public event Func<Agent, Task>? AgentConnected;
        public event Func<Agent, Task>? AgentReady;

        public string Id { get; private set; }
        public string? Name { get; private set; }
        public bool IsConnected { get; private set; }

        //public IReadOnlyDictionary<string, string?> AgentNames => _agents.ToDictionary(a => a.Key, a => a.Value.Name);

        private readonly Authority _authority;
        private readonly Broker _broker = new();
        private readonly Dictionary<string, Agent> _agents = new();
        private readonly Dictionary<string, AgentBuilder> _agentBuilders = new();
        private readonly KernelPluginCollection _plugins = new();

        private readonly string _clientSecret;
        private readonly string? _brokerUriOverride;

        public Host(string name, string authorityUri, string clientId, string clientSecret, string? brokerUriOverride = null)
        {
            this.Id = clientId ?? throw new ArgumentNullException("clientId");
            this.Name = name ?? throw new ArgumentNullException("name");
            _clientSecret = clientSecret ?? throw new ArgumentNullException("clientSecret");
            _authority = new Authority(authorityUri ?? throw new ArgumentNullException("authorityUri"));
            _brokerUriOverride = brokerUriOverride;
        }

        public async Task Run()
        {
            await Connect();

            do { await Task.Delay(10); } while (IsConnected);
        }

        public async Task Stop()
        {
            await Disconnect();
        }

        private async Task Connect()
        {
            await _authority.Initialize();

            var brokerUri = (string.IsNullOrEmpty(_brokerUriOverride) ? _authority.BrokerUri : _brokerUriOverride) ?? throw new ArgumentNullException("BrokerUri");

            var accessToken = await GetAccessToken() ?? throw new ArgumentNullException("accessToken");

            await _broker.Connect(accessToken, brokerUri);

            await _broker.Subscribe(_authority.HostTopic("+", "0"), _broker_ReceiveMessage); // All Hosts

            await _broker.Subscribe(_authority.HostTopic("+", Id), _broker_ReceiveMessage); // This Host

            await _broker.PublishAsync(new Message()
            {
                Type = MessageType.EVENT,
                Topic = _authority.AuthorityTopic(Id!),
                Data = new Data
                {
                    { "type", "host_connect" },
                    { "timestamp", _broker.Timestamp},
                    { "host", JsonSerializer.Serialize(ToAgienceModel()) }
                }
            });

            IsConnected = true;
        }

        private async Task Disconnect()
        {
            if (IsConnected)
            {
                foreach (Agent agent in _agents.Values)
                {
                    await agent.Disconnect();
                }

                await _broker.Unsubscribe(_authority.HostTopic("+", "0"));
                await _broker.Unsubscribe(_authority.HostTopic("+", Id));

                await _broker.Disconnect();

                IsConnected = false;
            }
        }

        private async Task _broker_ReceiveMessage(Message message)
        {
            if (message.SenderId == null || message.Data == null) { return; }

            // Incoming Agent Connect Message
            if (message.Type == MessageType.EVENT &&
                message.Data?["type"] == "agent_connect" &&
                message.Data?["agent"] != null)
            {
                var timestamp = DateTime.TryParse(message.Data?["timestamp"], out DateTime result) ? (DateTime?)result : null;
                var agent = JsonSerializer.Deserialize<Model.Agent>(message.Data?["agent"]!);
                // TODO: Collection of Plugins to Activate

                if (agent == null) { return; } // Invalid Agent

                await ReceiveAgentConnect(agent, timestamp);
            }
        }

        private async Task ReceiveAgentConnect(Model.Agent modelAgent, DateTime? timestamp)
        {
            if (modelAgent?.Id == null || modelAgent.Agency?.Id == null || modelAgent.Host?.Id != Id)
            {
                return; // Invalid Agent
            }

            var builder = _agentBuilders[modelAgent.Agency.Id]
                .WithName(modelAgent.Name)
                .WithPlugins(_plugins); // TODO: Select plugins based on message from Authority.
                                        // For now, we're just adding all plugins to all agents.

            if (AgentBuilding != null)
            {   
                await AgentBuilding.Invoke(builder);
            }

            var agent = builder.Build();

            await agent.Connect();

            _agents.Add(agent.Id!, agent);

            if (AgentConnected != null)
            {
                await AgentConnected.Invoke(agent);
            }
            // Adding a short delay to accept incoming Templates, set defaults, etc.
            // TODO: Improve this. Maybe not needed now that we're using SK.
            await Task.Delay(5000);

            if (AgentReady != null)
            {
                await AgentReady.Invoke(agent);
            }
        }

        private async Task<string?> GetAccessToken()
        {
            var clientSecret = _clientSecret;
            var tokenEndpoint = _authority.TokenEndpoint ?? throw new ArgumentNullException("tokenEndpoint");

            // TODO: Use a shared HttpClient host
            using (var httpClient = new HttpClient())
            {
                var basicAuthHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{Id}:{clientSecret}"));
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", basicAuthHeader);

                var parameters = new Dictionary<string, string>
                {
                    { "grant_type", "client_credentials" },
                    { "scope", "connect" }
                };

                var httpResponse = await httpClient.PostAsync(tokenEndpoint, new FormUrlEncodedContent(parameters));

                return httpResponse.IsSuccessStatusCode ?
                    (await httpResponse.Content.ReadFromJsonAsync<TokenResponse>())?.access_token :
                    throw new HttpRequestException("Unauthorized", null, httpResponse.StatusCode);
            }
        }

        internal Model.Host ToAgienceModel()
        {
            return new Model.Host
            {
                Id = Id,
                Name = Name
            };
        }

        public void ImportPluginFromType<T>(string? pluginName = null, IServiceProvider? serviceProvider = null)
        {
            _plugins.AddFromType<T>(pluginName, serviceProvider);
        }

        public void AddPlugins(IEnumerable<KernelPlugin> plugins)
        {
            _plugins.AddRange(plugins);
        }

        public void AddAgentBuilder(string name, AgentBuilder agentBuilder)
        {
            _agentBuilders.Add(name, agentBuilder);
        }

        public Agent? GetAgent(string? agentId)
        {
            return !string.IsNullOrEmpty(agentId) && _agents.ContainsKey(agentId) ? _agents[agentId!] : null;
        }

        internal class TokenResponse
        {
            public string? access_token { get; set; }
            public string? token_type { get; set; }
            public int? expires_in { get; set; }
        }
    }
}