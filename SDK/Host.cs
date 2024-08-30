﻿using Agience.SDK.Mappings;
using Agience.SDK.Models.Messages;
using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Agience.SDK
{
    [AutoMap(typeof(Models.Entities.Host), ReverseMap = true)]
    public class Host
    {
        public event Func<Agent, Task>? AgentConnected;
        public event Func<Agency, Task>? AgencyConnected;

        public string Id => _id;
        public bool IsConnected { get; private set; }

        public IReadOnlyDictionary<string, Agent> Agents => _agents;
        public IReadOnlyDictionary<string, Agency> Agencies => _agencies;

        private readonly string _id;
        private readonly string _hostSecret;
        private readonly Authority _authority;
        private readonly Broker _broker;
        private readonly AgentFactory _agentFactory;
        //private readonly PluginRuntimeLoader _pluginRuntimeLoader;
        private readonly ILogger<Host> _logger;
        private readonly IMapper _mapper;

        private readonly Dictionary<string, Agent> _agents = new();
        private readonly Dictionary<string, Agency> _agencies = new();

        public ServiceCollection Services { get; } = new();

        internal Host(
            string hostId,
            string hostSecret,
            Authority authority,
            Broker broker,
            AgentFactory agentFactory,
            //PluginRuntimeLoader pluginRuntimeLoader,
            ILogger<Host> logger)
        {
            _id = !string.IsNullOrEmpty(hostId) ? hostId : throw new ArgumentNullException(nameof(hostId));
            _hostSecret = !string.IsNullOrEmpty(hostSecret) ? hostSecret : throw new ArgumentNullException(nameof(hostSecret));
            _authority = authority ?? throw new ArgumentNullException(nameof(authority));
            _broker = broker ?? throw new ArgumentNullException(nameof(broker));
            _agentFactory = agentFactory ?? throw new ArgumentNullException(nameof(agentFactory));
            // _pluginRuntimeLoader = pluginRuntimeLoader;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _mapper = AutoMapperConfig.GetMapper();
        }

        public async Task RunAsync()
        {
            await StartAsync();

            while (IsConnected)
            {
                await Task.Delay(100);
            }
        }

        public async Task Stop()
        {
            _logger.LogInformation("Stopping Host");

            await Disconnect();
        }

        public async Task StartAsync()
        {
            _logger.LogInformation("Starting Host");

            while (!IsConnected)
            {
                try
                {   
                    await Connect();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unable to Connect");
                    _logger.LogInformation("Retrying in 10 seconds");

                    await Task.Delay(10 * 1000); // TODO: With backoff
                }
            }
        }

        private async Task Connect()
        {
            _logger.LogInformation("Connecting Host");

            await _authority.InitializeWithBackoff();

            var brokerUri = _authority.BrokerUri ?? throw new ArgumentNullException("BrokerUri");

            var accessToken = await GetAccessToken() ?? throw new ArgumentNullException("accessToken");

            await _broker.Connect(accessToken, _authority.BrokerUri!);

            if (_broker.IsConnected)
            {
                await _broker.Subscribe(_authority.HostTopic("+", "0"), _broker_ReceiveMessage); // Hosts Broadcast

                await _broker.Subscribe(_authority.HostTopic("+", Id), _broker_ReceiveMessage); // This Host

                await _broker.PublishAsync(new BrokerMessage()
                {
                    Type = BrokerMessageType.EVENT,
                    Topic = _authority.AuthorityTopic(Id!),
                    Data = new Data
                {
                    { "type", "host_connect" },
                    { "timestamp", _broker.Timestamp},
                    { "host", JsonSerializer.Serialize(_mapper.Map<Models.Entities.Host>(this)) }
                }
                });

                IsConnected = true;
            }
            else
            {
                throw new Exception("Broker Connection Failed");
            }

            _logger.LogInformation("Host Connected");
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

                if (httpResponse.IsSuccessStatusCode)
                {
                    return (await httpResponse.Content.ReadFromJsonAsync<TokenResponse>())?.access_token;
                }

                return null;
            }
        }

        public void AddPluginFromType<T>(string name) where T : class
        {
            _agentFactory.AddHostPluginFromType<T>(name);
        }

        private async Task ReceiveHostWelcome(Models.Entities.Host modelHost, IEnumerable<Models.Entities.Plugin> modelPlugins, IEnumerable<Models.Entities.Agent> modelAgents)
        {
            foreach (var modelPlugin in modelPlugins)
            {
                _agentFactory.AddHostPlugin(modelPlugin);
            }

            foreach (var modelAgent in modelAgents)
            {
                await ReceiveAgentConnect(modelAgent, null);
            }
        }

        private async Task ReceiveAgentConnect(Models.Entities.Agent modelAgent, DateTime? timestamp)
        {
            // Creates an Agent configured with the plugins and functions.
            // Agent instantiation is initiated from Authority. The Host does not have control.
            // Agent has an Agency which connects them directly to other Agents in the Agency.

            var agent = _agentFactory.CreateAgent(modelAgent, Services);

            // Connect the Agency first
            if (!_agencies.ContainsKey(agent.Agency.Id))
            {
                _agencies.Add(agent.Agency.Id, agent.Agency);

                await agent.Agency.Connect();

                _logger.LogInformation($"{agent.Agency.Name} Connected");
            }

            if (AgencyConnected != null)
            {
                await AgencyConnected.Invoke(agent.Agency);
            }

            // Connect the Agent now
            _agents[agent.Id!] = agent;

            await agent.Connect();

            _logger.LogInformation($"{agent.Name} Connected");

            if (AgentConnected != null)
            {
                await AgentConnected.Invoke(agent);
            }

            //var response = await agent.PromptAsync("Hello");

            // agent.AutoStart();

            //  *******************************
            // TODO: Add remote plugins/functions (MQTT, GRPC, HTTP) that we want the Agent Kernels to consider local.
            // TODO: Probably this should be done in the Functions themselves, so it can be dynamic and lazy initialized.
            // _host.ImportPluginFromGrpcFile("path-to.proto", "plugin-name");
            //  *******************************
        }


        private async Task _broker_ReceiveMessage(BrokerMessage message)
        {
            if (message.SenderId == null || message.Data == null) { return; }

            /*
            // Loading Plugins From External
            if (message.Type == BrokerMessageType.EVENT &&
                message.Data?["type"] == "load_plugins") //TODO: Review Message Data
            {
                _logger.LogInformation("Loading Plugins for Agent.");

                _pluginRuntimeLoader.SyncPlugins();

                _logger.LogInformation("Agent Plugins Loaded.");
            }*/

            // Incoming Host Welcome Message
            if (message.Type == BrokerMessageType.EVENT &&
                message.Data?["type"] == "host_welcome" &&
                message.Data?["host"] != null)
            {
                var host = JsonSerializer.Deserialize<Models.Entities.Host>(message.Data?["host"]!);
                var plugins = JsonSerializer.Deserialize<IEnumerable<Models.Entities.Plugin>>(message.Data?["plugins"]!) ?? [];
                var agents = JsonSerializer.Deserialize<IEnumerable<Models.Entities.Agent>>(message.Data?["agents"]!) ?? [];

                if (host?.Id == null)
                {
                    _logger.LogError("Invalid Host");
                }
                else
                {
                    _logger.LogInformation($"Received Host Welcome Message from {host.Name}");

                    await ReceiveHostWelcome(host, plugins, agents);
                }
            }
            /*
            // Incoming Agent Connect Message
            if (message.Type == BrokerMessageType.EVENT &&
                message.Data?["type"] == "agent_connect" &&
                message.Data?["agent"] != null)
            {
                var timestamp = DateTime.TryParse(message.Data?["timestamp"], out DateTime result) ? (DateTime?)result : null;
                var agent = JsonSerializer.Deserialize<Models.Entities.Agent>(message.Data?["agent"]!);

                if (agent?.Id == null || agent.Agency?.Id == null)
                {
                    _logger.LogError("Invalid Agent");                    
                }
                else
                {
                    await ReceiveAgentConnect(agent, timestamp);
                }
            }*/
        }

        internal class TokenResponse
        {
            public string? access_token { get; set; }
            public string? token_type { get; set; }
            public int? expires_in { get; set; }
        }
    }
}