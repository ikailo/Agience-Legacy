using Agience.SDK.Mappings;
using AutoMapper;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Agience.SDK
{
    [AutoMap(typeof(Models.Host), ReverseMap = true)]
    public class Host
    {
        public event Func<Agent, Task>? AgentConnected;

        public string Id => _id;
        public string? Name { get; private set; }
        public bool IsConnected { get; private set; }        

        private readonly string _id;
        private readonly string _hostName;
        private readonly string _hostSecret;
        private readonly Authority _authority;
<<<<<<< Updated upstream
        private readonly Broker _broker;        
        private readonly AgentFactory _agentFactory;        
        private readonly PluginRuntimeLoader _pluginRuntimeLoader;        
        private readonly ILogger<Host> _logger;

        private readonly IMapper _mapper;
        private readonly Dictionary<string, Agent> _agents = new();

        //public Host() { }

        internal Host(
            string? hostName, // TODO: HostName should be provided by the Authority in the welcome message.
            string hostId,
            string hostSecret,
            Authority authority,
            Broker broker,
            AgentFactory agentFactory,
            PluginRuntimeLoader pluginRuntimeLoader,
            ILogger<Host> logger)
        {
            _id = !string.IsNullOrEmpty(hostId) ? hostId : throw new ArgumentNullException(nameof(hostId));
            _hostName = !string.IsNullOrEmpty(hostName) ? hostName : _id; // Fallback to Id if no name is provided.
            _hostSecret = !string.IsNullOrEmpty(hostSecret) ? hostSecret : throw new ArgumentNullException(nameof(hostSecret));
            _authority = authority ?? throw new ArgumentNullException(nameof(authority));
            _broker = broker ?? throw new ArgumentNullException(nameof(broker));
            _agentFactory = agentFactory ?? throw new ArgumentNullException(nameof(agentFactory));
            _pluginRuntimeLoader = pluginRuntimeLoader;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
=======

        private readonly Broker _broker;
        
        private readonly Dictionary<string, Agent> _agents = new();
        private readonly Dictionary<string, AgentBuilder> _agentBuilders = new();

        private readonly ServiceCollection _services = new();
        private readonly KernelPluginCollection _plugins = new();        

        private readonly string _clientSecret;
        private readonly string? _brokerUriOverride;

        private readonly IMapper _mapper;

        private readonly ILogger<Host> _logger;

        public Host(
            string name,
            string authorityUri,
            string clientId,
            string clientSecret,
            Broker broker,           
            string? brokerUriOverride = null)
        {
            Id = clientId ?? throw new ArgumentNullException("clientId");
            Name = name ?? throw new ArgumentNullException("name");
            _clientSecret = clientSecret ?? throw new ArgumentNullException("clientSecret");
            _authority = new Authority(authorityUri, broker);
            _broker = broker;         
            _brokerUriOverride = brokerUriOverride;
>>>>>>> Stashed changes
            _mapper = AutoMapperConfig.GetMapper();
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
            await _authority.InitializeWithBackoff();

            var accessToken = await GetAccessToken() ?? throw new ArgumentNullException("accessToken");

            var brokerUri = _authority.BrokerUri ?? throw new ArgumentNullException("BrokerUri");

            await _broker.Connect(accessToken, _authority.BrokerUri!);

            await _broker.Subscribe(_authority.HostTopic("+", "0"), _broker_ReceiveMessage); // All Hosts

            await _broker.Subscribe(_authority.HostTopic("+", Id), _broker_ReceiveMessage); // This Host

            await _broker.PublishAsync(new BrokerMessage()
            {
                Type = BrokerMessageType.EVENT,
                Topic = _authority.AuthorityTopic(Id!),
                Data = new Data
                {
                    { "type", "host_connect" },
                    { "timestamp", _broker.Timestamp},
                    { "host", JsonSerializer.Serialize(_mapper.Map<Host, Models.Host>(this)) }
                    // TODO: Include a list of local plugins and services.
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

        private async Task _broker_ReceiveMessage(BrokerMessage message)
        {
            if (message.SenderId == null || message.Data == null) { return; }

            // Loading Plugins From External
            if (message.Type == BrokerMessageType.EVENT &&
                message.Data?["type"] == "load_plugins") //TODO: Review Message Data
            {
                _logger.LogInformation("Loading Plugins for Agent.");
              
                _pluginRuntimeLoader.SyncPlugins();
              
                _logger.LogInformation("Agent Plugins Loaded.");
            }

            // Incoming Agent Connect Message
            if (message.Type == BrokerMessageType.EVENT &&
                message.Data?["type"] == "agent_connect" &&
                message.Data?["agent"] != null)
            {
                var timestamp = DateTime.TryParse(message.Data?["timestamp"], out DateTime result) ? (DateTime?)result : null;
                var agent = JsonSerializer.Deserialize<Models.Agent>(message.Data?["agent"]!);
                // TODO: Collection of Plugins to Activate

                if (agent == null) { return; } // Invalid Agent

                await ReceiveAgentConnect(agent, timestamp);
            }
        }

        private async Task ReceiveAgentConnect(Models.Agent modelAgent, DateTime? timestamp)
        {
            if (modelAgent?.Id == null || modelAgent.Agency?.Id == null || modelAgent.Host?.Id != Id)
            {
                return; // Invalid Agent
            }

            var agent = _agentFactory.CreateAgent(modelAgent);

            await agent.Connect();

            _agents.Add(agent.Id!, agent);

            _logger.LogInformation($"{agent.Name} Connected");

            if (AgentConnected != null)
            {
                await AgentConnected.Invoke(agent);
            }

            // ***** Adding a short delay to accept incoming messages, set defaults, etc.
            // TODO: Improve this. Maybe not needed now that we're using SDK.
            // await Task.Delay(5000);
            // *****

            //  *******************************
            // TODO: Add remote plugins/functions (MQTT, GRPC, HTTP) that we want the Agent Kernels to consider local.
            // TODO: Probably this should be done in the Functions themselves, so it can be dynamic and lazy initialized.
            // _host.ImportPluginFromGrpcFile("path-to.proto", "plugin-name");
            //  *******************************

            // Agent instantiation is initiated from Authority. The Host does not have control.
            // Returns an agent that has access to all the local & psuedo-local functions
            // Agent has an Agency which connects them directly to other agents who are experts in their domain.            

        }

        private async Task<string?> GetAccessToken()
        {
            var clientSecret = _hostSecret;
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

        public Agent? GetAgentById(string? agentId)
        {
            return !string.IsNullOrEmpty(agentId) && _agents.TryGetValue(agentId!, out var agent) ? agent : null;
        }

        public Agent? GetAgentByAgencyId(string? agencyId)
        {
            return !string.IsNullOrEmpty(agencyId) ? _agents.Values.FirstOrDefault(agent => agent.Agency?.Id == agencyId) : null;
        }

        internal class TokenResponse
        {
            public string? access_token { get; set; }
            public string? token_type { get; set; }
            public int? expires_in { get; set; }
        }
    }
}