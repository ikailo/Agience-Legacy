using AutoMapper;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.Text.Json;
using Agience.SDK.Mappings;
using Microsoft.Extensions.Logging;
using Agience.SDK.Models.Entities;
using Agience.SDK.Models.Messages;

namespace Agience.SDK
{
    public class Authority
    {
        private const string BROKER_URI_KEY = "broker_uri";
        private const string OPENID_CONFIG_PATH = "/.well-known/openid-configuration";

        private readonly IAuthorityDataAdapter _authorityDataAdapter;
        private readonly IHostDataAdapter _hostDataAdapter;

        public string Id => _authorityUri.Host;
        public string? BrokerUri { get; private set; }
        public string? TokenEndpoint { get; private set; }
        public bool IsConnected { get; private set; }
        public string Timestamp => _broker.Timestamp;

        private readonly Uri _authorityUri; // Expect without trailing slash
        private readonly Broker _broker;
        private readonly ILogger<Authority> _logger;
        private readonly IMapper _mapper;

        public Authority() { }

        public Authority(string authorityUri, Broker broker, IAuthorityDataAdapter authorityDataAdapter, ILogger<Authority>? logger = null)
        {
            _authorityUri = !string.IsNullOrEmpty(authorityUri) ? new Uri(authorityUri) : throw new ArgumentNullException(nameof(authorityUri));
            _broker = broker ?? throw new ArgumentNullException(nameof(broker));
            _authorityDataAdapter = authorityDataAdapter ?? throw new ArgumentNullException(nameof(authorityDataAdapter));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _mapper = AutoMapperConfig.GetMapper();
        }

        public Authority(string authorityUri, Broker broker, IHostDataAdapter hostDataAdapter, ILogger<Authority>? logger = null)
        {
            _authorityUri = !string.IsNullOrEmpty(authorityUri) ? new Uri(authorityUri) : throw new ArgumentNullException(nameof(authorityUri));
            _broker = broker ?? throw new ArgumentNullException(nameof(broker));
            _hostDataAdapter = hostDataAdapter ?? throw new ArgumentNullException(nameof(hostDataAdapter));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _mapper = AutoMapperConfig.GetMapper();
        }

        internal async Task InitializeWithBackoff(double maxDelaySeconds = 16)
        {
            if (!string.IsNullOrEmpty(BrokerUri) && !string.IsNullOrEmpty(TokenEndpoint))
            {
                _logger.LogInformation("Authority already initialized.");
                return;
            }

            var delay = TimeSpan.FromSeconds(1);

            while (true)
            {
                try
                {
                    _logger.LogInformation($"Initializing Authority: {_authorityUri.OriginalString}");

                    var configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                    $"{_authorityUri.OriginalString}{OPENID_CONFIG_PATH}",
                    new OpenIdConnectConfigurationRetriever());

                    var configuration = await configurationManager.GetConfigurationAsync();

                    BrokerUri = configuration?.AdditionalData[BROKER_URI_KEY].ToString();
                    TokenEndpoint = configuration?.TokenEndpoint;

                    _logger.LogInformation($"Authority initialized.");

                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex.ToString());
                    _logger.LogInformation($"Unable to initialize Authority. Retrying in {delay.TotalSeconds} seconds.");

                    await Task.Delay(delay);

                    delay = TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds * 2, maxDelaySeconds));
                }
            }
        }

        public async Task Connect(string accessToken)
        {
            if (!IsConnected)
            {
                if (string.IsNullOrEmpty(BrokerUri))
                {
                    await InitializeWithBackoff();
                }

                var brokerUri = BrokerUri ?? throw new ArgumentNullException("BrokerUri");

                await _broker.Connect(accessToken, brokerUri);
                await _broker.Subscribe(AuthorityTopic("+"), async message => await _broker_ReceiveMessage(message));
                IsConnected = true;
            }
        }

        public async Task Disconnect()
        {
            if (IsConnected)
            {
                await _broker.Unsubscribe(AuthorityTopic("+"));
                await _broker.Disconnect();
                IsConnected = false;
            }
        }

        private async Task _broker_ReceiveMessage(BrokerMessage message)
        {
            if (message.SenderId == null ||
                message.Data == null //|| 
                                     //message.Payload.Format != DataFormat.STRUCTURED
                ) { return; }

            if (message.Type == BrokerMessageType.EVENT &&
                message.Data?["type"] == "host_connect" &&
                message.Data?["host"] != null)
            {
                var host = JsonSerializer.Deserialize<Models.Entities.Host>(message.Data?["host"]!);

                // TODO: Move to seperate method
                if (host?.Id == message.SenderId)
                {
                    await OnHostConnected(host);
                }
            }
        }

        private async Task OnHostConnected(Models.Entities.Host host)
        {
            if (_authorityDataAdapter == null) { throw new ArgumentNullException(nameof(_authorityDataAdapter)); }

            _logger.LogInformation($"Received hostConnected from: {host.Name}");

            // TODO: Respond with a host-welcome message. Include the host's name, plugins, and agents.

            foreach (Plugin plugin in await _authorityDataAdapter.GetPluginsForHostIdAsync(host.Id!))
            {
                // TODO: PublishHostLoadPluginEvent(plugin);
            }
        }

        private void PublishAgentConnectEvent(Models.Entities.Agent agent)
        {
            throw new NotImplementedException();
            /*
            if (!IsConnected) { throw new InvalidOperationException("Not Connected"); }

            if (agent.Host?.Id == null) { throw new ArgumentNullException(nameof(agent.Host.Id)); }

            _broker.Publish(new BrokerMessage()
            {
                Type = BrokerMessageType.EVENT,
                Topic = HostTopic(Id, agent.Host.Id),
                Data = new Data
                {
                    { "type", "agent_connect" },
                    { "timestamp", _broker.Timestamp},
                    { "agent", JsonSerializer.Serialize(_mapper.Map<Models.Agent>(agent)) }                    
                }
            });*/
        }

        private void PublishAgentDisconnectEvent(Models.Entities.Agent agent)
        {
            throw new NotImplementedException();
            /*
            if (!IsConnected) { throw new InvalidOperationException("Not Connected"); }

            if (agent.Host?.Id == null) { throw new ArgumentNullException(nameof(agent.Host.Id)); }

            _broker!.Publish(new BrokerMessage()
            {
                Type = BrokerMessageType.EVENT,
                Topic = HostTopic(Id, agent.Host.Id),
                Data = new Data
                {
                    { "type", "agent_disconnect" },
                    { "timestamp", _broker.Timestamp},
                    { "agent", JsonSerializer.Serialize(_mapper.Map<Models.Agent>(agent)) }
                }
            }); */
        }

        internal string Topic(string senderId, string? hostId, string? agencyId, string? agentId)
        {
            var result = $"{(senderId != Id ? senderId : "-")}/{Id}/{hostId ?? "-"}/{agencyId ?? "-"}/{agentId ?? "-"}";
            return result;
        }

        internal string AuthorityTopic(string senderId)
        {
            return Topic(senderId, null, null, null);
        }

        internal string HostTopic(string senderId, string? hostId)
        {
            return Topic(senderId, hostId, null, null);
        }

        internal string AgencyTopic(string senderId, string agencyId)
        {
            return Topic(senderId, null, agencyId, null);
        }

        internal string AgentTopic(string senderId, string agentId)
        {
            return Topic(senderId, null, null, agentId);
        }
    }
}
