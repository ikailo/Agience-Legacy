using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Agience.Client
{
    public class Instance
    {
        public event Func<Agent, Task>? AgentConnected;
        public string Id { get; private set; }
        public string? Name { get; private set; }
        public bool IsConnected { get; private set; }
        public IReadOnlyList<Agent> Agents => _agents.Values.ToList().AsReadOnly();

        private readonly Config _config;
        private readonly Authority _authority;
        private readonly Dictionary<string, Agent> _agents = new();
        private readonly Catalog _catalog = new();
        private readonly Broker _broker = new();

        public Instance(Config config)
        {
            _config = config;
            _authority = new Authority(_config.AuthorityUri ?? throw new ArgumentNullException("AuthorityUri"));
            Id = _config.ClientId ?? throw new ArgumentNullException("ClientId");
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
            //await Task.Delay(1000); // Wait for the authority to start. TODO: Skip in production.

            await _authority.Initialize();

            var brokerUri = _config.BrokerUriOverride ?? _authority.BrokerUri ?? throw new ArgumentNullException("BrokerUri");

            var accessToken = await GetAccessToken() ?? throw new ArgumentNullException("accessToken");

            await _broker.Connect(accessToken, brokerUri);

            await _broker.Subscribe(_authority.InstanceTopic("+", "0"), _broker_ReceiveMessage); // All Instances

            await _broker.Subscribe(_authority.InstanceTopic("+", Id), _broker_ReceiveMessage); // This Instance

            await _broker.Publish(new Message()
            {
                Type = MessageType.EVENT,
                Topic = _authority.AuthorityTopic(Id!),
                Payload = new Data(new()
                {
                    { "type", "instanceConnect" },
                    { "timestamp", _broker.Timestamp},
                    { "instance", JsonSerializer.Serialize(ToAgienceModel()) }
                })
            }); ;

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

                await _broker.Unsubscribe(_authority.InstanceTopic("+", "0"));
                await _broker.Unsubscribe(_authority.InstanceTopic("+", Id));

                await _broker.Disconnect();

                IsConnected = false;
            }
        }

        private async Task _broker_ReceiveMessage(Message message)
        {
            if (message.SenderId == null || message.Payload == null) { return; }

            // Incoming Agent Connect Message
            if (message.Type == MessageType.EVENT &&
                message.Payload.Format == DataFormat.STRUCTURED &&
                message.Payload["type"] == "agentConnect" &&
                message.Payload["agent"] != null)
            {
                var timestamp = DateTime.TryParse(message.Payload["timestamp"], out DateTime result) ? (DateTime?)result : null;
                var agent = JsonSerializer.Deserialize<Model.Agent>(message.Payload["agent"]!);

                if (agent == null) { return; } // Invalid Agent

                await AgentConnect(agent, timestamp);
            }
        }

        private async Task AgentConnect(Model.Agent modelAgent, DateTime? timestamp)
        {
            if (modelAgent?.Id == null || modelAgent.Agency?.Id == null || modelAgent.Instance?.Id != Id)
            {
                return; // Invalid Agent
            }

            Agent agent = new(_authority, _broker, modelAgent.Agency)
            {
                Id = modelAgent.Id,
                Name = modelAgent.Name,
            };

            await agent.AddTemplates(_catalog.GetTemplatesForAgent(agent));

            await agent.Connect();

            _agents.Add(agent.Id, agent);

            if (AgentConnected != null)
            {
                _ = Task.Run(() => AgentConnected.Invoke(agent))
                    .ContinueWith(t =>
                    {
                        if (t.IsFaulted)
                        {
                            // Rethrow the exception on the ThreadPool
                            ThreadPool.QueueUserWorkItem(_ => { throw t.Exception; });
                        }
                    });
            }
        }

        private async Task<string?> GetAccessToken()
        {
            var clientSecret = _config.ClientSecret ?? throw new ArgumentNullException("ClientSecret");
            var tokenEndpoint = _authority.TokenEndpoint ?? throw new ArgumentNullException("TokenEndpoint");

            // TODO: Use a shared HttpClient instance
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

        public void AddTemplate<T>(OutputCallback? callback = null) where T : Template, new()
        {
            _catalog.Add<T>(callback);

            foreach (var agent in _agents.Values)
            {
                var template = _catalog.GetTemplateForAgent(typeof(T).FullName!, agent);

                if (template.HasValue)
                {
                    agent.AddTemplate(template.Value);
                }
            }
        }

        internal Model.Instance ToAgienceModel()
        {
            return new Model.Instance
            {
                Id = Id,
                Name = Name
            };
        }

        internal class TokenResponse
        {
            public string? access_token { get; set; }
            public string? token_type { get; set; }
            public int? expires_in { get; set; }
        }
    }
}